using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Possession.Tests
{
    public class HeavyW2Tests
    {
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = 1;
            var level = SceneManager.GetActiveScene();
            var empty = SceneManager.CreateScene("HeavyW2_Cleanup");
            SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync(level);
            EventBus.Clear();
        }

        [UnityTest] public IEnumerator Level01RetainsHeavyStats() => CheckPlacement("Level_01", 1, 1.8f);
        [UnityTest] public IEnumerator Level02HasOneHeavyWithPatrolAndSpeed2() => CheckPlacement("Level_02", 1, 2f);
        [UnityTest] public IEnumerator Level03HasTwoHeaviesWithPatrolAndSpeed22() => CheckPlacement("Level_03", 2, 2.2f);
        [UnityTest] public IEnumerator Level02HeavySmashesWallAndSpiritFinishes() => CheckPuzzle("Level_02");
        [UnityTest] public IEnumerator Level03HeaviesSmashBothWallsAndSpiritFinishes() => CheckPuzzle("Level_03");

        [UnityTest]
        public IEnumerator Level03RightCorridorHasNoSolidCrossWallsBetweenGates()
        {
            yield return Load("Level_03");
            FreezeHeavies(); Physics.SyncTransforms();
            Assert.That(Physics.SphereCast(new Vector3(6, 1.5f, 28), .65f,
                Vector3.back, out var hit, 14, ~0, QueryTriggerInteraction.Ignore), Is.False,
                "The intended Heavy route must be clear between walls: " + (hit.collider != null ? hit.collider.name : "none"));
        }

        [UnityTest]
        public IEnumerator GuardDoorAndHeavyWallsStillSealTheirOpenings()
        {
            foreach (var name in new[] { "Level_02", "Level_03" })
            {
                yield return Load(name); FreezeHeavies();
                var door = Object.FindFirstObjectByType<Door>();
                Assert.That(door.IsOpen, Is.False);
                Assert.That(Object.FindFirstObjectByType<LevelExit>().IsReady, Is.False);
                foreach (var wall in Walls())
                {
                    Assert.That(wall.MinDamageRequired, Is.EqualTo(2));
                    var bounds = wall.GetComponent<Collider>().bounds;
                    for (float offset = -1f; offset <= 1f; offset += .5f)
                    {
                        var origin = bounds.center + new Vector3(offset, -1.5f, -1f);
                        Assert.That(Physics.Raycast(origin, Vector3.forward, 2f, 1 << 7), Is.True);
                    }
                    int hp = wall.CurrentHP;
                    wall.TakeDamage(1);
                    Assert.That(wall.CurrentHP, Is.EqualTo(hp), "Guard bullets must not substitute for a Heavy smash.");
                }
            }
        }

        private static IEnumerator CheckPlacement(string name, int count, float speed)
        {
            yield return Load(name);
            var heavies = Heavies();
            Assert.That(heavies.Length, Is.EqualTo(count));
            if (name != "Level_01") Assert.That(Walls().Length, Is.EqualTo(count));
            Assert.That(Object.FindObjectsByType<HUDManager>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            var positions = heavies.Select(h => h.transform.position).ToArray();
            foreach (var heavy in heavies)
            {
                Assert.That(heavy.GetType(), Is.EqualTo(typeof(HeavyEnemy)));
                Assert.That(Read<float>(heavy, "moveSpeed"), Is.EqualTo(speed));
                Assert.That(heavy.CurrentHP, Is.EqualTo(5));
                Assert.That(heavy.GetComponent<SmashAbility>(), Is.Not.Null);
                Assert.That(heavy.GetComponent<EnemyContactDamage>(), Is.Not.Null);
                Assert.That(heavy.GetComponent<HeavySmashIndicator>(), Is.Not.Null);
                Assert.That(heavy.GetComponent<CharacterController>().minMoveDistance, Is.EqualTo(0));
                Assert.That(heavy.GetComponent<Renderer>().sharedMaterial, Is.Not.Null);
                var points = Read<Transform[]>(heavy, "waypoints");
                Assert.That(points.Length, Is.EqualTo(2));
                foreach (var point in points)
                {
                    Assert.That(point, Is.Not.Null);
                    Assert.That(point.IsChildOf(heavy.transform), Is.False);
                    Assert.That(point.gameObject.scene, Is.EqualTo(heavy.gameObject.scene));
                }
                if (name != "Level_01")
                {
                    int index = heavy.name.EndsWith("02") ? 2 : 1;
                    var marker = GameObject.Find("SpawnPoint_Heavy_0" + index);
                    Assert.That(heavy.transform.position.x, Is.EqualTo(marker.transform.position.x).Within(.1f));
                    Assert.That(heavy.transform.position.z, Is.EqualTo(marker.transform.position.z).Within(.1f));
                    Assert.That(Physics.SphereCast(points[0].position, .65f,
                        (points[1].position - points[0].position).normalized, out _,
                        Vector3.Distance(points[0].position, points[1].position), 1 << 0,
                        QueryTriggerInteraction.Ignore), Is.False, "Patrol segment must clear the room walls.");
                }
            }
            yield return new WaitForSeconds(1.3f);
            for (int i = 0; i < heavies.Length; i++)
            {
                Vector3 displacement = heavies[i].transform.position - positions[i]; displacement.y = 0;
                Assert.That(displacement.magnitude, Is.GreaterThan(.15f), heavies[i].name + " did not patrol.");
            }
        }

        private static IEnumerator CheckPuzzle(string name)
        {
            yield return Load(name); FreezeHeavies();
            var player = Object.FindFirstObjectByType<PossessionController>(); player.enabled = false;
            var spirit = player.GetComponent<SpiritController>();
            foreach (var target in Object.FindObjectsByType<Target>(FindObjectsSortMode.None)) target.OnHit();
            Assert.That(Object.FindFirstObjectByType<Door>().IsOpen, Is.True);
            var heavies = Heavies(); var walls = Walls();
            for (int i = 0; i < walls.Length; i++)
            {
                var heavy = heavies[i];
                Vector3 forward = name == "Level_02" ? Vector3.forward : Vector3.back;
                Vector3 attack = walls[i].transform.position - forward * 2.1f; attack.y = 1.45f;
                Move(heavy, attack, forward);
                Invoke(player, "Possess", heavy);
                Assert.That(player.CurrentState, Is.EqualTo(PlayerState.Possessed_Heavy));
                Assert.That(heavy.GetComponent<HeavySmashIndicator>().IsVisible, Is.True);
                heavy.GetComponent<SmashAbility>().TryExecute();
                Assert.That(walls[i].CurrentHP, Is.LessThanOrEqualTo(0));
                Assert.That(walls[i].gameObject.activeSelf, Is.False);
                Assert.That(heavy.GetComponent<SmashAbility>().UsesLeft, Is.EqualTo(0));
                Invoke(player, "Eject", false);
                Assert.That(heavy.GetComponent<HeavySmashIndicator>().IsVisible, Is.False);
                if (i < walls.Length - 1) Assert.That(Object.FindFirstObjectByType<LevelExit>().IsReady, Is.False);
            }
            Assert.That(Object.FindFirstObjectByType<LevelExit>().IsReady, Is.True);
            var exit = Object.FindFirstObjectByType<LevelExit>();
            spirit.RestoreAt(new Vector3(exit.transform.position.x, 1.15f, exit.transform.position.z));
            Physics.SyncTransforms(); yield return new WaitForSecondsRealtime(.2f);
            Assert.That(GameManager.Instance.CurrentState, Is.EqualTo(LevelState.Complete));
        }

        private static IEnumerator Load(string name)
        {
            Time.timeScale = 1; EventBus.Clear();
            yield return SceneManager.LoadSceneAsync(name); yield return null;
        }
        private static HeavyEnemy[] Heavies() => Object.FindObjectsByType<HeavyEnemy>(FindObjectsSortMode.None).OrderBy(h => h.name).ToArray();
        private static BreakableWall[] Walls() => Object.FindObjectsByType<BreakableWall>(FindObjectsSortMode.None).OrderByDescending(w => w.transform.position.z).ToArray();
        private static void FreezeHeavies() { foreach (var heavy in Heavies()) heavy.enabled = false; }
        private static void Move(HeavyEnemy heavy, Vector3 position, Vector3 forward)
        {
            var cc = heavy.GetComponent<CharacterController>(); cc.enabled = false;
            heavy.transform.SetPositionAndRotation(position, Quaternion.LookRotation(forward));
            cc.enabled = true; Physics.SyncTransforms();
        }
        private static T Read<T>(object target, string name)
        {
            for (var type = target.GetType(); type != null; type = type.BaseType)
            {
                var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null) return (T)field.GetValue(target);
            }
            throw new System.MissingFieldException(name);
        }
        private static void Invoke(object target, string name, object value) => target.GetType()
            .GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, new[] { value });
    }
}
