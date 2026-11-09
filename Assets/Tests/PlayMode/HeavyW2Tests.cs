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
        [UnityTest] public IEnumerator Level02HasThreeHeaviesWithPatrolAndSpeed2() => CheckPlacement("Level_02", 3, 2f);
        [UnityTest] public IEnumerator Level03HasTwoHeaviesWithPatrolAndSpeed22() => CheckPlacement("Level_03", 2, 2.2f);
        // Level02ChargeMazeTests exercises both replacement Level 2 solutions with charge collection.
        [UnityTest] public IEnumerator Level03RequiresBothSmashesAndGuardShotBeforeSpiritFinishes() => CheckPuzzle("Level_03");

        [UnityTest]
        public IEnumerator Level03RightCorridorHasNoSolidCrossWallsBetweenGates()
        {
            yield return Load("Level_03");
            FreezeHeavies(); Physics.SyncTransforms();
            // The redesigned corridor runs along x=8; Target_01 at x=6 is a valid puzzle obstacle.
            Assert.That(Physics.SphereCast(new Vector3(8, 1.5f, 28), .65f,
                Vector3.back, out var hit, 14, 1 << 0, QueryTriggerInteraction.Ignore), Is.False,
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
                Assert.That(name == "Level_02" ? Object.FindFirstObjectByType<Level02Director>().CanExit
                    : Object.FindFirstObjectByType<LevelExit>().IsReady, Is.False);
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
            if (name != "Level_01") Assert.That(Walls().Length, Is.EqualTo(name == "Level_02" ? 2 : count));
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
                    var marker = GameObject.Find("SpawnPoint_" + heavy.name);
                    Assert.That(marker, Is.Not.Null, "Missing spawn marker for " + heavy.name);
                    Assert.That(heavy.transform.position.x, Is.EqualTo(marker.transform.position.x).Within(.1f));
                    Assert.That(heavy.transform.position.z, Is.EqualTo(marker.transform.position.z).Within(.1f));
                    Assert.That(Physics.SphereCast(points[0].position, .65f,
                        (points[1].position - points[0].position).normalized, out _,
                        Vector3.Distance(points[0].position, points[1].position), 1 << 0,
                        QueryTriggerInteraction.Ignore), Is.False, "Patrol segment must clear the room walls.");
                }
            }
            yield return new WaitForSeconds(heavies.Max(h => h.PatrolStartDelay) + 1.3f);
            for (int i = 0; i < heavies.Length; i++)
            {
                Vector3 displacement = heavies[i].transform.position - positions[i]; displacement.y = 0;
                Assert.That(displacement.magnitude, Is.GreaterThan(.15f), heavies[i].name + " did not patrol.");
            }
        }

        private static IEnumerator CheckPuzzle(string name)
        {
            yield return Load(name); FreezeHeavies();
            foreach (var enemy in Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None)) enemy.enabled = false;
            foreach (var attack in Object.FindObjectsByType<EnemyContactDamage>(FindObjectsSortMode.None)) attack.enabled = false;
            var player = Object.FindFirstObjectByType<PossessionController>(); player.enabled = false;
            var spirit = player.GetComponent<SpiritController>();
            var door = Object.FindFirstObjectByType<Door>();
            Assert.That(door.IsOpen, Is.False);
            var heavies = Heavies(); var walls = Walls().OrderBy(w => w.transform.position.z).ToArray();
            Assert.That(walls.Select(w => w.name), Is.EqualTo(new[] { "BreakableWall_Entry", "BreakableWall_01" }));
            for (int i = 0; i < walls.Length; i++)
            {
                var heavy = heavies.Single(h => h.name == (i == 0 ? "Heavy_03" : "Heavy_01"));
                Vector3 forward = i == 0 ? Vector3.forward : Vector3.back;
                Vector3 attack = walls[i].transform.position - forward * 2.1f; attack.y = 1.45f;
                Move(heavy, attack, forward);
                spirit.RestoreAt(attack - forward * 1.5f);
                Physics.SyncTransforms();
                Invoke(player, "Possess", heavy);
                Assert.That(player.CurrentState, Is.EqualTo(PlayerState.Possessed_Heavy));
                Assert.That(heavy.GetComponent<HeavySmashIndicator>().IsVisible, Is.True);
                heavy.GetComponent<SmashAbility>().TryExecute();
                Assert.That(walls[i].CurrentHP, Is.LessThanOrEqualTo(0));
                Assert.That(walls[i].gameObject.activeSelf, Is.False);
                Assert.That(heavy.GetComponent<SmashAbility>().UsesLeft, Is.EqualTo(0));
                Invoke(player, "Eject", false);
                Set(player, "ejectCooldownTimer", 0f);
                Assert.That(heavy.GetComponent<HeavySmashIndicator>().IsVisible, Is.False);
                var charge = player.GetComponent<PossessionCharge>();
                if (charge != null && !charge.HasCharge)
                {
                    var pickup = Object.FindObjectsByType<PossessionChargePickup>(FindObjectsSortMode.None)
                        .FirstOrDefault(p => p.IsReady);
                    if (pickup != null) { spirit.RestoreAt(pickup.transform.position + Vector3.up); Physics.SyncTransforms(); pickup.TryCollect(charge); }
                }
                Assert.That(Object.FindFirstObjectByType<LevelExit>().IsReady, Is.False,
                    "Smashing must not substitute for the Guard's target/door puzzle.");
            }
            var guard = Object.FindObjectsByType<GuardEnemy>(FindObjectsSortMode.None).Single(g => g.name == "Guard_03");
            var target = Object.FindObjectsByType<Target>(FindObjectsSortMode.None).Single(t => t.name == "Target_01");
            Move(guard, new Vector3(8f, 1.05f, 18f), target.transform.position - guard.transform.position);
            spirit.RestoreAt(guard.transform.position - guard.transform.forward * 1.8f);
            Physics.SyncTransforms();
            Invoke(player, "Possess", guard);
            Assert.That(player.PossessedEnemy, Is.SameAs(guard));
            yield return Shoot(guard, target);
            Assert.That(door.IsOpen, Is.True);
            Assert.That(Object.FindFirstObjectByType<LevelExit>().IsReady, Is.True);
            Assert.That(player.TryEject(false), Is.True);
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
        private static IEnumerator Shoot(GuardEnemy guard, Target target)
        {
            var camera = Camera.main;
            var saved = camera.transform.position;
            var ray = camera.ScreenPointToRay(Input.mousePosition);
            Assert.That(new Plane(Vector3.up, 0f).Raycast(ray, out float distance), Is.True);
            var offset = target.transform.position - ray.GetPoint(distance); offset.y = 0f;
            camera.transform.position += offset;
            var ability = guard.GetComponent<ShootAbility>();
            int uses = ability.UsesLeft;
            ability.TryExecute();
            camera.transform.position = saved;
            Assert.That(ability.UsesLeft, Is.EqualTo(uses - 1));
            float deadline = Time.realtimeSinceStartup + 2f;
            while (!target.IsHit && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(target.IsHit, Is.True, "The shared Guard projectile must unlock the exit door.");
        }

        private static void Move(EnemyBase heavy, Vector3 position, Vector3 forward)
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
        private static void Set(object target, string name, object value)
        {
            for (var type = target.GetType(); type != null; type = type.BaseType)
            {
                var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null) { field.SetValue(target, value); return; }
            }
            throw new System.MissingFieldException(name);
        }
        private static void Invoke(object target, string name, object value) => target.GetType()
            .GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, new[] { value });
    }
}
