using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Possession.Tests
{
    public sealed class ExpandedCampaignTests
    {
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = 1f;
            var old = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("SpacingCleanup"));
            yield return SceneManager.UnloadSceneAsync(old);
            EventBus.Clear();
        }

        [UnityTest]
        public IEnumerator FirstStageHasWideRoomsAndSealedPuzzleOpenings()
        {
            yield return Load(CampaignLevels.FirstScene);
            Size("Entry_Floor", 12f, 28f);
            Size("Fork_Floor", 48f, 16f);
            Size("Left_Floor", 16f, 36f);
            Size("Right_Floor", 16f, 68f);
            foreach (string name in new[] {"BreakableWall_Entry", "BreakableWall_01", "ExitDoor"})
            {
                var collider = GameObject.Find(name).GetComponent<Collider>();
                Assert.That(collider.bounds.size.x, Is.EqualTo(6.4f).Within(.02f));
                for (float x = -2.8f; x <= 2.8f; x += .7f)
                    Assert.That(collider.Raycast(new Ray(collider.bounds.center + new Vector3(x, 0f, -4f), Vector3.forward), out _, 8f), Is.True,
                        name + " must still seal its wider opening.");
            }
            ClearTurningArea(new Vector3(2f, 1.6f, 10f));
            ClearTurningArea(new Vector3(0f, 1.6f, 36f));
        }

        [UnityTest]
        public IEnumerator HuntedHasLargerRoomsAndSixMetrePassagesAndExpandedCheckpoint()
        {
            yield return Load(CampaignLevels.SecondScene);
            Size("R1_Floor", 16f, 22.4f);
            Size("R3_Floor", 22.4f, 16f);
            Size("Hall_0", 9.6f, 6.4f);
            Size("Hall_1", 6.4f, 9.6f);
            foreach (string name in new[] {"GN", "GS"})
                Assert.That(GameObject.Find(name).GetComponent<Collider>().bounds.size.z, Is.EqualTo(6.4f).Within(.02f));
            foreach (string name in new[] {"BN", "BS"})
                Assert.That(GameObject.Find(name).GetComponent<Collider>().bounds.size.x, Is.EqualTo(6.4f).Within(.02f));
            var checkpoint = GameObject.Find("Courtyard checkpoint").GetComponent<BoxCollider>();
            Assert.That(checkpoint.bounds.size.x, Is.EqualTo(18.6f).Within(.02f));
            Assert.That(checkpoint.bounds.size.z, Is.EqualTo(21.6f).Within(.02f));
            ClearTurningArea(Map(12f, 13.5f) + Vector3.up * .45f);
            var director = Object.FindFirstObjectByType<Level02Director>();director.MarkCheckpoint();director.RetryCourtyard();
            yield return null;yield return null;
            director = Object.FindFirstObjectByType<Level02Director>();
            Assert.That(director.CheckpointReached, Is.True);
            Assert.That(Vector3.Distance(director.Spirit.transform.position, Map(10f, 14f)), Is.LessThan(.3f));
            director.FullRestart();yield return null;yield return null;
            Assert.That(Object.FindFirstObjectByType<Level02Director>().CheckpointReached, Is.False);
        }

        [UnityTest]
        public IEnumerator SpaceDoesNotEnlargeActorsPickupsTargetsOrSharedRanges()
        {
            foreach (string scene in new[] {CampaignLevels.FirstScene, CampaignLevels.SecondScene})
            {
                yield return Load(scene);
                Assert.That(CampaignLayoutScale.ForScene(SceneManager.GetActiveScene()), Is.EqualTo(2f));
                var spirit = Object.FindFirstObjectByType<SpiritController>();
                Assert.That(spirit.transform.lossyScale, Is.EqualTo(Vector3.one));
                Assert.That(spirit.CurrentHP, Is.EqualTo(5));
                Assert.That(Read<float>(spirit.GetComponent<PossessionController>(), "possessRange"), Is.EqualTo(5f));
                foreach (var enemy in Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))
                {
                    float scale = enemy is HeavyEnemy ? 1.3f : 1f;
                    Assert.That(enemy.transform.lossyScale.x, Is.EqualTo(scale).Within(.001f));
                    Assert.That(enemy.GetComponent<CharacterController>().radius, Is.EqualTo(.5f));
                    Assert.That(enemy.DetectionRange, Is.LessThanOrEqualTo(5f));
                }
                foreach (var target in Object.FindObjectsByType<Target>(FindObjectsSortMode.None))
                    Assert.That(target.transform.lossyScale.x, Is.EqualTo(1.2f).Within(.001f));
                foreach (var pickup in Object.FindObjectsByType<PossessionChargePickup>(FindObjectsSortMode.None))
                    Assert.That(pickup.transform.lossyScale.x, Is.EqualTo(1f));
                Assert.That(Read<float>(GameManager.Instance, "levelTime"), Is.EqualTo(scene == CampaignLevels.FirstScene ? 180f : 240f));
            }
        }

        [UnityTest]
        public IEnumerator FirstStageSupportsRealPossessionShotsSmashesCrossingsAndExit()
        {
            yield return Load(CampaignLevels.FirstScene);
            var entry = GameObject.Find("BreakableWall_Entry").GetComponent<BreakableWall>();
            var heavy = GameObject.Find("Heavy_03").GetComponent<HeavyEnemy>();
            yield return SmashAndCross(heavy, entry);
            var target = Object.FindFirstObjectByType<Target>();
            var guard = GameObject.Find("Guard_03").GetComponent<GuardEnemy>();
            yield return Shoot(guard, target, target.transform.position + Vector3.forward * 4f);
            var door = Object.FindFirstObjectByType<Door>();Assert.That(door.IsOpen, Is.True);
            yield return Eject();
            var wall = GameObject.Find("BreakableWall_01").GetComponent<BreakableWall>();
            yield return SmashAndCross(GameObject.Find("Heavy_01").GetComponent<HeavyEnemy>(), wall);
            var exit = Object.FindFirstObjectByType<LevelExit>();Assert.That(exit.IsReady, Is.True);
            var spirit = Object.FindFirstObjectByType<SpiritController>();spirit.RestoreAt(exit.transform.position);Physics.SyncTransforms();
            yield return new WaitForSecondsRealtime(.1f);
            Assert.That(GameManager.Instance.CurrentState, Is.EqualTo(LevelState.Complete));
        }

        [UnityTest]
        public IEnumerator BothHuntedRoutesStillWorkWithLocalHostsAndTheSharedChargeSystem()
        {
            foreach (bool north in new[] {true, false})
            {
                yield return Load(CampaignLevels.SecondScene);
                var director = Object.FindFirstObjectByType<Level02Director>();
                Assert.That(director.CanExit, Is.False);
                var target = GameObject.Find(north ? "TN" : "TS").GetComponent<Target>();
                var guard = GameObject.Find(north ? "Guard_North" : "Guard_South").GetComponent<GuardEnemy>();
                yield return Shoot(guard, target, target.transform.position + Vector3.left * 4f);
                yield return Eject();
                var wall = GameObject.Find(north ? "BN" : "BS").GetComponent<BreakableWall>();
                var heavy = GameObject.Find(north ? "Heavy_North" : "Heavy_Courtyard").GetComponent<HeavyEnemy>();
                yield return SmashAndCross(heavy, wall);
                Assert.That(north ? director.NorthReady : director.SouthReady, Is.True);
                Assert.That(north ? director.SouthReady : director.NorthReady, Is.False);
                Assert.That(director.Charge.SuccessfulPossessions, Is.EqualTo(2));
                var exit = Object.FindFirstObjectByType<Level02Exit>();director.Spirit.RestoreAt(exit.transform.position);Physics.SyncTransforms();
                Assert.That(exit.TryFinish(director.Spirit.GetComponent<Collider>()), Is.True);
            }
        }

        private static IEnumerator Load(string scene)
        {
            Time.timeScale = 1f;EventBus.Clear();yield return SceneManager.LoadSceneAsync(scene);yield return null;
            foreach (var e in Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None)) e.enabled = false;
            foreach (var e in Object.FindObjectsByType<EnemyContactDamage>(FindObjectsSortMode.None)) e.enabled = false;
            foreach (var e in Object.FindObjectsByType<HuntedEnemyPressure>(FindObjectsSortMode.None)) e.enabled = false;
        }
        private static void Size(string name, float x, float z)
        {
            var bounds = GameObject.Find(name).GetComponent<Renderer>().bounds;
            Assert.That(bounds.size.x, Is.EqualTo(x).Within(.02f), name);
            Assert.That(bounds.size.z, Is.EqualTo(z).Within(.02f), name);
        }
        private static void ClearTurningArea(Vector3 at)
        {
            Assert.That(Physics.CheckBox(at, new Vector3(1.4f, .5f, 1.4f), Quaternion.identity, 1 << 0, QueryTriggerInteraction.Ignore),
                Is.False, "Expected unobstructed 2.8 metre turning space at " + at);
        }
        private static Vector3 Map(float x, float y) => CampaignLayoutScale.FromOriginal(
            SceneManager.GetActiveScene(), new Vector3((x - 17f) * 1.6f, 1.15f, (12f - y) * 1.6f));
        private static T Read<T>(object value, string name) => (T)value.GetType().GetField(name,
            BindingFlags.Instance | BindingFlags.NonPublic).GetValue(value);
        private static void Possess(EnemyBase host, Vector3 position, Vector3 forward)
        {
            var spirit = Object.FindFirstObjectByType<SpiritController>();var charge = spirit.GetComponent<PossessionCharge>();
            if (!charge.HasCharge)
            {
                var pickup = Object.FindObjectsByType<PossessionChargePickup>(FindObjectsSortMode.None).First(p => p.IsReady);
                spirit.RestoreAt(pickup.transform.position + Vector3.up);Physics.SyncTransforms();
                Assert.That(pickup.TryCollect(charge), Is.True);
            }
            var cc = host.GetComponent<CharacterController>();cc.enabled = false;position.y = host.transform.position.y;
            host.transform.SetPositionAndRotation(position, Quaternion.LookRotation(forward));cc.enabled = true;
            var behind = position - forward * 1.8f;behind.y = 1.15f;spirit.RestoreAt(behind);Physics.SyncTransforms();
            var pc = spirit.GetComponent<PossessionController>();
            typeof(PossessionController).GetMethod("Possess", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(pc, new object[] {host});
            Assert.That(pc.PossessedEnemy, Is.SameAs(host));
        }
        private static IEnumerator Eject()
        {
            var pc = Object.FindFirstObjectByType<PossessionController>();Assert.That(pc.TryEject(false), Is.True);
            yield return new WaitForSeconds(2.05f);
        }
        private static IEnumerator Shoot(GuardEnemy guard, Target target, Vector3 from)
        {
            Possess(guard, from, (target.transform.position - from).normalized);
            var camera = Camera.main;var saved = camera.transform.position;var ray = camera.ScreenPointToRay(Input.mousePosition);
            Assert.That(new Plane(Vector3.up, 0f).Raycast(ray, out float distance), Is.True);
            var delta = target.transform.position - ray.GetPoint(distance);delta.y = 0f;camera.transform.position += delta;
            guard.GetComponent<ShootAbility>().TryExecute();camera.transform.position = saved;
            float deadline = Time.time + 1.5f;while (!target.IsHit && Time.time < deadline) yield return null;
            Assert.That(target.IsHit, Is.True, "Shared projectile did not reach " + target.name);
        }
        private static IEnumerator SmashAndCross(HeavyEnemy heavy, BreakableWall wall)
        {
            Vector3 forward = wall.name == "BreakableWall_Entry" ? Vector3.forward : Vector3.back;
            Vector3 start = wall.transform.position - forward * 2.1f;
            Possess(heavy, start, forward);heavy.GetComponent<SmashAbility>().TryExecute();
            Assert.That(wall.CurrentHP, Is.LessThanOrEqualTo(0), wall.name);
            Vector3 end = heavy.transform.position + forward * 4.5f;float deadline = Time.time + 4f;
            while (Vector3.Dot(end - heavy.transform.position, forward) > .1f && Time.time < deadline)
            {
                heavy.GetComponent<CharacterController>().Move(forward * heavy.MoveSpeed * Time.deltaTime);yield return null;
            }
            Assert.That(Vector3.Dot(end - heavy.transform.position, forward), Is.LessThan(.15f), "Broken wall must open a walkable passage.");
            yield return Eject();
        }
    }
}
