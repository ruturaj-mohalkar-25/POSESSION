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
    public sealed class Level4StairTraversalTests
    {
        private SpiritController spirit;
        private Rigidbody body;
        private CapsuleCollider capsule;
        private Level4StairInputDriver input;

        [UnitySetUp]
        public IEnumerator LoadSavedLevel()
        {
            Time.timeScale = 1f;
            EventBus.Clear();
            yield return SceneManager.LoadSceneAsync("Level_04_Gold");
            yield return null;
            spirit = Object.FindFirstObjectByType<SpiritController>();
            Assert.That(spirit, Is.Not.Null);
            body = spirit.GetComponent<Rigidbody>();
            capsule = spirit.GetComponent<CapsuleCollider>();
            Assert.That(body, Is.Not.Null);
            Assert.That(capsule, Is.Not.Null);
            foreach (var enemy in Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None)) enemy.enabled = false;
            foreach (var contact in Object.FindObjectsByType<EnemyContactDamage>(FindObjectsSortMode.None)) contact.enabled = false;
            foreach (var pressure in Object.FindObjectsByType<Level4SentryPressure>(FindObjectsSortMode.None)) pressure.enabled = false;
            foreach (var reporter in Object.FindObjectsByType<FirebaseReporter>(FindObjectsSortMode.None))
                Object.DestroyImmediate(reporter.gameObject);
            foreach (var tracker in Object.FindObjectsByType<P5TimingTracker>(FindObjectsSortMode.None)) tracker.PersistReports = false;
            input = spirit.gameObject.AddComponent<Level4StairInputDriver>();
            input.Spirit = spirit;
        }

        [UnityTearDown]
        public IEnumerator UnloadSavedLevel()
        {
            Time.timeScale = 1f;
            if (input != null) input.MoveInput = Vector2.zero;
            var old = SceneManager.GetActiveScene();
            var empty = SceneManager.CreateScene("Level4StairTraversalCleanup");
            SceneManager.SetActiveScene(empty);
            if (old.IsValid() && old.isLoaded) yield return SceneManager.UnloadSceneAsync(old);
            EventBus.Clear();
        }

        [UnityTest]
        public IEnumerator SharedSpiritCanTakeTheDirectGalleryEntranceAndClimbWithoutAJump()
        {
            spirit.RestoreAt(P(62f, 1.4f, 8f));
            Assert.That(spirit.enabled && capsule.enabled && body.useGravity && !body.isKinematic, Is.True);
            yield return Settle(.35f);
            float lowerHeight = body.position.y;
            var ramp = Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None)
                .Single(c => c.name == "Stair_Ramp_WalkSurface");
            var directEntry = P(74f, 1.4f, 8f);
            // The entrance stays at the gallery's actual side opening. The foot
            // and flight length may move while preserving this intuitive route.
            var topApproach = new Vector3(ramp.bounds.center.x, 6.4f, ramp.bounds.max.z + 1.25f);
            var landing = P(74f, 6.4f, 25f);
            yield return WalkTo(directEntry);
            yield return WalkTo(topApproach);
            yield return WalkTo(landing);
            yield return Settle(.2f);
            Assert.That(body.position.y - lowerHeight, Is.EqualTo(5f).Within(.2f), Diagnostic("direct stair rise", landing));
            Assert.That(spirit.IsGrounded, Is.True, Diagnostic("direct upper landing", landing));
            Assert.That(input.RampSupportSamples, Is.GreaterThan(10));
            yield return WalkTo(topApproach);
            yield return WalkTo(directEntry);
            yield return WalkTo(P(62f, 1.4f, 8f));
            yield return Settle(.25f);
            Assert.That(body.position.y, Is.EqualTo(lowerHeight).Within(.2f), Diagnostic("direct gallery return", P(62f, 1.4f, 8f)));
            Assert.That(spirit.IsGrounded, Is.True);
            Debug.Log($"Direct Spirit stair route: fixed={input.FixedSamples}, grounded={input.GroundedSamples}, "
                + $"ramp={input.RampSupportSamples}, height={input.LowestY:0.00}..{input.HighestY:0.00}m.");
        }

        [UnityTest]
        public IEnumerator SharedSpiritWalksFromGalleryAroundTheStairFootToUpperLandingAndBack()
        {
            // Stage in the gallery, never on the ramp. All later movement uses the
            // saved Spirit's own FixedUpdate, Rigidbody, capsule, gravity and friction.
            spirit.RestoreAt(P(62f, 1.4f, 8f));
            Assert.That(spirit.enabled && capsule.enabled && body.useGravity && !body.isKinematic, Is.True);
            yield return Settle(.35f);
            float lowerHeight = body.position.y;
            Assert.That(spirit.IsGrounded, Is.True, Diagnostic("gallery grounding", body.position));
            var outward = new[]
            {
                P(69.4f, 1.4f, 8f), P(69.4f, 1.4f, 3f), P(74f, 1.4f, 3f),
                P(74f, 6.4f, 19f), P(74f, 6.4f, 25f)
            };
            foreach (var waypoint in outward) yield return WalkTo(waypoint);
            yield return Settle(.2f);
            Assert.That(body.position.y - lowerHeight, Is.EqualTo(5f).Within(.2f), Diagnostic("upper landing rise", outward.Last()));
            Assert.That(spirit.IsGrounded, Is.True, Diagnostic("upper landing grounding", outward.Last()));
            Assert.That(input.RampSupportSamples, Is.GreaterThan(10),
                "The real Spirit must physically traverse the ramp surface; " + Diagnostic("ramp support", outward.Last()));
            Assert.That(input.HighestY, Is.GreaterThan(6f));

            foreach (var waypoint in outward.Reverse().Skip(1)) yield return WalkTo(waypoint);
            yield return WalkTo(P(62f, 1.4f, 8f));
            yield return Settle(.25f);
            Assert.That(body.position.y, Is.EqualTo(lowerHeight).Within(.2f), Diagnostic("gallery return height", P(62f, 1.4f, 8f)));
            Assert.That(spirit.IsGrounded, Is.True, Diagnostic("gallery return grounding", P(62f, 1.4f, 8f)));
            Assert.That(capsule.enabled && body.useGravity && !body.isKinematic && spirit.enabled, Is.True);
            Debug.Log($"Spirit stair round trip: fixed samples={input.FixedSamples}, grounded={input.GroundedSamples}, "
                + $"ramp support={input.RampSupportSamples}, height={input.LowestY:0.00}..{input.HighestY:0.00}m.");
        }

        [UnityTest]
        public IEnumerator SpiritCanStillJumpFromTheUpperFloorOntoCover()
        {
            var upper = GameObject.Find("Level04_Geometry").transform.Find("Upper floor");
            var cover = upper.GetComponentsInChildren<BoxCollider>().Single(c => c.name == "Cover_03");
            var start = cover.bounds.center;
            start.y = 6.4f;
            start.z = cover.bounds.max.z + 2f;
            spirit.RestoreAt(start);
            yield return Settle(.3f);
            spirit.RequestJump();
            yield return new WaitForSeconds(.3f);
            float until = Time.time + 1.5f;
            while (Time.time < until)
            {
                input.MoveInput = new Vector2(0, Mathf.Clamp((cover.bounds.center.z - body.position.z)
                    / (spirit.MoveSpeed * Time.fixedDeltaTime), -1, 1));
                yield return null;
            }
            input.MoveInput = Vector2.zero;
            Assert.That(spirit.IsGrounded, Is.True);
            Assert.That(capsule.bounds.min.y, Is.EqualTo(cover.bounds.max.y).Within(.12f));
        }

        [UnityTest]
        public IEnumerator SpiritCannotJumpFromUpperCoverOntoTheBoundaryWall()
        {
            var upper = GameObject.Find("Level04_Geometry").transform.Find("Upper floor");
            var cover = upper.GetComponentsInChildren<BoxCollider>().Single(c => c.name == "Cover_03");
            // This cover is reachable from the floor; use its top as the next jump's start.
            Vector3 start = cover.bounds.center;
            start.y = cover.bounds.max.y + capsule.bounds.extents.y + .03f;
            spirit.RestoreAt(start);
            yield return Settle(.3f);
            Assert.That(spirit.IsGrounded, Is.True);
            float boundaryZ = P(0,0,34).z;
            spirit.RequestJump();
            // Delay forward movement until the capsule clears the wall edge.
            yield return new WaitForSeconds(.35f);
            float until = Time.time + 2f;
            bool stoodOnWall = false;
            while (Time.time < until)
            {
                float delta = boundaryZ - body.position.z;
                input.MoveInput = new Vector2(0, Mathf.Clamp(delta / (spirit.MoveSpeed * Time.fixedDeltaTime), -1, 1));
                if (spirit.IsGrounded && capsule.bounds.min.y > 9.3f) stoodOnWall = true;
                yield return null;
            }
            input.MoveInput = Vector2.zero;
            Assert.That(stoodOnWall, Is.False, "A second jump from upper cover must not land on the wall cap.");
            Assert.That(body.position.z, Is.GreaterThan(boundaryZ + .4f), "The upper boundary must keep Spirit inside, not allow a drop to the lower storey.");
        }

        private IEnumerator WalkTo(Vector3 target)
        {
            float startTime = Time.time, startReal = Time.realtimeSinceStartup;
            float distance = Vector3.ProjectOnPlane(target - body.position, Vector3.up).magnitude;
            float budget = distance / spirit.MoveSpeed + 3f;
            Vector3 progressPosition = body.position;
            float progressReal = startReal;
            while (Vector3.ProjectOnPlane(target - body.position, Vector3.up).magnitude > .23f)
            {
                if (Time.time - startTime >= budget) Assert.Fail(Diagnostic("scaled movement deadline", target));
                if (Time.realtimeSinceStartup - startReal >= Mathf.Max(12f, budget * 2f))
                    Assert.Fail(Diagnostic("realtime movement deadline", target));
                if (body.position.y <= .7f) Assert.Fail(Diagnostic("fell below the lower floor", target));
                var delta = target - body.position;
                input.MoveInput = Vector2.ClampMagnitude(new Vector2(delta.x, delta.z)
                    / Mathf.Max(.001f, spirit.MoveSpeed * Time.fixedDeltaTime), 1f);
                if (Time.realtimeSinceStartup - progressReal > 1.1f)
                {
                    Assert.That(Vector3.ProjectOnPlane(body.position - progressPosition, Vector3.up).magnitude,
                        Is.GreaterThan(.04f), Diagnostic("no horizontal progress for 1.1 seconds", target));
                    progressPosition = body.position;
                    progressReal = Time.realtimeSinceStartup;
                }
                // Frame waits keep the realtime deadline effective even if another
                // system unexpectedly pauses scaled time and therefore FixedUpdate.
                yield return null;
            }
            input.MoveInput = Vector2.zero;
        }

        private IEnumerator Settle(float seconds)
        {
            input.MoveInput = Vector2.zero;
            float until = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < until) yield return null;
        }

        private string Diagnostic(string phase, Vector3 target)
        {
            Bounds b = capsule.bounds;
            var ground = Physics.RaycastAll(b.center, Vector3.down, 12f, ~0, QueryTriggerInteraction.Ignore)
                .Where(h => !h.transform.IsChildOf(spirit.transform)).OrderBy(h => h.distance).FirstOrDefault();
            Vector3 direction = Vector3.ProjectOnPlane(target - body.position, Vector3.up).normalized;
            float radius = Mathf.Min(b.extents.x, b.extents.z) * .9f;
            Vector3 segment = Vector3.up * Mathf.Max(0f, b.extents.y - radius);
            var front = Physics.CapsuleCastAll(b.center + segment, b.center - segment, radius,
                    direction, 1.1f, ~0, QueryTriggerInteraction.Ignore)
                .Where(h => !h.transform.IsChildOf(spirit.transform)).OrderBy(h => h.distance).FirstOrDefault();
            string contacts = string.Join(", ", Physics.OverlapCapsule(b.center + segment, b.center - segment,
                    radius + .025f, ~0, QueryTriggerInteraction.Ignore)
                .Where(c => !c.transform.IsChildOf(spirit.transform)).Select(c => c.name).Distinct().Take(8));
            string Ground(RaycastHit hit) => hit.collider == null ? "none" :
                $"{hit.collider.name} at {hit.point:F3}, normal={hit.normal:F3}, distance={hit.distance:0.000}";
            return $"{phase}: position={body.position:F3}, target={target:F3}, velocity={body.linearVelocity:F3}, "
                + $"grounded={spirit.IsGrounded}, feetY={b.min.y:0.000}, timeScale={Time.timeScale}, "
                + $"game={GameManager.Instance?.CurrentState}; below=[{Ground(ground)}], forward=[{Ground(front)}], "
                + $"contacts=[{contacts}], fixed={input.FixedSamples}, rampSamples={input.RampSupportSamples}.";
        }

        private static Vector3 P(float x, float y, float z) => Level4Layout.Map(new Vector3(x, y, z));
    }

    // Legacy Input cannot synthesize keyboard state. Feed its already-normalized
    // movement sample immediately before the unchanged shared physics controller.
    [DefaultExecutionOrder(-10000)]
    public sealed class Level4StairInputDriver : MonoBehaviour
    {
        private static readonly FieldInfo MoveField = typeof(SpiritController)
            .GetField("moveInput", BindingFlags.Instance | BindingFlags.NonPublic);
        public SpiritController Spirit;
        public Vector2 MoveInput;
        public int FixedSamples, GroundedSamples, RampSupportSamples;
        public float LowestY = float.PositiveInfinity, HighestY = float.NegativeInfinity;

        private void FixedUpdate()
        {
            if (Spirit == null || !Spirit.isActiveAndEnabled) return;
            MoveField.SetValue(Spirit, MoveInput);
            FixedSamples++;
            if (Spirit.IsGrounded) GroundedSamples++;
            LowestY = Mathf.Min(LowestY, Spirit.transform.position.y);
            HighestY = Mathf.Max(HighestY, Spirit.transform.position.y);
            foreach (var hit in Physics.RaycastAll(Spirit.transform.position, Vector3.down, 3f,
                         ~0, QueryTriggerInteraction.Ignore))
                if (hit.collider.name == "Stair_Ramp_WalkSurface") { RampSupportSamples++; break; }
        }
    }
}
