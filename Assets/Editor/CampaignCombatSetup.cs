using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Possession.Editor
{
    public static class CampaignCombatSetup
    {
        public const float CharacterMultiplier = 1.25f;
        public const float GuardDetection = 9f, HeavyDetection = 7f, ConeAngle = 75f;
        public const float GuardShotRange = 8f, HeavyAttackRange = 3.5f;

        [MenuItem("Possession/Level Design/Tune Campaign Characters and Enemy Cones")]
        public static void TuneLevels()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new System.InvalidOperationException("Stop Play Mode before tuning campaign scenes.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            foreach (string path in CampaignLevels.ScenePaths)
            {
                var scene = EditorSceneManager.OpenScene(path);
                Apply(scene);
                EditorSceneManager.SaveScene(scene);
            }
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            AssetDatabase.SaveAssets();
        }

        public static void Apply(Scene scene)
        {
            if (!CampaignLevels.SceneNames.Contains(scene.name)) return;
            var roots = scene.GetRootGameObjects();
            var spirit = roots.SelectMany(r => r.GetComponentsInChildren<SpiritController>(true)).Single();
            var enemies = roots.SelectMany(r => r.GetComponentsInChildren<EnemyBase>(true)).ToArray();
            // PR 68 starts the entry Heavy at the near end, walking away from the Spirit.
            if (scene.name == CampaignLevels.FirstScene)
            {
                var entryHeavy = enemies.FirstOrDefault(e => e.name == "Heavy_03");
                if (entryHeavy != null)
                {
                    float z = 7f * CampaignLayoutScale.ForScene(scene);
                    entryHeavy.transform.position = new Vector3(entryHeavy.transform.position.x, entryHeavy.transform.position.y, z);
                    var spawn = roots.SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                        .FirstOrDefault(t => t.name == "SpawnPoint_Heavy_03");
                    if (spawn != null) spawn.position = entryHeavy.transform.position;
                }
            }
            Resize(spirit.transform, CharacterMultiplier);
            foreach (var enemy in enemies)
            {
                bool heavy = enemy is HeavyEnemy;
                Resize(enemy.transform, (heavy ? 1.3f : 1f) * CharacterMultiplier);
                var so = new SerializedObject(enemy);
                so.FindProperty("detectionRange").floatValue = heavy ? HeavyDetection : GuardDetection;
                so.FindProperty("visionAngle").floatValue = ConeAngle;
                so.FindProperty("moveSpeed").floatValue = heavy ? 4f : 5f;
                var points = so.FindProperty("waypoints");
                for (int i = 0; i < points.arraySize; i++)
                    if (points.GetArrayElementAtIndex(i).objectReferenceValue is Transform point)
                        point.position = new Vector3(point.position.x, enemy.transform.position.y, point.position.z);
                so.ApplyModifiedPropertiesWithoutUndo();
                var attacks = enemy.GetComponent<EnemyContactDamage>();
                if (attacks != null)
                {
                    var attack = new SerializedObject(attacks);
                    if (enemy.GetComponent<ShootAbility>() is ShootAbility shoot)
                    {
                        var ability = new SerializedObject(shoot);
                        attack.FindProperty("projectilePrefab").objectReferenceValue = ability.FindProperty("projectilePrefab").objectReferenceValue;
                        attack.FindProperty("firePoint").objectReferenceValue = ability.FindProperty("firePoint").objectReferenceValue;
                    }
                    attack.FindProperty("shootRange").floatValue = GuardShotRange;
                    attack.FindProperty("smashRange").floatValue = HeavyAttackRange;
                    attack.ApplyModifiedPropertiesWithoutUndo();
                }
                PrefabUtility.RecordPrefabInstancePropertyModifications(enemy);
                foreach (var label in roots.SelectMany(r => r.GetComponentsInChildren<WorldLabel>(true)).Where(l => l.Follow == enemy.transform))
                {
                    var text = new SerializedObject(label);var offset = text.FindProperty("offset");
                    var value = offset.vector3Value;value.y = Mathf.Max(value.y, enemy.GetComponent<CharacterController>().bounds.extents.y + .6f);
                    offset.vector3Value = value;text.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            foreach (var director in roots.SelectMany(r => r.GetComponentsInChildren<Level02Director>(true)))
            {
                var so = new SerializedObject(director);var courtyard = so.FindProperty("courtyard");
                var point = courtyard.vector3Value;point.y = spirit.transform.position.y;courtyard.vector3Value = point;so.ApplyModifiedPropertiesWithoutUndo();
                var charges = new SerializedObject(spirit.GetComponent<PossessionCharge>());
                charges.FindProperty("startingCharges").intValue = 1;charges.ApplyModifiedPropertiesWithoutUndo();
            }
            // Keep the merged presentation on all three campaign scenes, including Warehouse's copied shell.
            foreach (var camera in roots.SelectMany(r => r.GetComponentsInChildren<Camera>(true)))
            {
                var follow = camera.GetComponent<CameraFollow>();if (follow == null) continue;
                var so = new SerializedObject(follow);var offset = new Vector3(0f, 25f, -18f);
                so.FindProperty("offset").vector3Value = offset;so.ApplyModifiedPropertiesWithoutUndo();
                camera.transform.position = spirit.transform.position + offset;
            }
            foreach (var ammo in roots.SelectMany(r => r.GetComponentsInChildren<AmmoDisplay>(true)))
            {
                var so = new SerializedObject(ammo);
                so.FindProperty("bulletSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/BulletPip.png");
                so.FindProperty("hammerSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/HammerPip.png");
                so.FindProperty("pipSpacing").floatValue = 2f;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static void Resize(Transform actor, float scale)
        {
            actor.localScale = Vector3.one * scale;
            Physics.SyncTransforms();
            Collider collider = actor.GetComponent<CharacterController>();
            if (collider == null) collider = actor.GetComponent<Collider>();
            if (collider == null) return;
            var ray = new Ray(actor.position + Vector3.up * 5f, Vector3.down);
            var floor = Physics.RaycastAll(ray, 15f, 1 << 0, QueryTriggerInteraction.Ignore)
                .Where(h => h.collider.name.EndsWith("_Floor") || h.collider.name.StartsWith("Hall_"))
                .OrderBy(h => h.distance).FirstOrDefault();
            if (floor.collider == null) throw new System.InvalidOperationException("No floor under " + actor.name);
            actor.position += Vector3.up * (floor.point.y + .05f - collider.bounds.min.y);
            PrefabUtility.RecordPrefabInstancePropertyModifications(actor);
            Physics.SyncTransforms();
        }
    }
}
