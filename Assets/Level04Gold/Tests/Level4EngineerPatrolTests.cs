using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Possession.Level04Gold.Tests
{
    public sealed class Level4EngineerPatrolTests
    {
        private EngineerEnemy[] engineers;
        private SpiritController spirit;

        [UnitySetUp]
        public IEnumerator LoadSavedLevel()
        {
            Time.timeScale = 1f;
            Time.captureFramerate = 60;
            EventBus.Clear();
            yield return SceneManager.LoadSceneAsync("Level_04_Gold");
            foreach (var reporter in Object.FindObjectsByType<FirebaseReporter>(FindObjectsSortMode.None))
                Object.DestroyImmediate(reporter.gameObject);
            foreach (var tracker in Object.FindObjectsByType<P5TimingTracker>(FindObjectsSortMode.None))
                tracker.PersistReports = false;
            foreach (var enemy in All<EnemyBase>()) enemy.enabled = false;
            foreach (var damage in All<EnemyContactDamage>()) damage.enabled = false;
            foreach (var pressure in All<Level4SentryPressure>()) pressure.enabled = false;
            spirit = All<SpiritController>().Single();
            spirit.SetVisible(false);
            engineers = All<EngineerEnemy>().OrderBy(e => e.transform.position.y).ToArray();
            Assert.That(engineers.Length, Is.EqualTo(2));
            yield return null;
            Physics.SyncTransforms();
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.captureFramerate = 0;
            Time.timeScale = 1f;
            var old = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Level4EngineerPatrolCleanup"));
            if (old.IsValid() && old.isLoaded) yield return SceneManager.UnloadSceneAsync(old);
            EventBus.Clear();
        }

        [Test]
        public void BothSavedEngineerRoutesUseGuardPaceAndClearTheActualRoomGeometry()
        {
            var guard = All<GuardEnemy>().First();
            Assert.That(engineers.Select(e => e.PatrolStartDelay).Distinct().Count(), Is.EqualTo(2),
                "Engineers should not start their patrols in lockstep.");
            foreach (var engineer in engineers)
            {
                Assert.That(engineer.MoveSpeed, Is.EqualTo(guard.MoveSpeed));
                Assert.That(engineer.PatrolSpeed, Is.EqualTo(guard.PatrolSpeed));
                Assert.That(Read<float>(engineer, "turnSpeed"), Is.EqualTo(Read<float>(guard, "turnSpeed")));
                Assert.That(Read<float>(engineer, "waypointWaitTime"), Is.EqualTo(Read<float>(guard, "waypointWaitTime")));
                Assert.That(engineer.HasHomeBounds, Is.True);
                var route = Route(engineer);
                Assert.That(route.Length, Is.EqualTo(4), engineer.name + " needs a complete patrol loop.");
                Assert.That(route.All(p => p != null), Is.True);
                Assert.That(Vector3.Cross(route[1].position - route[0].position,
                    route[2].position - route[1].position).magnitude, Is.GreaterThan(1f),
                    engineer.name + " must circulate around the room rather than pace along one line.");
                var body = engineer.GetComponent<CharacterController>();
                float radius = body.radius * engineer.transform.lossyScale.x * .98f;
                float shaft = body.height * engineer.transform.lossyScale.y * .5f - radius;
                for (int i = 0; i < route.Length; i++)
                {
                    Vector3 center = route[i].position + engineer.transform.TransformVector(body.center);
                    Assert.That(engineer.IsInsideHome(center), Is.True, engineer.name + " waypoint outside room.");
                    Assert.That(Physics.CheckCapsule(center + Vector3.up * shaft, center - Vector3.up * shaft,
                        radius, (1 << 0) | (1 << 7), QueryTriggerInteraction.Ignore), Is.False,
                        engineer.name + " waypoint intersects a wall, stair or security console.");
                    Vector3 delta = route[(i + 1) % route.Length].position - route[i].position;
                    bool blocked = Physics.CapsuleCast(center + Vector3.up * shaft, center - Vector3.up * shaft,
                        radius, delta.normalized, out var hit, delta.magnitude,
                        (1 << 0) | (1 << 7), QueryTriggerInteraction.Ignore);
                    Assert.That(blocked, Is.False, engineer.name + " patrol blocked by "
                        + (hit.collider != null ? hit.collider.name : "unknown geometry"));
                }
            }
        }

        [UnityTest]
        public IEnumerator BothEngineersCompleteTheirSavedLoopsWithRealPausesAndRoomContainment()
        {
            var routes = engineers.Select(Route).ToArray();
            var lastArrival = Enumerable.Repeat(-1, engineers.Length).ToArray();
            var arrivals = new int[engineers.Length];
            var idleTime = new float[engineers.Length];
            var pauseObserved = new bool[engineers.Length];
            var idlePositions = engineers.Select(e => e.transform.position).ToArray();
            var initialFacing = engineers.Select(e => e.transform.rotation).ToArray();
            var maximumTurn = new float[engineers.Length];
            foreach (var engineer in engineers) engineer.enabled = true;
            float deadline = Time.time + 30f;
            while (arrivals.Any(n => n < 5) && Time.time < deadline)
            {
                for (int i = 0; i < engineers.Length; i++)
                {
                    var engineer = engineers[i];
                    Assert.That(engineer.IsInsideHome(engineer.transform.position), Is.True,
                        engineer.name + " left its home room during its ordinary patrol.");
                    maximumTurn[i] = Mathf.Max(maximumTurn[i],
                        Quaternion.Angle(initialFacing[i], engineer.transform.rotation));
                    for (int j = 0; j < routes[i].Length; j++)
                    {
                        if (HorizontalDistance(engineer.transform.position, routes[i][j].position) > .35f
                            || lastArrival[i] == j) continue;
                        lastArrival[i] = j;
                        arrivals[i]++;
                    }
                    if (engineer.IsIdle)
                    {
                        if (idleTime[i] == 0f) idlePositions[i] = engineer.transform.position;
                        Assert.That(HorizontalDistance(idlePositions[i], engineer.transform.position), Is.LessThan(.05f),
                            engineer.name + " should stop to look around at a patrol pause.");
                        idleTime[i] += Time.deltaTime;
                        if (arrivals[i] >= 2 && idleTime[i] > .25f) pauseObserved[i] = true;
                    }
                    else idleTime[i] = 0f;
                }
                yield return null;
            }
            for (int i = 0; i < engineers.Length; i++)
            {
                Assert.That(arrivals[i], Is.GreaterThanOrEqualTo(5),
                    engineers[i].name + " stalled before completing its full saved patrol loop.");
                Assert.That(pauseObserved[i], Is.True, engineers[i].name + " never stopped to look around.");
                Assert.That(maximumTurn[i], Is.GreaterThan(60f), engineers[i].name + " never turned through its loop.");
                var body = engineers[i].GetComponent<CharacterController>();
                Assert.That(body.isGrounded, Is.True);
                Assert.That(body.bounds.min.y, Is.EqualTo(i == 0 ? 0f : 5f).Within(.12f));
            }
        }

        [UnityTest]
        public IEnumerator RealPlayerPossessionStopsEachPatrolAndManualEjectRestoresIt()
        {
            var possession = spirit.GetComponent<PossessionController>();
            var charges = spirit.GetComponent<PossessionCharge>();
            foreach (var engineer in engineers)
            {
                while (charges.TryCollect()) { }
                spirit.SetVisible(false);
                Vector3 patrolStartedAt = engineer.transform.position;
                engineer.enabled = true;
                float walkingDeadline = Time.time + 6f;
                while (HorizontalDistance(engineer.transform.position, patrolStartedAt) <= .8f
                    && Time.time < walkingDeadline) yield return null;
                Assert.That(HorizontalDistance(engineer.transform.position, patrolStartedAt), Is.GreaterThan(.8f),
                    engineer.name + " must be actively walking before testing the possession handover.");
                Vector3 behind = engineer.transform.position - engineer.transform.forward * 2.5f;
                behind.y = engineer.transform.position.y;
                spirit.RestoreAt(behind);
                Physics.SyncTransforms();
                Assert.That(engineer.CanSee(spirit.transform), Is.False,
                    "The possession fixture must approach from behind the Engineer's current facing.");
                int chargeCount = charges.Count;
                Assert.That(possession.TryPossess(engineer), Is.True, engineer.name);
                Assert.That(charges.Count, Is.EqualTo(chargeCount - 1));
                Assert.That(possession.CurrentState, Is.EqualTo(PlayerState.Possessed_Engineer));
                Vector3 possessedAt = engineer.transform.position;
                yield return new WaitForSeconds(.6f);
                Assert.That(HorizontalDistance(engineer.transform.position, possessedAt), Is.LessThan(.03f),
                    "The Engineer must stop following its route while the player has control.");
                Assert.That(possession.TryEject(false), Is.True);
                Assert.That(engineer.IsPossessed || engineer.IsHostile, Is.False);
                Assert.That(possession.IsEjectShielded, Is.True);
                spirit.SetVisible(false);
                Vector3 releasedAt = engineer.transform.position;
                float deadline = Time.time + 5f;
                while (HorizontalDistance(engineer.transform.position, releasedAt) < 1f && Time.time < deadline)
                    yield return null;
                Assert.That(HorizontalDistance(engineer.transform.position, releasedAt), Is.GreaterThanOrEqualTo(1f),
                    engineer.name + " did not resume its normal patrol after manual ejection.");
                Assert.That(engineer.IsInsideHome(engineer.transform.position), Is.True);
                engineer.enabled = false;
                // The shared cooldown remains active even when no Engineer is being controlled.
                yield return new WaitForSeconds(2.05f);
            }
        }

        private static float HorizontalDistance(Vector3 a, Vector3 b)
        { a.y = b.y = 0f; return Vector3.Distance(a, b); }

        private static Transform[] Route(EngineerEnemy engineer) => Read<Transform[]>(engineer, "waypoints");

        private static T[] All<T>() where T : Component => SceneManager.GetActiveScene().GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<T>(true)).ToArray();

        private static T Read<T>(object source, string name)
        {
            for (var type = source.GetType(); type != null; type = type.BaseType)
            {
                var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field != null) return (T)field.GetValue(source);
            }
            throw new MissingFieldException(source.GetType().Name, name);
        }
    }
}
