using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Possession.Level04Gold.Editor
{
    public static class Level4GoldBuilder
    {
        public const string Root = "Assets/Level04Gold";
        public const string ScenePath = Root + "/Scenes/Level_04_Gold.unity";
        public const string ConfigPath = Root + "/LevelConfig_Level_04_Gold.asset";
        private static Material steel, cyan, amber, dark, green;
        private static GameObject guardTemplate, heavyTemplate, targetTemplate, wallTemplate, leafTemplate;

        [MenuItem("Possession/Level 4 Gold/Build and Open")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before rebuilding Level 4.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(Root + "/Scenes"); Directory.CreateDirectory(Root + "/Materials"); AssetDatabase.Refresh();
            var source = EditorSceneManager.OpenScene(CampaignLevels.ScenePaths[0]);
            if (!EditorSceneManager.SaveScene(source, ScenePath, true)) throw new IOException("Could not create Level 4 scene shell.");
            var scene = EditorSceneManager.OpenScene(ScenePath);
            var spirit = Object.FindFirstObjectByType<SpiritController>();
            var manager = Object.FindFirstObjectByType<GameManager>();
            var hud = Object.FindFirstObjectByType<HUDManager>();
            guardTemplate = Template(Object.FindFirstObjectByType<GuardEnemy>().gameObject);
            heavyTemplate = Template(Object.FindFirstObjectByType<HeavyEnemy>().gameObject);
            targetTemplate = Template(Object.FindFirstObjectByType<Target>().gameObject);
            wallTemplate = Template(Object.FindFirstObjectByType<BreakableWall>().gameObject);
            leafTemplate = Template(Object.FindFirstObjectByType<SlidingDoorView>().LeftLeaf.gameObject);
            var templates = new[] { guardTemplate, heavyTemplate, targetTemplate, wallTemplate, leafTemplate };
            foreach (var root in scene.GetRootGameObjects())
            {
                if (templates.Contains(root) || root.GetComponent<SpiritController>() != null || root.GetComponent<GameManager>() != null
                    || root.GetComponent<Camera>() != null || root.GetComponent<Light>() != null || root.GetComponent<EventSystem>() != null
                    || root.GetComponentInChildren<HUDManager>(true) != null) continue;
                Object.DestroyImmediate(root);
            }

            PrepareMaterials();
            var geometry = Level4Geometry.Build();
            var lower = geometry.transform.Find("Lower floor"); var upper = geometry.transform.Find("Upper floor");
            var gameplay = new GameObject("Level04_Gameplay").transform;
            var config = MakeConfig();
            Possession.Editor.LevelBuildKit.ApplyConfig(config, manager, spirit.gameObject);
            spirit.transform.position = Level4Geometry.Spawn;
            spirit.transform.localScale = Vector3.one * 1.25f;
            Set(spirit, "moveSpeed", 8f); Set(spirit, "jumpForce", 8f); Set(spirit, "maxHP", 5);
            var possession = spirit.GetComponent<PossessionController>(); Set(possession, "possessRange", 5f); Set(possession, "ejectCooldown", 2f);
            var charge = spirit.GetComponent<PossessionCharge>() ?? spirit.gameObject.AddComponent<PossessionCharge>();
            Set(charge, "maxCharges", 3); Set(charge, "startingCharges", 1);
            Set(hud, "objectives", Array.Empty<string>());
            Level4PresentationSetup.Apply(scene);

            var guardJunction = Actor<GuardEnemy>(guardTemplate, lower, "Guard_Junction", new Vector3(26, 1.4f, 7),
                new[] { P(26, 7), P(29, 11), P(36, 11), P(36, 4) }, new Bounds(new Vector3(30, 2, 8), new Vector3(20, 8, 12)));
            Actor<GuardEnemy>(guardTemplate, lower, "Guard_Service", new Vector3(28, 1.4f, 24),
                new[] { P(28, 24), P(26, 20), P(22, 24), P(28, 28) }, new Bounds(new Vector3(27, 2, 24), new Vector3(14, 8, 12)));
            Actor<HeavyEnemy>(heavyTemplate, lower, "Heavy_Workshop", new Vector3(46, 1.775f, 25),
                new[] { P(46, 25, 1.775f), P(51, 21, 1.775f), P(42, 21, 1.775f) }, new Bounds(new Vector3(46, 2, 24), new Vector3(16, 8, 12)));
            var watchBounds = new Bounds(new Vector3(69, 7, 40), new Vector3(18, 8, 12));
            Actor<HeavyEnemy>(heavyTemplate, upper, "Heavy_UpperWatch", new Vector3(74.5f, 6.775f, 43),
                new[] { P(74.5f, 43, 6.775f), P(69.5f, 43, 6.775f), P(69.5f, 37, 6.775f), P(69.5f, 43, 6.775f) }, watchBounds);
            var eastSentry = Actor<GuardEnemy>(guardTemplate, upper, "Guard_ArchiveEast", P(74, 36, 6.4f),
                new[] { P(73, 36, 6.4f), P(76.2f, 36, 6.4f), P(76.2f, 37.6f, 6.4f), P(73, 37.6f, 6.4f) }, watchBounds);
            eastSentry.transform.rotation = Quaternion.LookRotation(Vector3.left);
            var westSentry = Actor<GuardEnemy>(guardTemplate, upper, "Guard_ArchiveWest", P(62, 43.8f, 6.4f),
                new[] { P(62, 43.8f, 6.4f), P(65, 44.4f, 6.4f), P(67, 42.5f, 6.4f), P(62, 41.3f, 6.4f) }, watchBounds);
            westSentry.transform.rotation = Quaternion.LookRotation(new Vector3(1, 0, -1));
            AddArchiveScanner(eastSentry); AddArchiveScanner(westSentry);
            var engineer = NewHost<EngineerEnemy>(lower, "Engineer", P(10, 25), amber,
                Array.Empty<Vector3>(), new Bounds(new Vector3(9, 2, 24), new Vector3(14, 8, 12)));
            DecorateEngineer(engineer);
            var archiveEngineer = NewHost<EngineerEnemy>(upper, "Engineer_Archive", P(70, 27.5f, 6.4f), amber,
                Array.Empty<Vector3>(), new Bounds(new Vector3(71, 7, 24), new Vector3(14, 8, 12)));
            DecorateEngineer(archiveEngineer);
            ApplyEngineerPatrols(scene);
            guardJunction.transform.rotation = Quaternion.LookRotation(Vector3.right);

            var galleryDoor = BuildTargetDoor(gameplay, "Gallery_Door", new Vector3(42, 2.25f, 8), 90f,
                new[] { new Vector3(24, 1.6f, 12), new Vector3(38, 1.6f, 11), new Vector3(37, 1.6f, 3) }, new Color(.15f, .8f, 1f));
            BuildTargetDoor(gameplay, "Service_Door", new Vector3(36, 2.25f, 24), 90f,
                new[] { new Vector3(21, 1.6f, 21), new Vector3(32, 1.6f, 28), new Vector3(32, 1.6f, 19) }, new Color(1f, .55f, .2f));
            var wall = Object.Instantiate(wallTemplate, gameplay); wall.name = "Workshop_SmashWall";
            wall.transform.SetPositionAndRotation(Level4Layout.Map(new Vector3(46, 2.25f, 32)), Quaternion.identity);
            wall.transform.localScale = new Vector3(Level4Layout.CorridorWidth, 4.5f, .65f); wall.SetActive(true);
            Set(wall.GetComponent<BreakableWall>(), "hitsToBreak", 2); Set(wall.GetComponent<BreakableWall>(), "minDamageRequired", 2);
            var wallBox = wall.GetComponent<BoxCollider>(); if (wallBox != null) { wallBox.size = Vector3.one; wallBox.center = Vector3.zero; }

            var access = BuildAccess(gameplay);
            var maintenance = BuildSecurityGate(lower, "Maintenance_Access_Door", P(18, 38, 2.25f), 90, P(15, 38), P(21.5f, 38));
            var vault = BuildVault(gameplay, hud);
            var archiveEntry = BuildSecurityGate(upper, "Archive_Entry_Door", P(70, 32, 7.25f), 0, P(70, 29, 6.4f), P(70, 34.5f, 6.4f));
            var archiveClue = BuildSecurityGate(upper, "Archive_Clue_Door", P(56, 40, 7.25f), 90, P(58.5f, 40, 6.4f), P(53.5f, 40, 6.4f));
            var cipher = BuildClue(upper);
            Set(cipher, "returnGates", new Object[] { archiveEntry, archiveClue });
            Set(vault, "cipher", cipher);
            Sign(lower, "ENGINEER", new Vector3(9, 3.5f, 29.7f), amber.color, .055f);
            Sign(lower, "MAINTENANCE", new Vector3(9, 3.5f, 45.7f), cyan.color, .045f);
            Sign(lower, "NORTH GALLERY", new Vector3(55, 3.3f, 13.7f), cyan.color, .055f);
            Sign(upper, "ARCHIVE / ENGINEER", new Vector3(70, 8.5f, 29.6f), amber.color, .04f);

            int index = 0;
            foreach (var point in new[] { P(6, 8, .1f), P(47, 6, .1f), P(23, 23, .1f), P(5, 41, .1f), P(48, 38, .1f), P(34, 40, .1f) }) Pickup(lower, point, ++index);
            Pickup(upper, P(75, 26, 5.1f), ++index);
            Pickup(upper, P(70, 26, 5.1f), ++index);
            Pickup(upper, P(43, 43, 5.1f), ++index);
            foreach (var point in new[] { P(5, 26, .1f), P(22, 39, .1f) }) Health(lower, point);
            Health(upper, P(54, 25, 5.1f));
            var director = gameplay.gameObject.AddComponent<Level4Director>();
            Set(director, "engineers", new Object[] { engineer, archiveEngineer }); Set(director, "accessGate", access); Set(director, "vault", vault); Set(director, "hud", hud);
            Set(director, "cipher", cipher); Set(director, "galleryDoor", galleryDoor);
            Set(director, "securityDoors", new Object[] { maintenance, archiveEntry, archiveClue });
            var over = new SerializedObject(hud).FindProperty("gameOverPanel").objectReferenceValue as GameObject;
            if (over != null)
            {
                var title = over.GetComponentsInChildren<Text>(true).FirstOrDefault(t => t.text.Contains("GAME OVER"));
                if (title != null) Set(director, "failureTitle", title);
            }
            var camera = Camera.main; var follow = camera.GetComponent<CameraFollow>();
            Set(follow, "target", spirit.transform); Set(follow, "offset", new Vector3(0, 25, -18));
            camera.transform.position = spirit.transform.position + new Vector3(0, 25, -18); camera.transform.rotation = Quaternion.Euler(55, 0, 0);
            camera.fieldOfView = 48f;
            var visibility = geometry.GetComponent<Level4FloorVisibility>(); if (visibility != null) visibility.RefreshVisibility();
            foreach (var template in templates) Object.DestroyImmediate(template);
            var buildScenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList(); buildScenes.Add(new EditorBuildSettingsScene(ScenePath, true)); EditorBuildSettings.scenes = buildScenes.ToArray();
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            if (SceneView.lastActiveSceneView != null) SceneView.lastActiveSceneView.LookAt(Level4Layout.Map(new Vector3(39, 0, 28)), Quaternion.Euler(55, -20, 0), 80f);
            Debug.Log("Level 4 Gold ready: expanded rooms, six-metre passages, eight hosts, Engineer security doors, archive cipher and vault.");
        }

        [MenuItem("Possession/Level 4 Gold/Open Local Level")]
        public static void Open()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!File.Exists(ScenePath)) { Build(); return; }
            EditorSceneManager.OpenScene(ScenePath);
        }

        [MenuItem("Possession/Level 4 Gold/Update Engineer Patrols")]
        public static void UpdateEngineerPatrols()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath);
            ApplyEngineerPatrols(scene);
            EditorSceneManager.SaveScene(scene);
        }

        public static void ApplyEngineerPatrols(Scene scene)
        {
            var engineers = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<EngineerEnemy>(true)).ToArray();
            ConfigureEngineerPatrol(engineers.Single(e => e.name == "Engineer"),
                new[] { P(10, 25), P(12.5f, 25), P(12.5f, 21), P(7, 21) }, 1.2f);
            ConfigureEngineerPatrol(engineers.Single(e => e.name == "Engineer_Archive"),
                new[] { P(70, 27.5f, 6.4f), P(74.5f, 27.5f, 6.4f), P(74.5f, 23, 6.4f), P(67.5f, 23, 6.4f) }, 2.4f);
            EditorSceneManager.MarkSceneDirty(scene);
        }

        private static void ConfigureEngineerPatrol(EngineerEnemy engineer, Vector3[] points, float startDelay)
        {
            // Waypoints live on the floor, outside the moving body, just like Guard routes.
            string routeName = engineer.name + "_Patrol";
            var route = engineer.transform.parent.Find(routeName);
            if (route == null)
            {
                route = new GameObject(routeName).transform;
                route.SetParent(engineer.transform.parent, false);
            }
            var nodes = new Object[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                string nodeName = "Waypoint_" + (i + 1);
                var node = route.Find(nodeName);
                if (node == null)
                {
                    node = new GameObject(nodeName).transform;
                    node.SetParent(route, false);
                }
                node.position = Level4Layout.Map(points[i]);
                nodes[i] = node;
            }
            Set(engineer, "waypoints", nodes);
            Set(engineer, "patrolSpeedMultiplier", .5f);
            Set(engineer, "turnSpeed", 240f);
            Set(engineer, "waypointWaitTime", .9f);
            Set(engineer, "patrolStartDelay", startDelay);
        }

        private static GameObject Template(GameObject source) { var clone = Object.Instantiate(source); clone.transform.SetParent(null); clone.SetActive(false); return clone; }
        private static Vector3 P(float x, float z, float y = 1.4f) => new Vector3(x, y, z);
        private static T Actor<T>(GameObject template, Transform parent, string name, Vector3 position, Vector3[] points, Bounds bounds) where T : EnemyBase
        {
            var go = Object.Instantiate(template, parent); go.name = name; go.transform.SetPositionAndRotation(Level4Layout.Map(position), Quaternion.identity); go.SetActive(true);
            var enemy = go.GetComponent<T>(); Configure(enemy, points, bounds); return enemy;
        }
        private static T NewHost<T>(Transform parent, string name, Vector3 position, Material material, Vector3[] points, Bounds bounds) where T : EnemyBase
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule); go.name = name; go.transform.SetParent(parent); go.layer = 6;
            Object.DestroyImmediate(go.GetComponent<CapsuleCollider>());
            go.transform.position = Level4Layout.Map(position); go.transform.localScale = Vector3.one * 1.25f; go.GetComponent<Renderer>().sharedMaterial = material;
            var cc = go.AddComponent<CharacterController>(); cc.height = 2f; cc.radius = .5f; cc.skinWidth = .05f; cc.stepOffset = .3f;
            var host = go.AddComponent<T>(); go.AddComponent<EnemyContactDamage>(); Configure(host, points, bounds); return host;
        }
        private static void Configure(EnemyBase enemy, Vector3[] points, Bounds bounds)
        {
            Set(enemy, "moveSpeed", enemy is HeavyEnemy ? 4f : 5f); Set(enemy, "maxHP", enemy is HeavyEnemy ? 5 : 3);
            Set(enemy, "detectionRange", enemy is HeavyEnemy ? 7f : enemy is GuardEnemy ? 9f : 5f); Set(enemy, "visionAngle", 75f);
            Set(enemy, "patrolSpeedMultiplier", .5f); Set(enemy, "turnSpeed", 240f); Set(enemy, "waypointWaitTime", .9f);
            Set(enemy, "spiritLayer", ~0); Set(enemy, "useHomeBounds", true); Set(enemy, "homeBounds", Level4Layout.MapBounds(bounds));
            var nodes = new List<Transform>();
            foreach (var point in points) { var node = new GameObject(enemy.name + "_Waypoint").transform; node.SetParent(enemy.transform.parent); node.position = Level4Layout.Map(point); nodes.Add(node); }
            Set(enemy, "waypoints", nodes.Cast<Object>().ToArray());
            var controller = enemy.GetComponent<CharacterController>(); if (controller != null) { controller.minMoveDistance = 0f; controller.slopeLimit = 45f; }
        }

        private static void AddArchiveScanner(GuardEnemy guard)
        {
            var ability = new SerializedObject(guard.GetComponent<ShootAbility>());
            var pressure = guard.gameObject.AddComponent<Level4SentryPressure>();
            Set(pressure, "firePoint", ability.FindProperty("firePoint").objectReferenceValue);
            Set(pressure, "projectilePrefab", ability.FindProperty("projectilePrefab").objectReferenceValue);
            var lamp = Cube(guard.transform, "Archive scanner warning", new Vector3(0, 1.15f, .1f), new Vector3(.25f, .14f, .25f), amber);
            Set(pressure, "warningLamp", lamp.GetComponent<Renderer>());
        }

        private static void DecorateEngineer(EngineerEnemy engineer)
        {
            Cube(engineer.transform, "Equipment helmet", new Vector3(0, .75f, 0), new Vector3(1.05f, .2f, 1.05f), amber);
            Cube(engineer.transform, "Equipment toolkit", new Vector3(.42f, -.1f, 0), new Vector3(.35f, .45f, .35f), steel);
        }

        private static Level4AccessGate BuildSecurityGate(Transform parent, string name, Vector3 position, float yaw, Vector3 outsidePanel, Vector3 insideRelease)
        {
            var root = new GameObject(name, typeof(BoxCollider), typeof(Level4AccessGate));
            root.transform.SetParent(parent); root.transform.SetPositionAndRotation(Level4Layout.Map(position), Quaternion.Euler(0, yaw, 0));
            float width = Level4Layout.CorridorWidth;
            root.GetComponent<BoxCollider>().size = new Vector3(width, Level4Geometry.WallHeight, .4f);
            var panel = Cube(root.transform, "Retracting security panel", Vector3.zero, new Vector3(width-.1f,4.4f,.35f),steel).transform;
            Cube(panel,"Cyan seam",new Vector3(0,0,-.56f),new Vector3(.015f,.95f,.1f),cyan);
            foreach(float x in new[]{-.38f,.38f}) Cube(panel,"Security light strip",new Vector3(x,0,-.56f),new Vector3(.02f,.68f,.1f),cyan);
            Cube(root.transform, "Left jamb", new Vector3(-width*.5f, 0, 0), new Vector3(.18f, 4.5f, .5f), dark);
            Cube(root.transform, "Right jamb", new Vector3(width*.5f, 0, 0), new Vector3(.18f, 4.5f, .5f), dark);
            var release = new GameObject("Inside return release").transform; release.SetParent(parent); release.position = Level4Layout.Map(insideRelease);
            var control = new GameObject("Engineer security panel").transform; control.SetParent(parent); control.position = Level4Layout.Map(outsidePanel);
            Cube(control,"Engineering controls",new Vector3(0,-.4f,0),new Vector3(.7f,1f,.45f),dark);
            var lamp = Cube(control, "Security status", new Vector3(0,.15f,-.25f), new Vector3(.55f,.12f,.1f), amber);
            Cube(release,"Return button",new Vector3(0,-.3f,0),new Vector3(.45f,.7f,.4f),cyan);
            var gate = root.GetComponent<Level4AccessGate>();
            Set(gate,"engineerPoint",control);Set(gate,"releasePoint",release);Set(gate,"movingPanel",panel);Set(gate,"releaseLamp",lamp.GetComponent<Renderer>());Set(gate,"hidePanelWhenOpen",true);
            return gate;
        }

        private static Door BuildTargetDoor(Transform parent, string name, Vector3 position, float yaw, Vector3[] positions, Color color)
        {
            var root = new GameObject(name, typeof(BoxCollider), typeof(Door), typeof(SlidingDoorView)); root.transform.SetParent(parent);
            root.transform.SetPositionAndRotation(Level4Layout.Map(position), Quaternion.Euler(0, yaw, 0)); root.transform.localScale = new Vector3(Level4Layout.CorridorWidth/4f, 1, 1); root.GetComponent<BoxCollider>().size = new Vector3(4, 4.5f, .55f);
            var targets = new List<Target>();
            for (int i = 0; i < positions.Length; i++)
            {
                var go = Object.Instantiate(targetTemplate, parent); go.name = name + "_Target_" + (i + 1); go.transform.position = Level4Layout.Map(positions[i]); go.transform.localScale = Vector3.one * .85f; go.SetActive(true);
                var renderer = go.GetComponent<Renderer>(); if (renderer != null) renderer.sharedMaterial = Mat(name + " target", color, true);
                targets.Add(go.GetComponent<Target>());
            }
            var leaves = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                var leaf = Object.Instantiate(leafTemplate, root.transform); leaf.name = i == 0 ? "Left leaf" : "Right leaf"; leaf.SetActive(true);
                leaf.transform.localPosition = new Vector3(i == 0 ? -1 : 1, 0, 0); leaf.transform.localRotation = Quaternion.identity; leaf.transform.localScale = new Vector3(1.95f, 4.2f, .4f); leaves[i] = leaf.transform;
                foreach (var collider in leaf.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(collider);
            }
            Cube(root.transform, "Frame left", new Vector3(-2.16f, 0, 0), new Vector3(.25f, 4.6f, .7f), steel);
            Cube(root.transform, "Frame right", new Vector3(2.16f, 0, 0), new Vector3(.25f, 4.6f, .7f), steel);
            Cube(root.transform, "Frame crown", new Vector3(0, 2.25f, 0), new Vector3(4.55f, .25f, .7f), steel);
            var lamps = new List<Renderer>();
            for (int i = 0; i < 3; i++) lamps.Add(Cube(root.transform, "Target lamp", new Vector3((i - 1) * .55f, 2.25f, -.4f), new Vector3(.3f, .16f, .1f), Mat(name + " lamp", color, true)).GetComponent<Renderer>());
            Set(root.GetComponent<Door>(), "requiredTargets", targets.Cast<Object>().ToArray()); Set(root.GetComponent<Door>(), "isExit", false);
            var view = root.GetComponent<SlidingDoorView>(); Set(view, "targets", targets.Cast<Object>().ToArray()); Set(view, "width", 4f); Set(view, "leftLeaf", leaves[0]); Set(view, "rightLeaf", leaves[1]); Set(view, "clipToFrame", true); Set(view, "lamps", lamps.Cast<Object>().ToArray()); Set(view, "groupColor", color);
            return root.GetComponent<Door>();
        }

        private static Level4AccessGate BuildAccess(Transform parent)
        {
            var go = new GameObject("Service_Access_Gate", typeof(BoxCollider), typeof(Level4AccessGate)); go.transform.SetParent(parent); go.transform.position = Level4Layout.Map(new Vector3(26, 2.25f, 32)); go.transform.localScale = new Vector3(Level4Layout.CorridorWidth/4f,1,1);
            go.GetComponent<BoxCollider>().size = new Vector3(4, 4.5f, .55f);
            var panel = Cube(go.transform, "Access panel", Vector3.zero, new Vector3(3.95f, 4.4f, .35f), steel).transform;
            for (int i = 0; i < 5; i++) Cube(panel, "Access light strip", new Vector3(0, -.4f + i * .2f, -.53f), new Vector3(.9f, .015f, .05f), cyan);
            var release = new GameObject("Inside release").transform; release.SetParent(parent); release.position = Level4Layout.Map(new Vector3(26, 1.4f, 36));
            var lamp = Cube(release, "Release lamp", Vector3.zero, new Vector3(.6f, .5f, .4f), amber).GetComponent<Renderer>();
            Set(go.GetComponent<Level4AccessGate>(), "movingPanel", panel); Set(go.GetComponent<Level4AccessGate>(), "releasePoint", release); Set(go.GetComponent<Level4AccessGate>(), "releaseLamp", lamp);
            return go.GetComponent<Level4AccessGate>();
        }
        private static Level4Vault BuildVault(Transform parent, HUDManager hud)
        {
            var go = new GameObject("Engineer_Vault", typeof(BoxCollider), typeof(Level4Vault)); go.transform.SetParent(parent); go.transform.position = Level4Layout.Map(new Vector3(36, 2.25f, 44)); go.transform.localScale = new Vector3(Level4Layout.GeometryScale,1,1); go.GetComponent<BoxCollider>().size = new Vector3(6, 4.5f, .65f);
            var hinge = new GameObject("Vault hinge").transform; hinge.SetParent(go.transform, false); hinge.localPosition = new Vector3(-3, 0, 0);
            Cube(hinge, "Armoured vault door", new Vector3(3, 0, 0), new Vector3(5.8f, 4.4f, .55f), steel);
            var plate = GameObject.CreatePrimitive(PrimitiveType.Cylinder); plate.name = "Mechanical dial plate"; Object.DestroyImmediate(plate.GetComponent<Collider>()); plate.transform.SetParent(hinge, false);
            plate.transform.localPosition = new Vector3(3, 0, -.4f); plate.transform.localRotation = Quaternion.Euler(90, 0, 0); plate.transform.localScale = new Vector3(2.4f, .08f, 2.4f); plate.GetComponent<Renderer>().sharedMaterial = dark;
            var pointer = new GameObject("Vault pointer").transform; pointer.SetParent(hinge, false); pointer.localPosition = new Vector3(3, 0, -.52f);
            Cube(pointer, "Dial hand", new Vector3(0, .48f, 0), new Vector3(.07f, .95f, .07f), amber);
            var bolt1 = Cube(hinge, "First bolt", new Vector3(.35f, .9f, -.4f), new Vector3(.65f, .28f, .2f), cyan).transform;
            var bolt2 = Cube(hinge, "Second bolt", new Vector3(5.65f, -.9f, -.4f), new Vector3(.65f, .28f, .2f), cyan).transform;
            Cube(go.transform, "Vault frame left", new Vector3(-3.15f, 0, 0), new Vector3(.3f, 4.7f, 1f), dark);
            Cube(go.transform, "Vault frame right", new Vector3(3.15f, 0, 0), new Vector3(.3f, 4.7f, 1f), dark);
            Cube(go.transform, "Vault crown", new Vector3(0, 2.35f, 0), new Vector3(6.6f, .3f, 1f), amber);
            var at = new GameObject("Engineer operator point").transform; at.SetParent(parent); at.position = Level4Layout.Map(new Vector3(36, 1.4f, 41.5f));
            var vault = go.GetComponent<Level4Vault>(); Set(vault, "operatorPoint", at); Set(vault, "doorLeaf", hinge); Set(vault, "wheelPointer", pointer); Set(vault, "firstBolt", bolt1); Set(vault, "secondBolt", bolt2);
            Set(vault, "completionPanel", new SerializedObject(hud).FindProperty("levelCompletePanel").objectReferenceValue);
            Sign(parent, "VAULT", new Vector3(36, 3.9f, 43.5f), amber.color, .08f);
            return vault;
        }
        private static Level4Cipher BuildClue(Transform upper)
        {
            var plaque = new GameObject("Archive vault cipher", typeof(Level4Cipher)).transform; plaque.SetParent(upper); plaque.position = Level4Layout.Map(new Vector3(49, 7.2f, 45.6f));
            Cube(plaque, "Clue backing", Vector3.zero, new Vector3(6, 2.8f, .12f), dark);
            var triangle = new GameObject("Clockwise triangle", typeof(MeshFilter), typeof(MeshRenderer)); triangle.transform.SetParent(plaque, false); triangle.transform.localPosition = new Vector3(-1.2f, .15f, -.1f);
            var meshPath = Root + "/Meshes/Clue_triangle.asset"; var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            if (mesh == null) { mesh = new Mesh { name = "Clue triangle" }; mesh.vertices = new[] { new Vector3(0, .65f, 0), new Vector3(.65f, -.5f, 0), new Vector3(-.65f, -.5f, 0) }; mesh.triangles = new[] { 0, 1, 2, 2, 1, 0 }; mesh.RecalculateNormals(); AssetDatabase.CreateAsset(mesh, meshPath); }
            var symbol = Mat("Cipher_symbol", new Color(1f, .78f, .22f));
            symbol.shader = Shader.Find("Unlit/Color"); symbol.color = new Color(1f, .78f, .22f); EditorUtility.SetDirty(symbol);
            triangle.GetComponent<MeshFilter>().sharedMesh = mesh; triangle.GetComponent<MeshRenderer>().sharedMaterial = symbol;
            Cube(plaque, "Counterclockwise square", new Vector3(1.2f, .15f, -.1f), new Vector3(1, 1, .05f), cyan);
            Sign(plaque, "1  CW          2  CCW", new Vector3(49, 6.3f, 45.45f), Color.white, .035f);
            var point = new GameObject("Cipher recovery point").transform; point.SetParent(plaque); point.position = Level4Layout.Map(P(49.5f, 40, 6.4f));
            var console = Cube(point, "Cipher console", new Vector3(0, -.6f, 0), new Vector3(1.1f, .9f, .7f), dark);
            var lamp = Cube(point, "Cipher status", new Vector3(0, -.22f, -.38f), new Vector3(.8f, .12f, .1f), amber);
            var module = Cube(point, "Recovered cipher module", new Vector3(0, .2f, 0), new Vector3(.7f, .06f, .5f), cyan);
            Cube(module.transform, "Cipher contacts", new Vector3(0, .9f, 0), new Vector3(.7f, .35f, .7f), amber);
            var cipher = plaque.GetComponent<Level4Cipher>();
            Set(cipher, "recoveryPoint", point); Set(cipher, "moduleVisual", module); Set(cipher, "recoveredLamp", lamp.GetComponent<Renderer>());
            Sign(plaque, "VAULT CIPHER", new Vector3(49.5f, 8.2f, 40.3f), cyan.color, .045f);
            return cipher;
        }
        private static void Pickup(Transform parent, Vector3 point, int index)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Puzzle/SoulCoin.prefab");
            var go = prefab != null ? (GameObject)PrefabUtility.InstantiatePrefab(prefab) : new GameObject("Soul", typeof(SphereCollider), typeof(PossessionChargePickup));
            go.name = "Soul_Charge_" + index; go.transform.SetParent(parent); go.transform.position = Level4Layout.Map(point);
            Set(go.GetComponent<PossessionChargePickup>(), "reforms", true); Set(go.GetComponent<PossessionChargePickup>(), "cooldown", 8f);
            ObjectiveVisibility.Ensure(go.GetComponent<PossessionChargePickup>());
        }
        private static void Health(Transform parent, Vector3 point) { var go = new GameObject("Spirit_Health", typeof(SphereCollider), typeof(HealthPickup)); go.transform.SetParent(parent); go.transform.position = Level4Layout.Map(point); }
        private static TextMesh Sign(Transform parent, string text, Vector3 pos, Color color, float scale)
        {
            var go = new GameObject(text, typeof(TextMesh)); go.transform.SetParent(parent); go.transform.position = Level4Layout.Map(pos); go.transform.rotation = Quaternion.identity; go.transform.localScale = Vector3.one * scale;
            var sign = go.GetComponent<TextMesh>(); sign.text = text; sign.fontSize = 56; sign.anchor = TextAnchor.MiddleCenter; sign.alignment = TextAlignment.Center; sign.color = color; return sign;
        }
        private static GameObject Cube(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; Object.DestroyImmediate(go.GetComponent<Collider>()); go.transform.SetParent(parent, false); go.transform.localPosition = pos; go.transform.localScale = scale; go.GetComponent<Renderer>().sharedMaterial = mat; return go;
        }
        private static void PrepareMaterials()
        {
            steel = Mat("Pale steel", new Color(.63f, .71f, .76f)); dark = Mat("Dial steel", new Color(.12f, .19f, .25f));
            cyan = Mat("Cyan guidance", new Color(.15f, .85f, 1f), true); amber = Mat("Engineer amber", new Color(1f, .66f, .2f), true);
            green = Mat("Ready green", new Color(.2f, 1f, .6f), true);
        }
        private static Material Mat(string name, Color color, bool emission = false)
        {
            var path = Root + "/Materials/" + name.Replace(' ', '_') + ".mat"; var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find("Standard")); AssetDatabase.CreateAsset(material, path); }
            material.color = color; if (emission) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", color * .55f); }
            EditorUtility.SetDirty(material); return material;
        }
        private static LevelConfig MakeConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<LevelConfig>(ConfigPath);
            if (config == null) { config = ScriptableObject.CreateInstance<LevelConfig>(); AssetDatabase.CreateAsset(config, ConfigPath); }
            Set(config, "levelName", "Level_04_Gold"); Set(config, "levelTime", 600f); Set(config, "possessionTimer", 12f);
            Set(config, "guardMoveSpeed", 5f); Set(config, "guardMaxHP", 3); Set(config, "guardDetectionRange", 9f);
            Set(config, "guardWaypointWaitTime", .9f); Set(config, "guardChaseStopDistance", 1.5f);
            Set(config, "goldParTime", 210f); Set(config, "silverParTime", 330f); Set(config, "bronzeParTime", 540f); return config;
        }
        private static void Set(Object target, string name, object value)
        {
            var so = new SerializedObject(target); var property = so.FindProperty(name);
            if (property == null) throw new InvalidOperationException(target.GetType().Name + " has no setting " + name);
            if (value is Object[] objects) { property.arraySize = objects.Length; for (int i = 0; i < objects.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = objects[i]; }
            else if (value is string[] strings) { property.arraySize = strings.Length; for (int i = 0; i < strings.Length; i++) property.GetArrayElementAtIndex(i).stringValue = strings[i]; }
            else if (value is Object obj) property.objectReferenceValue = obj;
            else if (value is float f) property.floatValue = f; else if (value is int n) property.intValue = n;
            else if (value is bool b) property.boolValue = b; else if (value is string s) property.stringValue = s;
            else if (value is Vector3 v) property.vector3Value = v; else if (value is Bounds bounds) property.boundsValue = bounds;
            else if (value is Color color) property.colorValue = color; else throw new ArgumentException("Unsupported setting " + name);
            so.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(target);
        }
    }
}
