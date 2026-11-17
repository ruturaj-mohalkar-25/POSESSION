using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Possession.Tests
{
    public sealed class CampaignPressurePlacementTests
    {
        private sealed class Placement
        {
            public readonly string Name, Floor, AdjacentFloor, Baseline;
            public readonly bool WestEntrance;
            public Placement(string name, string floor, string adjacentFloor, string baseline, bool westEntrance)
            { Name = name; Floor = floor; AdjacentFloor = adjacentFloor; Baseline = baseline; WestEntrance = westEntrance; }
        }

        [SetUp]
        public void Setup()
        {
            Time.timeScale = 1f;
            Time.captureFramerate = 60;
            EventBus.Clear();
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = 1f;
            Time.captureFramerate = 0;
            var old = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("CampaignPressureCleanup"));
            if (old.IsValid() && old.isLoaded) yield return SceneManager.UnloadSceneAsync(old);
            EventBus.Clear();
        }

        [UnityTest]
        public IEnumerator SavedLevelsAddOnlyTheApprovedGuardsAndKeepStartingResources()
        {
            foreach (string scene in Scenes())
            {
                yield return Load(scene);
                bool hunted = scene == CampaignLevels.SecondScene;
                Assert.That(All<GuardEnemy>().Length, Is.EqualTo(hunted ? 7 : 6));
                Assert.That(All<HeavyEnemy>().Length, Is.EqualTo(hunted ? 3 : 2));
                Assert.That(All<EnemyBase>().Length, Is.EqualTo(hunted ? 10 : 8));
                Assert.That(All<PossessionChargePickup>().Length, Is.EqualTo(hunted ? 8 : 9));
                Assert.That(All<HUDManager>().Length, Is.EqualTo(1));
                var spirit = All<SpiritController>().Single();
                var charge = spirit.GetComponent<PossessionCharge>();
                Assert.That(spirit.MaxHP, Is.EqualTo(5));
                Assert.That(charge.Count, Is.EqualTo(1));
                Assert.That(charge.MaxCharges, Is.EqualTo(3));
                Assert.That(spirit.GetComponent<PossessionController>().EjectProtectionSeconds, Is.EqualTo(2f));
                foreach (var placement in Placements(scene)) Assert.That(All<GuardEnemy>().Count(g => g.name == placement.Name), Is.EqualTo(1));
                AssertSharedTuningAndInitialFacing(scene);
            }
        }

        [UnityTest]
        public IEnumerator AddedFourPointPatrolsFitTheirRoomsAndClearRealGeometryPickupsAndDoors()
        {
            foreach (string scene in Scenes())
            {
                yield return Load(scene);
                Physics.SyncTransforms();
                foreach (var placement in Placements(scene))
                {
                    var guard = Guard(placement.Name);
                    var floor = Floor(placement.Floor);
                    var route = Read<Transform[]>(guard, "waypoints");
                    Assert.That(route.Length, Is.EqualTo(4), guard.name);
                    Assert.That(route.All(p => p != null), Is.True);
                    Assert.That(Vector3.Cross(route[1].position - route[0].position,
                        route[2].position - route[1].position).magnitude, Is.GreaterThan(1f), "Use a real loop rather than a line.");
                    Assert.That(guard.HasHomeBounds, Is.True);
                    Assert.That(guard.HomeBounds.center.x, Is.EqualTo(floor.bounds.center.x).Within(.01f));
                    Assert.That(guard.HomeBounds.center.z, Is.EqualTo(floor.bounds.center.z).Within(.01f));
                    Assert.That(guard.HomeBounds.size.x, Is.EqualTo(floor.bounds.size.x).Within(.01f));
                    Assert.That(guard.HomeBounds.size.z, Is.EqualTo(floor.bounds.size.z).Within(.01f));
                    AssertBodyFits(guard, guard.transform.position, floor);
                    AssertSpawnSpacing(guard);
                    for (int i = 0; i < route.Length; i++)
                    {
                        Vector3 from = route[i].position, to = route[(i + 1) % route.Length].position;
                        AssertBodyFits(guard, from, floor);
                        Capsule(guard, from, out var top, out var bottom, out float radius);
                        Vector3 delta = to - from;
                        var obstructions = Physics.CapsuleCastAll(top, bottom, radius, delta.normalized,
                            delta.magnitude, ~0, QueryTriggerInteraction.Ignore).Where(h => SolidWorld(h.collider)).ToArray();
                        Assert.That(obstructions.Select(h => h.collider.name), Is.Empty, guard.name + " patrol segment is blocked.");
                        int samples = Mathf.CeilToInt(delta.magnitude / .4f);
                        for (int step = 0; step <= samples; step++)
                            AssertBodyFits(guard, Vector3.Lerp(from, to, step / (float)samples), floor);
                    }
                }
            }
        }

        [UnityTest]
        public IEnumerator AddedGuardsReallyPatrolWithStaggeredStartsAndRejectAdjacentRoomPursuit()
        {
            foreach (string scene in Scenes())
            {
                yield return Load(scene);
                All<SpiritController>().Single().SetVisible(false);
                var guards = Placements(scene).Select(s => Guard(s.Name)).ToArray();
                var routes = guards.Select(g => Read<Transform[]>(g, "waypoints")).ToArray();
                var origins = guards.Select(g => g.transform.position).ToArray();
                var previous = origins.ToArray();
                var firstMove = Enumerable.Repeat(-1f, guards.Length).ToArray();
                var lastWaypoint = Enumerable.Repeat(-1, guards.Length).ToArray();
                var arrivals = new int[guards.Length];
                var visited = new int[guards.Length];
                var distance = new float[guards.Length];
                foreach (var guard in guards) guard.enabled = true;
                float start = Time.time, realStart = Time.realtimeSinceStartup;
                while (arrivals.Any(n => n < 5) && Time.time - start < 28f && Time.realtimeSinceStartup - realStart < 45f)
                {
                    for (int i = 0; i < guards.Length; i++)
                    {
                        Vector3 at = guards[i].transform.position;
                        distance[i] += FlatDistance(at, previous[i]);
                        previous[i] = at;
                        if (firstMove[i] < 0f && FlatDistance(at, origins[i]) > .12f) firstMove[i] = Time.time - start;
                        Assert.That(guards[i].IsInsideHome(at), Is.True, guards[i].name + " escaped its room while patrolling.");
                        Assert.That(guards[i].IsChasing, Is.False, "This must measure patrol movement, not a chase.");
                        for (int p = 0; p < routes[i].Length; p++)
                            if (FlatDistance(at, routes[i][p].position) <= .35f && lastWaypoint[i] != p)
                            {
                                lastWaypoint[i] = p;
                                arrivals[i]++;
                                visited[i] |= 1 << p;
                            }
                    }
                    yield return null;
                }
                for (int i = 0; i < guards.Length; i++)
                {
                    float perimeter = Enumerable.Range(0, routes[i].Length)
                        .Sum(p => FlatDistance(routes[i][p].position, routes[i][(p + 1) % routes[i].Length].position));
                    Assert.That(arrivals[i], Is.GreaterThanOrEqualTo(5), guards[i].name + " stalled before a full loop.");
                    Assert.That(visited[i], Is.EqualTo(15), guards[i].name + " missed a patrol corner.");
                    Assert.That(distance[i], Is.GreaterThan(perimeter * .7f), "A static or oscillating body must not satisfy the loop check.");
                    Assert.That(firstMove[i], Is.GreaterThan(1f), "The authored start delay must affect real movement.");
                }
                Assert.That(Mathf.Abs(firstMove[0] - firstMove[1]), Is.GreaterThan(.4f), "Real patrol launches must be staggered.");
                foreach (var guard in guards) guard.enabled = false;
                var spirit = All<SpiritController>().Single();
                foreach (var placement in Placements(scene))
                {
                    var guard = Guard(placement.Name);
                    var adjacent = Floor(placement.AdjacentFloor).bounds.center;
                    spirit.RestoreAt(new Vector3(adjacent.x, guard.transform.position.y, adjacent.z));
                    Physics.SyncTransforms();
                    Assert.That(guard.IsInsideHome(spirit.transform.position), Is.False);
                    Assert.That(guard.CanPursue(spirit.transform), Is.False);
                    guard.OnForcedEject(.6f);
                    guard.enabled = true;
                    yield return new WaitForSeconds(.3f);
                    Assert.That(guard.IsChasing, Is.False);
                    Assert.That(guard.CanPursue(spirit.transform), Is.False);
                    Assert.That(guard.IsInsideHome(guard.transform.position), Is.True);
                    guard.enabled = false;
                }

            }
        }

        [UnityTest]
        public IEnumerator ExtraBodiesStillCostOneChargeAndEjectWithTwoSecondsOfRealProtection()
        {
            foreach (string scene in Scenes())
            {
                yield return Load(scene);
                var spirit = All<SpiritController>().Single();
                var player = spirit.GetComponent<PossessionController>();
                var charge = spirit.GetComponent<PossessionCharge>();
                foreach (var placement in Placements(scene))
                {
                    var guard = Guard(placement.Name);
                    if (!charge.HasCharge) Assert.That(charge.TryCollect(), Is.True);
                    Assert.That(charge.Count, Is.EqualTo(1));
                    spirit.RestoreAt(guard.transform.position - guard.transform.forward * 2.5f);
                    Physics.SyncTransforms();
                    Assert.That(player.TryPossess(guard), Is.True, guard.name + " needs a real clear rear approach.");
                    Assert.That(charge.Count, Is.Zero);
                    Assert.That(player.TryEject(false), Is.True);
                    Assert.That(spirit.DamageProtectionRemaining, Is.EqualTo(2f).Within(.03f));
                    int hp = spirit.CurrentHP;
                    spirit.TakeDamage(10);
                    Assert.That(spirit.CurrentHP, Is.EqualTo(hp));
                    yield return new WaitForSeconds(2.05f);
                    Assert.That(spirit.IsDamageProtected, Is.False);
                    spirit.TakeDamage(1);
                    Assert.That(spirit.CurrentHP, Is.EqualTo(hp - 1));
                }
            }
        }

        private static void AssertSharedTuningAndInitialFacing(string scene)
        {
            var specs = Placements(scene);
            var delays = specs.Select(s => Guard(s.Name).PatrolStartDelay).ToArray();
            Assert.That(delays.All(d => d > 1f), Is.True, "New pressure must allow an initial planning window.");
            Assert.That(delays.Distinct().Count(), Is.EqualTo(2), "The new patrols must not launch together.");
            foreach (var placement in specs)
            {
                var guard = Guard(placement.Name);
                var baseline = Guard(placement.Baseline);
                Assert.That(guard.MoveSpeed, Is.EqualTo(5f));
                Assert.That(guard.PatrolSpeed, Is.EqualTo(2.5f));
                Assert.That(guard.DetectionRange, Is.EqualTo(9f));
                Assert.That(guard.VisionAngle, Is.EqualTo(75f));
                Assert.That(guard.CurrentHP, Is.EqualTo(3));
                Assert.That(Vector3.Distance(guard.transform.lossyScale, baseline.transform.lossyScale), Is.LessThan(.0001f),
                    guard.name + " must preserve the baseline Guard's physical scale.");
                foreach (string field in new[] { "moveSpeed", "maxHP", "patrolSpeedMultiplier", "turnSpeed", "waypointWaitTime", "chaseStopDistance" })
                    Assert.That(Read<object>(guard, field), Is.EqualTo(Read<object>(baseline, field)), guard.name + " changed shared " + field);
                var body = guard.GetComponent<CharacterController>();
                var originalBody = baseline.GetComponent<CharacterController>();
                Assert.That(body.radius, Is.EqualTo(originalBody.radius));
                Assert.That(body.height, Is.EqualTo(originalBody.height));
                var attack = guard.GetComponent<EnemyContactDamage>();
                var originalAttack = baseline.GetComponent<EnemyContactDamage>();
                Assert.That(attack, Is.Not.Null);
                foreach (string field in new[] { "damage", "firstAttackDelay", "spotReactionTime", "shootRange", "shootInterval", "touchCooldown" })
                    Assert.That(Read<object>(attack, field), Is.EqualTo(Read<object>(originalAttack, field)), guard.name + " changed shared " + field);
                Assert.That(Read<GameObject>(attack, "projectilePrefab"), Is.SameAs(Read<GameObject>(originalAttack, "projectilePrefab")));
                var muzzle = Read<Transform>(attack, "firePoint");
                Assert.That(muzzle != null && muzzle.IsChildOf(guard.transform), Is.True, "The clone must fire from its own muzzle.");
                Assert.That(guard.GetComponent<ShootAbility>().MaxUses, Is.EqualTo(baseline.GetComponent<ShootAbility>().MaxUses));
                var hunted = guard.GetComponent<HuntedEnemyPressure>();
                var originalHunted = baseline.GetComponent<HuntedEnemyPressure>();
                Assert.That(hunted != null, Is.EqualTo(originalHunted != null));
                if (hunted != null)
                    foreach (string field in new[] { "windup", "hitInterval" })
                        Assert.That(Read<float>(hunted, field), Is.EqualTo(Read<float>(originalHunted, field)));
                var area = Floor(placement.Floor).bounds;
                Vector3 entrance = placement.WestEntrance
                    ? new Vector3(area.min.x, guard.transform.position.y, area.center.z)
                    : new Vector3(area.center.x, guard.transform.position.y, area.min.z);
                Assert.That(Vector3.Angle(guard.transform.forward, entrance - guard.transform.position),
                    Is.GreaterThan(guard.VisionAngle * .5f + 10f), guard.name + " should not cover its entry with its first cone.");
            }
        }

        private static void AssertBodyFits(GuardEnemy guard, Vector3 at, BoxCollider floor)
        {
            Capsule(guard, at, out var top, out var bottom, out float radius);
            var centre = (top + bottom) * .5f;
            Assert.That(guard.IsInsideHome(at), Is.True, guard.name);
            Assert.That(centre.x - radius, Is.GreaterThanOrEqualTo(floor.bounds.min.x));
            Assert.That(centre.x + radius, Is.LessThanOrEqualTo(floor.bounds.max.x));
            Assert.That(centre.z - radius, Is.GreaterThanOrEqualTo(floor.bounds.min.z));
            Assert.That(centre.z + radius, Is.LessThanOrEqualTo(floor.bounds.max.z));
            var overlaps = Physics.OverlapCapsule(top, bottom, radius, ~0, QueryTriggerInteraction.Ignore).Where(SolidWorld).ToArray();
            Assert.That(overlaps.Select(c => c.name), Is.Empty, guard.name + " overlaps static geometry at " + at);
            var support = Physics.RaycastAll(centre + Vector3.up * .2f, Vector3.down,
                    Vector3.Distance(top, bottom) * .5f + radius + .7f, ~0, QueryTriggerInteraction.Ignore)
                .Where(h => SolidWorld(h.collider) && h.normal.y > .65f).OrderBy(h => h.distance).ToArray();
            Assert.That(support, Is.Not.Empty, guard.name + " has no supporting floor at " + at);
            Assert.That(support[0].collider, Is.SameAs(floor));
            float foot = bottom.y - radius;
            Assert.That(foot - support[0].point.y, Is.InRange(-.01f, .2f));
        }

        private static void AssertSpawnSpacing(GuardEnemy guard)
        {
            Capsule(guard, guard.transform.position, out _, out _, out float radius);
            foreach (var door in All<Door>())
            {
                var bounds = door.GetComponent<Collider>().bounds;
                var point = bounds.ClosestPoint(new Vector3(guard.transform.position.x, bounds.center.y, guard.transform.position.z));
                Assert.That(FlatDistance(point, guard.transform.position), Is.GreaterThan(radius + .3f), guard.name + " starts too close to a puzzle door.");
            }
            foreach (var pickup in All<PossessionChargePickup>())
            {
                var trigger = pickup.GetComponent<Collider>().bounds;
                float pickupRadius = Mathf.Max(trigger.extents.x, trigger.extents.z);
                Assert.That(FlatDistance(guard.transform.position, trigger.center), Is.GreaterThan(radius + pickupRadius + .15f),
                    guard.name + " must leave the pickup's collection area free.");
            }
        }

        private static void Capsule(GuardEnemy guard, Vector3 at, out Vector3 top, out Vector3 bottom, out float radius)
        {
            var body = guard.GetComponent<CharacterController>();
            var scale = guard.transform.lossyScale;
            radius = body.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            float shaft = Mathf.Max(0f, body.height * Mathf.Abs(scale.y) * .5f - radius);
            Vector3 centre = at + guard.transform.TransformVector(body.center);
            top = centre + Vector3.up * shaft;
            bottom = centre - Vector3.up * shaft;
        }

        private static bool SolidWorld(Collider collider) => collider != null && !collider.isTrigger
            && collider.GetComponentInParent<EnemyBase>() == null && collider.GetComponentInParent<SpiritController>() == null;
        private static BoxCollider Floor(string name) => All<BoxCollider>().Single(c => c.name == name);
        private static GuardEnemy Guard(string name) => All<GuardEnemy>().Single(g => g.name == name);
        private static float FlatDistance(Vector3 a, Vector3 b) => Vector3.ProjectOnPlane(a - b, Vector3.up).magnitude;
        private static string[] Scenes() => new[] { CampaignLevels.SecondScene, CampaignLevels.ThirdScene };
        private static Placement[] Placements(string scene) => scene == CampaignLevels.SecondScene
            ? new[] { new Placement("Guard_NorthStorage_Pressure", "R4_Floor", "R3_Floor", "Guard_North", true),
                new Placement("Guard_GateChamber_Pressure", "R6_Floor", "R5_Floor", "Guard_GateChamber", true) }
            : new[] { new Placement("Inspection_Pressure_Guard", "Inspection_Floor", "Guard_room_Floor", "Inspection_Guard", false),
                new Placement("Loading_Pressure_Guard", "Loading_room_Floor", "Cargo_room_Floor", "Cargo_room_Guard", false) };

        private static IEnumerator Load(string scene)
        {
            Time.timeScale = 1f;
            EventBus.Clear();
            yield return SceneManager.LoadSceneAsync(scene);
            foreach (var enemy in All<EnemyBase>()) enemy.enabled = false;
            foreach (var attack in All<EnemyContactDamage>()) attack.enabled = false;
            foreach (var pressure in All<HuntedEnemyPressure>()) pressure.enabled = false;
            foreach (var reporter in All<FirebaseReporter>()) Object.DestroyImmediate(reporter.gameObject);
            foreach (var tracker in All<P5TimingTracker>()) tracker.PersistReports = false;
            yield return null;
            yield return null;
        }

        private static T[] All<T>() where T : Object => Object.FindObjectsByType<T>(FindObjectsSortMode.None);
        private static T Read<T>(object target, string name)
        {
            for (var type = target.GetType(); type != null; type = type.BaseType)
            {
                var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);
                if (field != null) return (T)field.GetValue(target);
            }
            throw new MissingFieldException(target.GetType().Name, name);
        }
    }
}
