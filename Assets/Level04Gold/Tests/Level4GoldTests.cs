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
    // These tests exercise the saved blockout, shared gameplay APIs and physical
    // connections. Actor placement below is test-fixture setup, not a level shortcut.
    public sealed class Level4GoldTests
    {
        private const string SceneName = "Level_04_Gold";
        private SpiritController spirit;
        private PossessionController possession;

        [UnitySetUp]
        public IEnumerator LoadLevel()
        {
            Time.timeScale = 1f;
            EventBus.Clear();
            yield return SceneManager.LoadSceneAsync(SceneName);
            yield return null;
            yield return new WaitForFixedUpdate();
            spirit = Object.FindFirstObjectByType<SpiritController>();
            Assert.That(spirit, Is.Not.Null, "Build the saved Level 4 scene before running its PlayMode tests.");
            possession = spirit.GetComponent<PossessionController>();

            // Automated fixture attempts must not become real playtest uploads or reports.
            foreach (var reporter in Object.FindObjectsByType<FirebaseReporter>(FindObjectsSortMode.None))
                Object.DestroyImmediate(reporter.gameObject);
            foreach (var tracker in Object.FindObjectsByType<P5TimingTracker>(FindObjectsSortMode.None))
                tracker.PersistReports = false;
        }

        [UnityTearDown]
        public IEnumerator UnloadLevel()
        {
            Time.timeScale = 1f;
            var old = SceneManager.GetActiveScene();
            var empty = SceneManager.CreateScene("Level4GoldTestCleanup");
            SceneManager.SetActiveScene(empty);
            if (old.IsValid() && old.isLoaded) yield return SceneManager.UnloadSceneAsync(old);
            EventBus.Clear();
        }

        [Test]
        public void SavedLevelUsesSharedBodiesResourcesAndRecoveryRules()
        {
            var hosts = All<EnemyBase>();
            Assert.That(hosts.Length, Is.EqualTo(8));
            Assert.That(hosts.Count(e => e.GetType() == typeof(GuardEnemy)), Is.EqualTo(4));
            Assert.That(hosts.Count(e => e.GetType() == typeof(HeavyEnemy)), Is.EqualTo(2));
            Assert.That(hosts.Count(e => e is EngineerEnemy), Is.EqualTo(2));
            Assert.That(All<HUDManager>().Length, Is.EqualTo(1));
            Assert.That(All<GameManager>().Length, Is.EqualTo(1));
            Assert.That(spirit.MaxHP, Is.EqualTo(5));
            Assert.That(possession.PossessDuration, Is.EqualTo(12f));
            Assert.That(possession.PossessRange, Is.EqualTo(5f));
            Assert.That(possession.EjectProtectionSeconds, Is.EqualTo(2f));
            var charges = spirit.GetComponent<PossessionCharge>();
            Assert.That(charges, Is.Not.Null);
            Assert.That(charges.MaxCharges, Is.EqualTo(3));
            Assert.That(charges.Count, Is.EqualTo(1));
            Assert.That(All<PossessionChargePickup>().Length, Is.GreaterThanOrEqualTo(6));
            Assert.That(All<HealthPickup>().Length, Is.GreaterThanOrEqualTo(3));
            foreach (var guard in hosts.OfType<GuardEnemy>())
                Assert.That(guard.GetComponent<ShootAbility>().MaxUses, Is.EqualTo(3));
            foreach (var heavy in hosts.OfType<HeavyEnemy>())
                Assert.That(heavy.GetComponent<SmashAbility>().MaxUses, Is.EqualTo(1));
        }

        [Test]
        public void SavedRoomsAndHallsExpandWithoutInflatingWallsOrActorBodies()
        {
            var boxes = All<BoxCollider>();
            var entry = boxes.Single(c => c.name == "Floor_Entry").bounds.size;
            var watch = boxes.Single(c => c.name == "Floor_WatchGallery").bounds.size;
            Assert.That(entry.x, Is.EqualTo(17.5f).Within(.01f));
            Assert.That(entry.z, Is.EqualTo(12.5f).Within(.01f));
            Assert.That(watch.x, Is.EqualTo(22.5f).Within(.01f));
            Assert.That(watch.z, Is.EqualTo(15f).Within(.01f));
            var halls = new[]
            {
                "Entry_Junction", "GalleryDoor", "Gallery_Stair", "Entry_Engineer",
                "Junction_Service", "Engineer_Service", "Engineer_Maintenance", "ServiceDoor",
                "AccessGate", "SmashWall", "MaintenanceAccess", "VaultDoor",
                "Landing_Records", "Landing_Watch", "Watch_Overlook"
            };
            foreach (string hall in halls)
            {
                var floor = boxes.Single(c => c.name == "Floor_" + hall);
                Assert.That(Mathf.Min(floor.bounds.size.x, floor.bounds.size.z), Is.GreaterThanOrEqualTo(5.99f),
                    hall + " must keep at least six metres of physical floor across its passage.");
            }
            var walls = boxes.Where(c => c.name.StartsWith("Wall_", StringComparison.Ordinal)
                && !c.name.StartsWith("Wall_Cap_", StringComparison.Ordinal)).ToArray();
            Assert.That(walls, Is.Not.Empty);
            foreach (var wall in walls)
            {
                var size = wall.bounds.size;
                Assert.That(size.y, Is.EqualTo(4.5f).Within(.01f));
                Assert.That(Mathf.Abs(size.x - .4f) < .01f || Mathf.Abs(size.z - .4f) < .01f, Is.True,
                    wall.name + " must preserve the physical wall thickness while the room expands.");
            }
            foreach (var engineer in All<EngineerEnemy>())
            {
                var body = engineer.GetComponent<CharacterController>();
                Assert.That(Vector3.Distance(engineer.transform.lossyScale, Vector3.one * 1.25f), Is.LessThan(.0001f));
                Assert.That(body.height, Is.EqualTo(2f).Within(.001f));
                Assert.That(body.radius, Is.EqualTo(.5f).Within(.001f));
            }
            Assert.That(spirit.transform.lossyScale, Is.EqualTo(Vector3.one * 1.25f));
        }

        [UnityTest]
        public IEnumerator TargetDoorsLatchTheirOwnThreeTargetsIndependently()
        {
            FreezeAutonomousBodies();
            var doors = All<Door>().OrderBy(d => d.name).ToArray();
            Assert.That(doors.Length, Is.EqualTo(2));
            Assert.That(All<Target>().Length, Is.EqualTo(6));
            var first = Read<Target[]>(doors[0], "requiredTargets");
            var second = Read<Target[]>(doors[1], "requiredTargets");
            Assert.That(first.Length, Is.EqualTo(3));
            Assert.That(second.Length, Is.EqualTo(3));
            Assert.That(first.Intersect(second), Is.Empty, "The two route locks must not accidentally share their targets.");
            first[0].OnHit(); first[1].OnHit();
            Assert.That(doors[0].IsOpen, Is.False);
            first[2].OnHit();
            Assert.That(doors[0].IsOpen, Is.True);
            Assert.That(doors[1].IsOpen, Is.False, "Opening the outward gallery must not solve the service route.");
            Assert.That(doors[0].GetComponent<Collider>().isTrigger, Is.True);
            foreach (var target in second) target.OnHit();
            yield return null;
            Assert.That(doors[1].IsOpen, Is.True);
        }

        [UnityTest]
        public IEnumerator HeavyActuallySmashesThePartitionAndCreatesAWalkableOpening()
        {
            FreezeAutonomousBodies();
            var wall = All<BreakableWall>().Single();
            Assert.That(wall.CurrentHP, Is.EqualTo(2));
            wall.TakeDamage(1);
            Assert.That(wall.CurrentHP, Is.EqualTo(2), "A Guard-strength hit must not replace the Heavy's one smash.");
            var heavy = All<HeavyEnemy>().OrderBy(h => h.transform.position.y).First();
            Position(heavy, M(46f, 1.775f, 29.7f), Vector3.forward);
            spirit.RestoreAt(M(46f, 1.4f, 27.2f));
            Physics.SyncTransforms();
            Assert.That(possession.TryPossess(heavy), Is.True);
            heavy.GetComponent<SmashAbility>().TryExecute();
            yield return null;
            Assert.That(wall.CurrentHP, Is.LessThanOrEqualTo(0));
            Assert.That(wall.gameObject.activeSelf, Is.False);
            Assert.That(Physics.Raycast(M(46f, 1f, 30.5f), Vector3.forward,
                3f, ~0, QueryTriggerInteraction.Ignore), Is.False,
                "Breaking the partition must create a physical passage, not just remove a decorative panel.");
        }

        [UnityTest]
        public IEnumerator AccessReleaseRequiresTheRealBodyNearbyOnTheCorrectFloorWithClearReach()
        {
            FreezeAutonomousBodies();
            var gate = All<Level4AccessGate>().Single(g => g.name == "Service_Access_Gate");
            Assert.That(gate, Is.Not.Null);
            Assert.That(gate.IsOpen, Is.False);
            Assert.That(gate.GetComponent<Collider>().isTrigger, Is.False);
            var at = gate.ReleasePoint.position;
            var close = new Vector3(at.x + 2f, 1.4f, at.z);

            var impostor = new GameObject("Not the controlled body");
            impostor.transform.position = close;
            Assert.That(gate.TryRelease(impostor.transform), Is.False);
            spirit.RestoreAt(close + Vector3.up * 5f);
            Assert.That(gate.TryRelease(spirit.transform), Is.False, "An upper-floor body cannot reach a lower release.");
            spirit.RestoreAt(close + Vector3.right * 5f);
            Assert.That(gate.TryRelease(spirit.transform), Is.False);

            spirit.RestoreAt(close);
            var occluder = GameObject.CreatePrimitive(PrimitiveType.Cube);
            occluder.name = "Solid obstruction between actor and release";
            occluder.transform.position = new Vector3(at.x + 1f, 1.5f, at.z);
            occluder.transform.localScale = new Vector3(.3f, 3f, 3f);
            Physics.SyncTransforms();
            Assert.That(gate.TryRelease(spirit.transform), Is.False, "Interaction must not pass through solid room geometry.");
            Object.Destroy(occluder);
            yield return null;
            Physics.SyncTransforms();
            Assert.That(gate.TryRelease(spirit.transform), Is.True);
            Assert.That(gate.IsOpen, Is.True);
            var openBlocker = gate.GetComponent<Collider>();
            Assert.That(!openBlocker.enabled || openBlocker.isTrigger, Is.True,
                "An opened access gate must stop blocking ordinary bodies.");
        }

        [UnityTest]
        public IEnumerator SecurityConsoleRequiresTheActualEngineerAndClearReach()
        {
            FreezeAutonomousBodies();
            var gate = All<Level4AccessGate>().Single(g => g.name == "Archive_Entry_Door");
            Assert.That(gate.EngineerPoint, Is.Not.Null);
            var near = gate.EngineerPoint.position + Vector3.back * 1.5f;
            spirit.RestoreAt(near);
            Assert.That(gate.TryUnlock(spirit.transform), Is.False);
            var impostor = new GameObject("Uncontrolled security-console actor");
            impostor.transform.position = near;
            Assert.That(gate.TryUnlock(impostor.transform), Is.False);
            var engineer = ArchiveEngineer();
            Vector3 originalEngineerPosition = engineer.transform.position;
            Position(engineer, near, Vector3.forward);
            engineer.enabled = true;
            engineer.OnPossessed();
            Assert.That(gate.TryUnlock(engineer.transform), Is.False,
                "Possession must belong to the real player controller.");
            engineer.OnEjected();
            Position(engineer, originalEngineerPosition + Vector3.left * 4f, Vector3.forward);

            var guard = All<GuardEnemy>().First();
            Vector3 originalGuardPosition = guard.transform.position;
            Position(guard, near, Vector3.forward);
            spirit.RestoreAt(near + Vector3.back * 2.5f);
            Assert.That(possession.TryPossess(guard), Is.True);
            Assert.That(gate.TryUnlock(guard.transform), Is.False);
            Assert.That(possession.TryEject(false), Is.True);
            FreezeAutonomousBodies();
            Position(guard, originalGuardPosition, Vector3.forward);
            yield return new WaitForSeconds(2.05f);
            while (spirit.GetComponent<PossessionCharge>().TryCollect()) { }
            Position(engineer, near, Vector3.forward);
            spirit.RestoreAt(near + Vector3.back * 2.5f);
            engineer.enabled = true;
            Assert.That(possession.TryPossess(engineer), Is.True);
            Position(engineer, near + Vector3.up * 5f, Vector3.forward);
            Assert.That(gate.TryUnlock(engineer.transform), Is.False);
            Position(engineer, near + Vector3.left * 5f, Vector3.forward);
            Assert.That(gate.TryUnlock(engineer.transform), Is.False);
            Position(engineer, near, Vector3.forward);
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = Vector3.Lerp(near, gate.EngineerPoint.position, .5f);
            wall.transform.localScale = new Vector3(3f, 3f, .2f);
            Physics.SyncTransforms();
            Assert.That(gate.TryUnlock(engineer.transform), Is.False);
            Object.Destroy(wall);
            yield return null;
            Physics.SyncTransforms();
            Assert.That(gate.TryUnlock(engineer.transform), Is.True);
            Assert.That(gate.IsOpen, Is.True);
            Assert.That(gate.GetComponent<Collider>().enabled, Is.False);
            Assert.That(possession.TryEject(false), Is.True);
            Assert.That(gate.IsOpen, Is.True, "Opened mechanical access must persist after ejecting.");
        }

        [UnityTest]
        public IEnumerator OnlyAPossessedEngineerWithinReachCanOperateTheVault()
        {
            yield return RecoverCipherWithTheRealEngineer();
            var vault = Vault();
            spirit.RestoreAt(M(36f, 1.4f, 41.5f));
            Assert.That(vault.TryBeginInteraction(), Is.False, "Spirit cannot operate the Engineer's mechanical socket.");
            var guard = All<GuardEnemy>().First();
            Position(guard, M(36f, 1.4f, 41.5f), Vector3.forward);
            spirit.RestoreAt(M(36f, 1.4f, 39f));
            Assert.That(possession.TryPossess(guard), Is.True);
            Assert.That(vault.TryBeginInteraction(), Is.False, "A Guard must not be treated as an Engineer.");
            Assert.That(vault.TryOpenHandle(), Is.False);
            Assert.That(vault.AlignedSteps, Is.Zero);
            Assert.That(vault.IsOpen, Is.False);
            yield return null;
        }

        [UnityTest]
        public IEnumerator KnowingTheDialStopsCannotBypassPhysicalCipherRecovery()
        {
            yield return PrepareEngineerAtVault(recoverCipher: false);
            var vault = Vault();
            Assert.That(Cipher().Recovered, Is.False);
            Assert.That(vault.CanOperate(), Is.False);
            Assert.That(vault.TryBeginInteraction(), Is.False);
            vault.RotateDial(60f);
            Assert.That(vault.TryConfirmAlignment(), Is.False);
            vault.RotateDial(-120f);
            Assert.That(vault.TryConfirmAlignment(), Is.False);
            Assert.That(vault.TryOpenHandle(), Is.False);
            Assert.That(vault.AlignedSteps, Is.Zero);
            Assert.That(vault.IsOpen, Is.False);
            Assert.That(GameManager.Instance.CurrentState, Is.EqualTo(LevelState.InProgress));
        }

        [UnityTest]
        public IEnumerator CipherRequiresTheRealPossessedEngineerWithClearReachOnItsFloor()
        {
            FreezeAutonomousBodies();
            var cipher = Cipher();
            Assert.That(cipher, Is.Not.Null);
            Assert.That(cipher.Recovered, Is.False);
            var near = cipher.RecoveryPoint.position + Vector3.left * 1.5f;
            spirit.RestoreAt(near);
            Assert.That(cipher.CanRecover(spirit.transform), Is.False);
            Assert.That(cipher.TryRecover(spirit.transform), Is.False);
            var impostor = new GameObject("Uncontrolled actor at cipher");
            impostor.transform.position = near;
            Assert.That(cipher.TryRecover(impostor.transform), Is.False);
            var engineer = ArchiveEngineer();
            Position(engineer, near, Vector3.right);
            engineer.enabled = true;
            engineer.OnPossessed();
            Assert.That(cipher.TryRecover(engineer.transform), Is.False,
                "A body marked possessed outside the player's real controller must not claim the cipher.");
            engineer.OnEjected();

            yield return PrepareEngineerAtCipher();
            Position(engineer, near + Vector3.up * 5f, Vector3.right);
            Assert.That(cipher.TryRecover(engineer.transform), Is.False);
            Position(engineer, near + Vector3.left * 5f, Vector3.right);
            Assert.That(cipher.TryRecover(engineer.transform), Is.False);
            Position(engineer, near, Vector3.right);
            var obstruction = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstruction.name = "Solid wall between Engineer and cipher";
            obstruction.transform.position = Vector3.Lerp(near, cipher.RecoveryPoint.position, .5f);
            obstruction.transform.localScale = new Vector3(.2f, 3f, 3f);
            Physics.SyncTransforms();
            Assert.That(cipher.CanRecover(engineer.transform), Is.False);
            Assert.That(cipher.TryRecover(engineer.transform), Is.False);
            Object.Destroy(obstruction);
            yield return null;
            Physics.SyncTransforms();
            Assert.That(cipher.CanRecover(engineer.transform), Is.True);
            Assert.That(cipher.TryRecover(engineer.transform), Is.True);
            Assert.That(cipher.Recovered, Is.True);
            Assert.That(possession.TryEject(false), Is.True);
            yield return null;
            Assert.That(cipher.Recovered, Is.True, "The recovered objective must survive returning to Spirit.");
            Assert.That(possession.CurrentState, Is.EqualTo(PlayerState.Spirit));
        }

        [UnityTest]
        public IEnumerator ADeadControlledEngineerCannotClaimTheCipher()
        {
            yield return PrepareEngineerAtCipher();
            var engineer = possession.PossessedEnemy;
            Assert.That(Cipher().CanRecover(engineer.transform), Is.True);
            engineer.TakeDamage(engineer.CurrentHP);
            Assert.That(Cipher().TryRecover(engineer.transform), Is.False);
            Assert.That(Cipher().Recovered, Is.False);
        }

        [UnityTest]
        public IEnumerator EngineerOpensBothArchiveDoorsAndSpiritCanPhysicallyReturn()
        {
            FreezeAutonomousBodies();
            var gates = All<Level4AccessGate>().Where(g => g.name.StartsWith("Archive_", StringComparison.Ordinal)).ToArray();
            Assert.That(gates.Select(g => g.name), Is.EquivalentTo(new[] { "Archive_Entry_Door", "Archive_Clue_Door" }));
            Assert.That(gates.All(g => !g.IsOpen && g.GetComponent<Collider>().enabled), Is.True);
            var entrance = gates.Single(g => g.name == "Archive_Entry_Door");
            var clueDoor = gates.Single(g => g.name == "Archive_Clue_Door");
            var engineer = ArchiveEngineer();
            Position(engineer, M(70f, 6.4f, 30.4f), Vector3.forward);
            var body = engineer.GetComponent<CharacterController>();
            body.Move(Vector3.forward * 4f);
            Assert.That(engineer.transform.position.z, Is.LessThan(M(70f, 6.4f, 31.4f).z),
                "The Engineer must unlock an ordinary solid security door before crossing it.");
            spirit.RestoreAt(entrance.EngineerPoint.position + Vector3.back);
            Assert.That(entrance.TryUnlock(spirit.transform), Is.False);
            Position(engineer, M(70f, 6.4f, 27.5f), Vector3.forward);
            spirit.RestoreAt(M(70f, 6.4f, 25f));
            engineer.enabled = true;
            Physics.SyncTransforms();
            Assert.That(possession.TryPossess(engineer), Is.True);
            yield return WalkEngineerTo(engineer, entrance.EngineerPoint.position + Vector3.back * .9f);
            Assert.That(entrance.TryUnlock(engineer.transform), Is.True);
            Assert.That(entrance.GetComponent<Collider>().enabled, Is.False);
            var route = new[]
            {
                M(70f, 6.4f, 35f), M(68f, 6.4f, 37f),
                M(68f, 6.4f, 40.8f), M(61f, 6.4f, 40.8f)
            };
            foreach (var waypoint in route) yield return WalkEngineerTo(engineer, waypoint);
            yield return WalkEngineerTo(engineer, clueDoor.EngineerPoint.position + Vector3.right * .9f);
            Assert.That(clueDoor.TryUnlock(engineer.transform), Is.True);
            yield return WalkEngineerTo(engineer, M(50.8f, 6.4f, 40f));
            Assert.That(possession.PossessedEnemy, Is.SameAs(engineer));
            Assert.That(Cipher().TryRecover(engineer.transform), Is.True);
            Assert.That(gates.All(g => g.IsOpen && !g.GetComponent<Collider>().enabled), Is.True);
            engineer.transform.rotation = Quaternion.LookRotation(Vector3.left);
            Assert.That(possession.TryEject(false), Is.True);
            yield return null;
            Assert.That(Cipher().Recovered, Is.True);
            Assert.That(spirit.IsDamageProtected, Is.True);
            foreach (var gate in gates)
                Assert.That(Physics.GetIgnoreCollision(body, gate.GetComponent<Collider>()), Is.False);

            // Drive the ordinary shared Rigidbody through the opened return route;
            // disabling only its input reader keeps the actual floor and wall physics.
            spirit.enabled = false;
            foreach (var waypoint in route.Reverse()) yield return WalkSpiritTo(waypoint);
            yield return WalkSpiritTo(M(70f, 6.4f, 27.5f));
            spirit.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
            spirit.enabled = true;
            Assert.That(spirit.transform.position.y, Is.InRange(6f, 6.6f));
            Assert.That(spirit.transform.position.z, Is.LessThan(M(70f, 6.4f, 28f).z));
            Assert.That(possession.CurrentState, Is.EqualTo(PlayerState.Spirit));
        }

        [UnityTest]
        public IEnumerator VaultExplainsEachDirectionStopAndFinalHandle()
        {
            yield return PrepareEngineerAtVault();
            var vault = Vault();
            Assert.That(vault.TryBeginInteraction(), Is.True);
            var instruction = All<UnityEngine.UI.Text>().SingleOrDefault(t => t.name == "VaultInstruction");
            Assert.That(instruction, Is.Not.Null, "Show the current action in plain language, not CW/CCW abbreviations alone.");
            StringAssert.Contains("D / RIGHT", instruction.text);
            StringAssert.Contains("TRIANGLE", instruction.text);
            StringAssert.Contains("CONFIRM", instruction.text);
            vault.RotateDial(60f); Assert.That(vault.TryConfirmAlignment(), Is.True);
            StringAssert.Contains("A / LEFT", instruction.text);
            StringAssert.Contains("SQUARE", instruction.text);
            vault.RotateDial(-120f); Assert.That(vault.TryConfirmAlignment(), Is.True);
            StringAssert.Contains("TURN HANDLE", instruction.text);
        }

        [UnityTest]
        public IEnumerator WrongStopsAndDirectionsLeaveTheCurrentBoltUnchanged()
        {
            yield return PrepareEngineerAtVault();
            var vault = Vault();
            Assert.That(vault.TryBeginInteraction(), Is.True);
            vault.RotateDial(30f);
            Assert.That(vault.TryConfirmAlignment(), Is.False);
            Assert.That(vault.AlignedSteps, Is.Zero);
            vault.RotateDial(30f);
            Assert.That(vault.TryConfirmAlignment(), Is.True);
            Assert.That(vault.AlignedSteps, Is.EqualTo(1));
            Assert.That(vault.TryOpenHandle(), Is.False, "One bolt must not release the final vault.");
            vault.RotateDial(240f); // Square reached clockwise: the clue asks for counterclockwise.
            Assert.That(vault.TryConfirmAlignment(), Is.False);
            Assert.That(vault.AlignedSteps, Is.EqualTo(1));
            vault.RotateDial(-10f); // A forgiving near-stop, now in the correct direction.
            Assert.That(vault.TryConfirmAlignment(), Is.True);
            Assert.That(vault.AlignedSteps, Is.EqualTo(2));
            Assert.That(vault.IsOpen, Is.False, "The separate handle is the final action.");
        }

        [UnityTest]
        public IEnumerator EngineerUsesTheRecoveredCipherAndCompletionIsAcceptedExactlyOnce()
        {
            yield return PrepareEngineerAtVault();
            var vault = Vault();
            Assert.That(Cipher().Recovered, Is.True);
            int completions = 0;
            EventBus.LevelCompleted += () => completions++;
            Assert.That(vault.TryBeginInteraction(), Is.True);
            vault.RotateDial(60f);
            Assert.That(vault.TryConfirmAlignment(), Is.True);
            vault.RotateDial(-120f);
            Assert.That(vault.TryConfirmAlignment(), Is.True);
            Assert.That(vault.TryOpenHandle(), Is.True);
            Assert.That(vault.IsOpen, Is.True);
            Assert.That(GameManager.Instance.CurrentState, Is.EqualTo(LevelState.Complete));
            Assert.That(completions, Is.EqualTo(1));
            Assert.That(vault.TryOpenHandle(), Is.False);
            Assert.That(completions, Is.EqualTo(1));
            yield return null;
        }

        [UnityTest]
        public IEnumerator ManualEjectionClosesTheControlsAndKeepsTheAlignedBolt()
        {
            yield return PrepareEngineerAtVault();
            var vault = Vault();
            Assert.That(vault.TryBeginInteraction(), Is.True);
            vault.RotateDial(60f);
            Assert.That(vault.TryConfirmAlignment(), Is.True);
            Assert.That(possession.TryEject(false), Is.True);
            yield return null;
            Assert.That(vault.IsInteracting, Is.False);
            Assert.That(vault.AlignedSteps, Is.EqualTo(1));
            Assert.That(vault.IsOpen, Is.False);
            Assert.That(Cipher().Recovered, Is.True);
            Assert.That(spirit.IsDamageProtected, Is.True);
            Assert.That(LowerEngineer().enabled, Is.True,
                "Closing the dial must restore the host's normal shared controller.");
        }

        [UnityTest]
        public IEnumerator SharedPausePreservesTheActiveVaultAndResumeRestoresItsControls()
        {
            yield return PrepareEngineerAtVault();
            var vault = Vault();
            var engineer = LowerEngineer();
            var pause = All<GoldEndScreenPresentation>().Single();
            Assert.That(vault.TryBeginInteraction(), Is.True);
            vault.RotateDial(60f);
            Assert.That(vault.TryConfirmAlignment(), Is.True);
            var overlay = All<Canvas>().Single(c => c.name == "Vault dial");
            pause.Pause();
            float hostTime = possession.PossessTimer;
            float levelTime = GameManager.Instance.LevelTimer;
            yield return null;
            yield return new WaitForSecondsRealtime(.2f);
            Assert.That(pause.IsPaused && pause.PausePanel.activeInHierarchy, Is.True);
            Assert.That(vault.IsInteracting, Is.True, "Pausing must keep the active operator and dial state.");
            Assert.That(engineer.enabled, Is.False);
            Assert.That(overlay.gameObject.activeSelf, Is.False, "Vault controls must not cover the shared pause buttons.");
            Assert.That(possession.PossessTimer, Is.EqualTo(hostTime).Within(.001f));
            Assert.That(GameManager.Instance.LevelTimer, Is.EqualTo(levelTime).Within(.001f));
            vault.RotateDial(-120f);
            Assert.That(vault.TryConfirmAlignment(), Is.False);
            Assert.That(vault.DialAngle, Is.EqualTo(60f));
            Assert.That(vault.AlignedSteps, Is.EqualTo(1));

            pause.PausePanel.transform.Find("Pause Card/ResumeButton")
                .GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            yield return null;
            Assert.That(pause.IsPaused, Is.False);
            Assert.That(vault.IsInteracting && overlay.gameObject.activeSelf, Is.True);
            Assert.That(engineer.enabled, Is.False);
            vault.RotateDial(-120f);
            Assert.That(vault.TryConfirmAlignment(), Is.True);
            Assert.That(vault.AlignedSteps, Is.EqualTo(2));
            vault.EndInteraction();
            Assert.That(engineer.enabled, Is.True);
        }

        [UnityTest]
        public IEnumerator TheNormalTimerContinuesAtTheDialAndForcedEjectionKeepsProgressAndShield()
        {
            yield return PrepareEngineerAtVault();
            var vault = Vault();
            var engineer = LowerEngineer();
            Assert.That(vault.TryBeginInteraction(), Is.True);
            vault.RotateDial(60f);
            Assert.That(vault.TryConfirmAlignment(), Is.True);
            float before = possession.PossessTimer;
            yield return new WaitForSeconds(.25f);
            Assert.That(possession.PossessTimer, Is.LessThan(before - .1f),
                "The puzzle must not silently pause the ordinary possession budget.");
            Time.timeScale = 5f;
            float deadline = Time.time + possession.PossessDuration + 1f;
            while (possession.CurrentState != PlayerState.Spirit && Time.time < deadline) yield return null;
            Time.timeScale = 1f;
            Assert.That(possession.CurrentState, Is.EqualTo(PlayerState.Spirit));
            Assert.That(engineer.IsHostile, Is.True);
            Assert.That(vault.IsInteracting, Is.False);
            Assert.That(vault.AlignedSteps, Is.EqualTo(1));
            Assert.That(Cipher().Recovered, Is.True);
            Assert.That(possession.IsEjectShielded, Is.True);
            int hp = spirit.CurrentHP;
            spirit.TakeDamage(1);
            Assert.That(spirit.CurrentHP, Is.EqualTo(hp));
        }

        [UnityTest]
        public IEnumerator LosingBothEngineersEndsTheAttemptInsteadOfLeavingASoftlock()
        {
            FreezeAutonomousBodies();
            var engineer = LowerEngineer();
            engineer.TakeDamage(engineer.CurrentHP);
            yield return null;
            Assert.That(GameManager.Instance.CurrentState, Is.EqualTo(LevelState.InProgress),
                "The second living Engineer must remain a recovery option.");
            var backup = ArchiveEngineer();
            backup.TakeDamage(backup.CurrentHP);
            yield return null;
            Assert.That(GameManager.Instance.CurrentState, Is.EqualTo(LevelState.Failed));
            Assert.That(Vault().IsOpen, Is.False);
            Assert.That(spirit.CurrentHP, Is.GreaterThan(0), "Failure should identify host loss, not kill the Spirit as a substitute.");
        }

        [UnityTest]
        public IEnumerator TheStairIsContinuousAndTheSharedControllerCanClimbAndDescendIt()
        {
            FreezeAutonomousBodies();
            var ramp = All<MeshCollider>().Single(c => c.name == "Stair_Ramp_WalkSurface");
            Bounds flight = ramp.bounds;
            float x = flight.center.x, lowerZ = flight.min.z - 1.5f, upperZ = flight.max.z + 1.5f;
            float rayY = flight.max.y + 6f;
            Assert.That(Physics.Raycast(new Vector3(x, rayY, lowerZ), Vector3.down, out var lower,
                20f, ~0, QueryTriggerInteraction.Ignore), Is.True);
            Assert.That(Physics.Raycast(new Vector3(x, rayY, upperZ), Vector3.down, out var upper,
                20f, ~0, QueryTriggerInteraction.Ignore), Is.True);
            Assert.That(upper.point.y - lower.point.y, Is.EqualTo(5f).Within(.05f));
            for (int i = 0; i < 13; i++)
            {
                float z = Mathf.Lerp(flight.min.z + .25f, flight.max.z - .25f, i / 12f);
                Assert.That(Physics.Raycast(new Vector3(x, rayY, z), Vector3.down, out var hit,
                    20f, ~0, QueryTriggerInteraction.Ignore), Is.True);
                Assert.That(hit.collider.name, Is.EqualTo("Stair_Ramp_WalkSurface"),
                    "An upper flat slab or decorative tread must not block the stair flight.");
                float expected = Mathf.Lerp(lower.point.y, upper.point.y,
                    Mathf.InverseLerp(flight.min.z, flight.max.z, z));
                Assert.That(hit.point.y, Is.EqualTo(expected).Within(.05f));
            }
            var engineer = LowerEngineer();
            var cc = engineer.GetComponent<CharacterController>();
            float bodyHalfHeight = cc.height * Mathf.Abs(engineer.transform.lossyScale.y) * .5f;
            Position(engineer, new Vector3(x, lower.point.y + bodyHalfHeight + .15f, lowerZ), Vector3.forward);
            float verticalSpeed = -2f;

            // Match EnemyBase's two Move calls: horizontal movement first, then
            // the grounded -2 m/s gravity stick and ordinary gravity acceleration.
            void MoveFrame(float destinationZ)
            {
                float dt = Time.deltaTime;
                float dz = Mathf.Clamp(destinationZ - engineer.transform.position.z,
                    -engineer.MoveSpeed * dt, engineer.MoveSpeed * dt);
                if (Mathf.Abs(dz) > .00001f)
                {
                    engineer.transform.rotation = Quaternion.LookRotation(Vector3.forward * Mathf.Sign(dz));
                    cc.Move(Vector3.forward * dz);
                }
                if (cc.isGrounded && verticalSpeed < 0f) verticalSpeed = -2f;
                verticalSpeed += Physics.gravity.y * dt;
                cc.Move(Vector3.up * verticalSpeed * dt);
            }

            IEnumerator TravelTo(float destinationZ, float floorHeight)
            {
                float started = Time.time, realStarted = Time.realtimeSinceStartup;
                float budget = Mathf.Abs(destinationZ - engineer.transform.position.z) / engineer.MoveSpeed * 2f + 3f;
                while (Mathf.Abs(destinationZ - engineer.transform.position.z) > .08f)
                {
                    if (Time.time - started >= budget || Time.realtimeSinceStartup - realStarted >= Mathf.Max(12f, budget * 2f))
                        Assert.Fail($"Engineer stair traversal blocked at {engineer.transform.position:F3}, "
                            + $"targetZ={destinationZ:0.000}, grounded={cc.isGrounded}, vertical={verticalSpeed:0.000}, "
                            + $"rampZ={flight.min.z:0.000}..{flight.max.z:0.000}.");
                    MoveFrame(destinationZ);
                    yield return null;
                }
                float settledAt = Time.realtimeSinceStartup + .25f;
                while (Time.realtimeSinceStartup < settledAt) { MoveFrame(destinationZ); yield return null; }
                Assert.That(cc.bounds.min.y, Is.EqualTo(floorHeight).Within(.12f),
                    "The controller must settle on the physically connected floor beyond the ramp.");
                Assert.That(cc.isGrounded, Is.True);
            }
            yield return TravelTo(upperZ, upper.point.y);
            yield return TravelTo(lowerZ, lower.point.y);
        }

        [UnityTest]
        public IEnumerator UpperFloorVisibilityNeverDisablesItsPhysicalFloor()
        {
            FreezeAutonomousBodies();
            var visibility = Object.FindFirstObjectByType<Level4FloorVisibility>();
            Assert.That(visibility, Is.Not.Null);
            spirit.RestoreAt(M(9f, 1.4f, 7f));
            yield return null;
            visibility.RefreshVisibility();
            Assert.That(visibility.UpperVisible, Is.False);
            var floors = visibility.UpperRoot.GetComponentsInChildren<Collider>()
                .Where(c => !c.isTrigger && c.bounds.size.x > 5f && c.bounds.size.z > 5f && c.bounds.size.y < 1f).ToArray();
            Assert.That(floors, Is.Not.Empty);
            Assert.That(floors.All(c => c.enabled && c.gameObject.activeInHierarchy), Is.True);
            Assert.That(visibility.UpperRoot.GetComponentsInChildren<Renderer>().Any(r => r.forceRenderingOff), Is.True);
            spirit.RestoreAt(M(70f, 6.4f, 25f));
            yield return null;
            visibility.RefreshVisibility();
            Assert.That(visibility.UpperVisible, Is.True);
            Assert.That(floors.All(c => c.enabled && c.gameObject.activeInHierarchy), Is.True);
        }

        private IEnumerator PrepareEngineerAtVault(bool recoverCipher = true)
        {
            FreezeAutonomousBodies();
            if (recoverCipher) yield return RecoverCipherWithTheRealEngineer();
            var engineer = LowerEngineer();
            Position(engineer, M(36f, 1.4f, 41.5f), Vector3.forward);
            spirit.RestoreAt(M(36f, 1.4f, 39f));
            // Keep the actual operator enabled before entering the dial, so the
            // interaction's controller restoration is tested against its normal state.
            engineer.enabled = true;
            Physics.SyncTransforms();
            Assert.That(possession.TryPossess(engineer), Is.True, "The fixture must use the real possession eligibility checks.");
            yield return null;
        }

        private IEnumerator PrepareEngineerAtCipher()
        {
            FreezeAutonomousBodies();
            var cipher = Cipher();
            Assert.That(cipher, Is.Not.Null, "The physical Overlook cipher must be saved in the level.");
            while (spirit.GetComponent<PossessionCharge>().TryCollect()) { }
            var engineer = ArchiveEngineer();
            var near = cipher.RecoveryPoint.position + Vector3.left * 1.5f;
            Position(engineer, near, Vector3.right);
            spirit.RestoreAt(near + Vector3.left * 2.5f);
            engineer.enabled = true;
            Physics.SyncTransforms();
            Assert.That(possession.TryPossess(engineer), Is.True,
                "Cipher fixtures must use the shared possession checks and spend a real charge.");
            yield return null;
        }

        private IEnumerator RecoverCipherWithTheRealEngineer()
        {
            yield return PrepareEngineerAtCipher();
            var engineer = possession.PossessedEnemy;
            Assert.That(Cipher().TryRecover(engineer.transform), Is.True);
            Assert.That(possession.TryEject(false), Is.True);
            FreezeAutonomousBodies();
            yield return new WaitForSeconds(2.05f);
            Assert.That(Cipher().Recovered, Is.True);
            Assert.That(possession.CanStartPossession, Is.True);
        }

        private IEnumerator WalkEngineerTo(EngineerEnemy engineer, Vector3 waypoint)
        {
            var body = engineer.GetComponent<CharacterController>();
            float deadline = Time.time + 5f;
            Vector3 delta;
            while ((delta = Vector3.ProjectOnPlane(waypoint - engineer.transform.position, Vector3.up)).magnitude > .15f
                && Time.time < deadline && possession.PossessedEnemy == engineer)
            {
                engineer.transform.rotation = Quaternion.LookRotation(delta);
                body.Move(Vector3.ClampMagnitude(delta, engineer.MoveSpeed * Time.fixedDeltaTime));
                yield return new WaitForFixedUpdate();
            }
            Assert.That(Vector3.ProjectOnPlane(waypoint - engineer.transform.position, Vector3.up).magnitude,
                Is.LessThan(.2f), "The shared Engineer controller must physically reach " + waypoint);
            Assert.That(engineer.transform.position.y, Is.InRange(6f, 6.6f));
        }

        private IEnumerator WalkSpiritTo(Vector3 waypoint)
        {
            var body = spirit.GetComponent<Rigidbody>();
            float deadline = Time.time + 5f;
            Vector3 delta;
            while ((delta = Vector3.ProjectOnPlane(waypoint - body.position, Vector3.up)).magnitude > .2f
                && Time.time < deadline)
            {
                Vector3 velocity = Vector3.ClampMagnitude(delta / Time.fixedDeltaTime, spirit.MoveSpeed);
                body.linearVelocity = new Vector3(velocity.x, body.linearVelocity.y, velocity.z);
                yield return new WaitForFixedUpdate();
            }
            body.linearVelocity = new Vector3(0f, body.linearVelocity.y, 0f);
            Assert.That(Vector3.ProjectOnPlane(waypoint - body.position, Vector3.up).magnitude,
                Is.LessThan(.25f), "The shared Spirit body must physically return through " + waypoint);
        }

        private static Vector3 M(float x, float y, float z) => Level4Layout.Map(new Vector3(x, y, z));
        private static EngineerEnemy LowerEngineer() => All<EngineerEnemy>().Single(e => e.name == "Engineer");
        private static EngineerEnemy ArchiveEngineer() => All<EngineerEnemy>().Single(e => e.name == "Engineer_Archive");
        private static Level4Vault Vault() => Object.FindFirstObjectByType<Level4Vault>();
        private static Level4Cipher Cipher() => Object.FindFirstObjectByType<Level4Cipher>();
        private static T[] All<T>() where T : Object => Object.FindObjectsByType<T>(FindObjectsSortMode.None);

        private static void FreezeAutonomousBodies()
        {
            foreach (var enemy in All<EnemyBase>()) enemy.enabled = false;
            foreach (var attack in All<EnemyContactDamage>()) attack.enabled = false;
            foreach (var pressure in All<Level4SentryPressure>()) pressure.enabled = false;
            Physics.SyncTransforms();
        }

        private static void Position(EnemyBase enemy, Vector3 position, Vector3 forward)
        {
            var controller = enemy.GetComponent<CharacterController>();
            bool wasEnabled = controller.enabled;
            controller.enabled = false;
            enemy.transform.SetPositionAndRotation(position, Quaternion.LookRotation(forward));
            controller.enabled = wasEnabled;
            Physics.SyncTransforms();
        }

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
