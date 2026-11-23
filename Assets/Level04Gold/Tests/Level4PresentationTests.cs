using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Possession.Level04Gold.Tests
{
    public sealed class Level4PresentationTests
    {
        private SpiritController spirit;
        private PossessionController player;

        [UnitySetUp]
        public IEnumerator LoadSavedLevel()
        {
            Time.captureFramerate = 60;
            yield return Load("Level_04_Gold");
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.captureFramerate = 0;
            Time.timeScale = 1f;
            var old = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Level4PresentationCleanup"));
            if (old.IsValid() && old.isLoaded) yield return SceneManager.UnloadSceneAsync(old);
            EventBus.Clear();
        }

        [UnityTest]
        public IEnumerator LocalHudMatchesCampaignAndContextDoesNotCoverSharedStatus()
        {
            yield return Load(CampaignLevels.FirstScene);
            string campaignLayout = HudSignature(All<HUDManager>().Single());
            yield return Load("Level_04_Gold");
            var hud = All<HUDManager>().Single();
            Assert.That(HudSignature(hud), Is.EqualTo(campaignLayout));
            Assert.That(All<ChargeHUD>().Length, Is.EqualTo(1));
            Assert.That(hud.GetComponentsInChildren<RectTransform>(true).Count(r => r.name == "ChargePanel"), Is.EqualTo(1));
            Assert.That(hud.transform.Find("ObjectiveBar").gameObject.activeInHierarchy, Is.True);
            var status = NamedRect("FloorStatus");
            Assert.That(status.anchorMin, Is.EqualTo(Vector2.one));
            Assert.That(status.anchorMax, Is.EqualTo(Vector2.one));
            Assert.That(All<Text>().Any(t => t.text.Contains("LEVEL 4 / THE SPLIT VAULT")), Is.False,
                "The old large top-left title must not compete with the shared HUD.");
            AssertNoSharedStatusOverlap(status, hud);
            Assert.That(NamedRect("ActionPrompt").gameObject.activeInHierarchy, Is.False,
                "No interaction prompt should be displayed far from an available action.");

            yield return PossessEngineerAtCipher();
            var cipher = All<Level4Cipher>().Single();
            Assert.That(cipher.TryRecover(player.PossessedEnemy.transform), Is.True);
            yield return null;
            Canvas.ForceUpdateCanvases();
            Assert.That(NamedRect("RecoveredCipher").gameObject.activeInHierarchy, Is.True);
            AssertNoSharedStatusOverlap(status, hud);
            var action = NamedRect("ActionPrompt");
            if (action.gameObject.activeInHierarchy) AssertNoSharedStatusOverlap(action, hud);
        }

        [UnityTest]
        public IEnumerator WorldPresentationUsesGoldMaterialsWithoutDuplicatingVisualsOrChangingPhysics()
        {
            var scene = SceneManager.GetActiveScene();
            var adapter = All<Level4WorldPresentation>().Single();
            Assert.That(adapter.IsApplied, Is.True, "The saved adapter must initialize the local scene on Start.");
            var floors = All<MeshRenderer>().Where(GoldRoomPresentation.IsFloor).ToArray();
            Assert.That(floors.Length, Is.GreaterThan(10));
            var goldFloors = new[] { "FloorWood", "FloorTile", "FloorStone" }
                .Select(n => Resources.Load<Material>("Gold/" + n)).ToArray();
            Assert.That(goldFloors.All(m => m != null && m.mainTexture != null), Is.True);
            Assert.That(floors.All(f => goldFloors.Contains(f.sharedMaterial)), Is.True);
            Assert.That(floors.All(f => !f.enabled && f.GetComponent<Collider>().enabled), Is.True);
            Assert.That(All<GoldFloorSurface>().Length, Is.EqualTo(2), "Each physical floor owns its rendering.");
            Assert.That(player.GetComponents<PlayerIdentityView>().Length, Is.EqualTo(1));
            Assert.That(All<GoldContactShadows>().Length, Is.EqualTo(1));
            Assert.That(All<GoldRoomDecor>().Length, Is.EqualTo(2));
            Assert.That(All<GoldDoorPresentation>().Length, Is.EqualTo(2));
            foreach (var gate in All<Level4AccessGate>())
            {
                Assert.That(gate.MovingPanel, Is.Not.Null);
                var panels = gate.MovingPanel.Find("Level4_GoldGatePanels");
                Assert.That(panels, Is.Not.Null, gate.name);
                Assert.That(panels.GetComponentsInChildren<Renderer>(true), Is.Not.Empty);
                Assert.That(panels.GetComponentsInChildren<Collider>(true).All(c => !c.enabled), Is.True,
                    "Door decoration must not add movement or aiming blockers.");
            }
            foreach (var skin in All<GoldDoorPresentation>())
            {
                Assert.That(skin.Panels.Count, Is.GreaterThan(0));
                Assert.That(skin.Panels.All(r => r.sharedMaterial != null && r.GetComponent<Collider>() == null), Is.True);
            }
            var colliders = All<Collider>().Select(c => c.GetInstanceID()).ToArray();
            var structure = All<Collider>().Where(c => c.GetComponentInParent<EnemyBase>() == null
                && c.GetComponentInParent<SpiritController>() == null
                && c.GetComponentInParent<PossessionChargePickup>() == null
                && c.GetComponentInParent<HealthPickup>() == null).ToArray();
            var bounds = structure.Select(c => c.bounds).ToArray();
            var transforms = structure.Select(c => c.transform.localToWorldMatrix).ToArray();
            int renderers = All<Renderer>().Length;
            adapter.Apply(scene);
            adapter.Apply(scene);
            Assert.That(All<Collider>().Select(c => c.GetInstanceID()), Is.EquivalentTo(colliders));
            Assert.That(All<Renderer>().Length, Is.EqualTo(renderers));
            Assert.That(All<GoldFloorSurface>().Length, Is.EqualTo(2));
            Assert.That(player.GetComponents<PlayerIdentityView>().Length, Is.EqualTo(1));
            Assert.That(All<GoldContactShadows>().Length, Is.EqualTo(1));
            Assert.That(All<GoldRoomDecor>().Length, Is.EqualTo(2));
            for (int i = 0; i < structure.Length; i++)
            {
                Assert.That(structure[i].bounds, Is.EqualTo(bounds[i]), structure[i].name);
                Assert.That(structure[i].transform.localToWorldMatrix, Is.EqualTo(transforms[i]), structure[i].name);
            }
            yield return null;
        }

        [Test]
        public void EngineerEquipmentFitsTheSharedHeadWithoutTheLegacySquareHelmet()
        {
            var engineers = All<EngineerEnemy>();
            Assert.That(engineers.Length, Is.EqualTo(2));
            foreach (var engineer in engineers)
            {
                var avatar = engineer.GetComponent<CharacterAvatarView>();
                Assert.That(avatar, Is.Not.Null);
                var head = avatar.Model.Find("Head").GetComponent<Renderer>().bounds;
                var crown = avatar.Model.Find("Hardhat crown");
                var rim = avatar.Model.Find("Hardhat rim");
                Assert.That(crown, Is.Not.Null);
                Assert.That(rim, Is.Not.Null);
                Assert.That(avatar.Model.Find("Engineer toolkit"), Is.Not.Null);
                Assert.That(avatar.Model.Find("Engineer toolkit handle"), Is.Not.Null);
                Assert.That(avatar.Model.Find("Face panel"), Is.Null);
                foreach (var part in new[] { crown, rim })
                {
                    var bounds = part.GetComponent<Renderer>().bounds;
                    Assert.That(Mathf.Max(bounds.size.x, bounds.size.z),
                        Is.InRange(Mathf.Max(head.size.x, head.size.z) * .9f,
                            Mathf.Max(head.size.x, head.size.z) * 1.5f),
                        "The Engineer's helmet should fit the new head instead of the old capsule width.");
                    Assert.That(bounds.center.y, Is.GreaterThan(head.center.y));
                }
                Assert.That(avatar.Model.GetComponentsInChildren<Collider>(true), Is.Empty);
                foreach (string name in new[] { "Equipment helmet", "Equipment toolkit" })
                    Assert.That(engineer.transform.Find(name).gameObject.activeInHierarchy, Is.False,
                        "Legacy equipment must not overlap the shared avatar.");
            }
        }

        [Test]
        public void DecorativeGateAndSignPartsUseAnOwnedUnitMeshInsteadOfBatchedFloorGeometry()
        {
            var roots = All<Transform>().Where(t => t.name == "Level4_GoldGatePanels" || t.name == "Gold_RoomSign").ToArray();
            Assert.That(roots.Count(t => t.name == "Level4_GoldGatePanels"), Is.EqualTo(4));
            Assert.That(roots.Any(t => t.name == "Gold_RoomSign"), Is.True);
            var parts = roots.SelectMany(t => t.GetComponentsInChildren<MeshFilter>(true)).ToArray();
            Assert.That(parts, Is.Not.Empty);
            var floorMeshes = All<MeshFilter>().Where(f => f.name.StartsWith("Floor_", StringComparison.Ordinal))
                .Select(f => f.sharedMesh).Where(m => m != null).Distinct().ToArray();
            Assert.That(floorMeshes, Is.Not.Empty);
            foreach (var part in parts)
            {
                var mesh = part.sharedMesh;
                Assert.That(mesh, Is.Not.Null, part.name);
                Assert.That(mesh.bounds.center.magnitude, Is.LessThan(.0001f),
                    part.name + " must be centered on its own unit mesh, not a combined scene mesh.");
                Assert.That((mesh.bounds.size - Vector3.one).magnitude, Is.LessThan(.0001f),
                    part.name + " must have unit local bounds before its authored decoration scale is applied.");
                Assert.That(floorMeshes.Contains(mesh), Is.False,
                    "Decoration must own its mesh instead of reusing a static-batched floor's geometry.");
                Assert.That(mesh.isReadable, Is.True, "The runtime-owned primitive must retain valid CPU geometry.");
                Assert.That(mesh.vertexCount, Is.GreaterThanOrEqualTo(8));
                Assert.That(mesh.vertices.All(v => !float.IsNaN(v.x) && !float.IsInfinity(v.x)
                    && !float.IsNaN(v.y) && !float.IsInfinity(v.y) && !float.IsNaN(v.z) && !float.IsInfinity(v.z)), Is.True);
            }
        }

        [UnityTest]
        public IEnumerator BothTargetDoorsClearTheirRenderedLeavesAfterTheirActualTargetsAreHit()
        {
            var skins = All<GoldDoorPresentation>();
            Assert.That(skins.Length, Is.EqualTo(2));
            foreach (var skin in skins)
            {
                var view = skin.GetComponent<SlidingDoorView>();
                var blocker = skin.GetComponent<Collider>();
                Assert.That(view.Opening, Is.Zero);
                Assert.That(view.Targets.Length, Is.EqualTo(3));
                Assert.That(skin.Panels.All(r => r.enabled), Is.True);
                Assert.That(blocker.enabled && !blocker.isTrigger, Is.True);
                Bounds closedBounds = blocker.bounds;
                foreach (var target in view.Targets) target.OnHit();
                for (int frame = 0; frame < 120 && view.Opening < .999f; frame++) yield return null;
                yield return null;
                Assert.That(skin.GetComponent<Door>().IsOpen, Is.True);
                Assert.That(view.Opening, Is.GreaterThanOrEqualTo(.999f));
                Assert.That(blocker.enabled && blocker.isTrigger, Is.True);
                Assert.That(blocker.bounds, Is.EqualTo(closedBounds));
                var leaves = new[] { view.LeftLeaf, view.RightLeaf };
                Assert.That(leaves.SelectMany(t => t.GetComponentsInChildren<Renderer>(true))
                    .Any(r => r.enabled && r.gameObject.activeInHierarchy && !r.forceRenderingOff), Is.False,
                    skin.name + " must not leave visible wood panels or handles across the unlocked passage.");
            }
        }

        [UnityTest]
        public IEnumerator BothGoldFloorsKeepTheirPhysicsAndFollowTheControlledFloorVisibility()
        {
            var visibility = All<Level4FloorVisibility>().Single();
            var surfaces = All<GoldFloorSurface>();
            var lower = surfaces.Single(s => s.transform.parent.name == "Lower floor");
            var upper = surfaces.Single(s => s.transform.parent.name == "Upper floor");
            var floorColliders = All<MeshRenderer>().Where(GoldRoomPresentation.IsFloor)
                .Select(r => r.GetComponent<Collider>()).ToArray();
            var identity = player.GetComponent<PlayerIdentityView>();
            var shadows = All<GoldContactShadows>().Single();
            foreach (var pose in new[] { P(9f, 1.4f, 7f), P(74f, 6.4f, 25f) })
            {
                spirit.RestoreAt(pose);
                yield return null;
                yield return null;
                visibility.RefreshVisibility();
                identity.Refresh();
                shadows.Refresh();
                bool upstairs = pose.y > 5f;
                Assert.That(visibility.UpperVisible, Is.EqualTo(upstairs));
                Assert.That(upper.GetComponentsInChildren<MeshRenderer>().All(r => r.forceRenderingOff == !upstairs), Is.True);
                Assert.That(lower.GetComponentsInChildren<MeshRenderer>().All(r => !r.forceRenderingOff), Is.True);
                Assert.That(floorColliders.All(c => c.enabled && c.gameObject.activeInHierarchy), Is.True);
                Assert.That(identity.TrackedBody, Is.EqualTo(spirit.transform));
                Assert.That(identity.IsShowing, Is.True);
                AssertShadowOnFloor(shadows.PlayerShadow, spirit.GetComponent<Renderer>(), upstairs ? 5f : 0f);
            }
        }

        [UnityTest]
        public IEnumerator StoreyDecorIsVisualOnlyProtectsEngineerControlsAndSurvivesCampaignEntry()
        {
            foreach (bool afterCampaign in new[] { false, true })
            {
                if (afterCampaign)
                {
                    yield return Load(CampaignLevels.FirstScene);
                    yield return Load(CampaignLevels.FourthScene);
                }
                var decorators = All<GoldRoomDecor>();
                Assert.That(decorators.Length, Is.EqualTo(2));
                Assert.That(decorators.Select(d => d.transform.parent.name),
                    Is.EquivalentTo(new[] { "Lower floor", "Upper floor" }));
                var keepouts = All<Level4AccessGate>()
                    .SelectMany(g => new[] { g.transform, g.EngineerPoint, g.ReleasePoint })
                    .Concat(All<Level4Cipher>().SelectMany(c => new[] { c.transform, c.RecoveryPoint }))
                    .Concat(All<Level4Vault>().SelectMany(v => new[] { v.transform, v.OperatorPoint }))
                    .Where(t => t != null).ToArray();
                foreach (var decor in decorators)
                {
                    Assert.That(decor.GetComponentsInChildren<Renderer>(true), Is.Not.Empty,
                        "Both storeys should receive the shared room dressing.");
                    Assert.That(decor.GetComponentsInChildren<Collider>(true), Is.Empty,
                        "Level 4 already has authored solid cover; added dressing must not block routes.");
                    Assert.That(Read<bool>(decor, "includeSolidCover"), Is.False);
                    var protectedPoints = Read<Transform[]>(decor, "objectives");
                    float floorY = decor.transform.parent.name == "Upper floor" ? 5f : 0f;
                    foreach (var point in keepouts.Where(t => Mathf.Abs(t.position.y - floorY) < 2.6f))
                        Assert.That(protectedPoints.Contains(point), Is.True,
                            point.name + " must be protected from decorative placement.");
                    GoldRoomDecor.Install(SceneManager.GetActiveScene(), decor.transform.parent, keepouts, false);
                }
                Assert.That(All<GoldRoomDecor>().Length, Is.EqualTo(2), "Repeated installation must remain idempotent.");
                var upperDecor = decorators.Single(d => d.transform.parent.name == "Upper floor");
                foreach (bool upstairs in new[] { true, false, true })
                {
                    spirit.RestoreAt(upstairs ? P(70f, 6.4f, 25f) : P(9f, 1.4f, 7f));
                    yield return null;
                    yield return null;
                    Assert.That(upperDecor.GetComponentsInChildren<Renderer>(true)
                        .All(r => r.forceRenderingOff == !upstairs), Is.True,
                        "Upper-storey dressing must follow the same cutaway as its physical room.");
                }
            }
        }

        [UnityTest]
        public IEnumerator SharedShadowsHideUpperActorsBelowAndNeverDoubleThePossessedHost()
        {
            var owner = All<GoldContactShadows>().Single();
            Assert.That(owner.GetComponentsInChildren<Renderer>(true).Count(r => r.name == "Gold_PlayerContactShadow"),
                Is.EqualTo(1));
            var npcStamps = owner.GetComponentsInChildren<Renderer>(true)
                .Where(r => r.name == "Gold_EnemyContactShadow").ToArray();
            Assert.That(npcStamps.Length, Is.EqualTo(All<EnemyBase>().Length));
            spirit.RestoreAt(P(70f, 6.4f, 25f));
            yield return null;
            yield return null;
            owner.Refresh();
            var upperActors = All<Level4FloorVisibility>().Single().UpperRoot.GetComponentsInChildren<EnemyBase>(true);
            var upperStamps = upperActors.Select(actor => npcStamps.Single(s => s.enabled
                && Vector2.Distance(new Vector2(s.transform.position.x, s.transform.position.z),
                    new Vector2(actor.transform.position.x, actor.transform.position.z)) < .03f)).ToArray();
            Assert.That(upperStamps.Length, Is.EqualTo(4));
            Assert.That(upperStamps.All(s => Mathf.Abs(s.transform.position.y - 5.025f) < .01f), Is.True);
            spirit.RestoreAt(P(9f, 1.4f, 7f));
            yield return null;
            yield return null;
            owner.Refresh();
            Assert.That(upperStamps.All(s => !s.enabled), Is.True,
                "No upper actor should leave a floating contact stamp over the lower floor.");

            var engineer = ArchiveEngineer();
            yield return PossessEngineerAt(engineer, P(70f, 6.4f, 27.5f));
            owner.Refresh();
            Assert.That(owner.PlayerShadow.enabled, Is.True);
            Assert.That(npcStamps.Count(s => s.enabled && Vector3.Distance(s.transform.position,
                owner.PlayerShadow.transform.position) < .03f), Is.Zero,
                "The controlled host receives the player stamp instead of duplicate player and NPC stamps.");
            Assert.That(player.TryEject(false), Is.True);
            yield return null;
            yield return null;
            owner.Refresh();
            Assert.That(npcStamps.Count(s => s.enabled && Vector2.Distance(
                new Vector2(s.transform.position.x, s.transform.position.z),
                new Vector2(engineer.transform.position.x, engineer.transform.position.z)) < .03f), Is.EqualTo(1));
            Assert.That(All<GoldContactShadows>().Length, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator DisablingAndRestoringAnUpperAvatarKeepsFallbackBodyAndFloorMaskCorrect()
        {
            var engineer = ArchiveEngineer();
            var avatar = engineer.GetComponent<CharacterAvatarView>();
            var capsule = engineer.GetComponent<Renderer>();
            spirit.RestoreAt(P(70f, 6.4f, 25f));
            yield return null;
            yield return null;
            AssertAvatarVisible(engineer);
            avatar.enabled = false;
            yield return null;
            yield return null;
            Assert.That(avatar.Model.gameObject.activeInHierarchy, Is.False);
            Assert.That(capsule.forceRenderingOff, Is.False,
                "A disabled avatar must restore the original body on the visible floor.");
            spirit.RestoreAt(P(9f, 1.4f, 7f));
            yield return null;
            yield return null;
            Assert.That(capsule.forceRenderingOff, Is.True, "Fallback geometry still belongs to the upper floor.");
            spirit.RestoreAt(P(70f, 6.4f, 25f));
            yield return null;
            yield return null;
            Assert.That(capsule.forceRenderingOff, Is.False);
            avatar.enabled = true;
            yield return null;
            yield return null;
            AssertAvatarVisible(engineer);
        }

        [UnityTest]
        public IEnumerator UpperFloorWarningBlinksAfterFloorVisibilityAndRestoresOnEject()
        {
            var engineer = ArchiveEngineer();
            yield return PossessEngineerAt(engineer, P(70f, 6.4f, 27.5f));
            Assert.That(player.GetComponents<PossessedBodyBlink>().Length, Is.EqualTo(1));
            Set(player, "possessTimer", 4.5f);
            var body = engineer.GetComponent<Renderer>();
            Material possessedMaterial = body.sharedMaterial;
            yield return SampleBothBlinkStates(engineer, 90, true);
            Assert.That(body.sharedMaterial, Is.SameAs(possessedMaterial));
            Assert.That(player.TryEject(false), Is.True);
            yield return null;
            AssertAvatarVisible(engineer);
            Assert.That(player.GetComponent<PlayerIdentityView>().TrackedBody, Is.EqualTo(spirit.transform));
        }

        [UnityTest]
        public IEnumerator VaultControllerPauseKeepsTheLiveCountdownBlinkAndSharedHud()
        {
            yield return PossessEngineerAtCipher();
            var engineer = (EngineerEnemy)player.PossessedEnemy;
            Assert.That(All<Level4Cipher>().Single().TryRecover(engineer.transform), Is.True);
            var vault = All<Level4Vault>().Single();
            Position(engineer, vault.OperatorPoint.position + Vector3.back * .4f, Vector3.forward);
            Set(player, "possessTimer", 6f);
            Assert.That(vault.TryBeginInteraction(), Is.True);
            Assert.That(engineer.enabled, Is.False, "The dial pauses movement while the body remains possessed.");
            yield return null;
            yield return null;
            var hud = All<HUDManager>().Single();
            Assert.That(Read<Text>(hud, "healthText").gameObject.activeInHierarchy, Is.True);
            Assert.That(Read<Text>(hud, "levelTimerText").gameObject.activeInHierarchy, Is.True);
            Assert.That(Read<GameObject>(hud, "possessTimerGroup").activeSelf, Is.True);
            var panel = NamedRect("Mechanical lock");
            AssertNoSharedStatusOverlap(panel, hud);
            Assert.That(NamedRect("FloorStatus").gameObject.activeInHierarchy, Is.False);
            Assert.That(NamedRect("ActionPrompt").gameObject.activeInHierarchy, Is.False);
            float remaining = player.PossessTimer;
            yield return SampleBothBlinkStates(engineer, 90, false);
            Assert.That(player.PossessTimer, Is.LessThan(remaining - .5f));
            Assert.That(vault.IsInteracting, Is.True);
            Assert.That(engineer.enabled, Is.False);
            vault.EndInteraction();
            Assert.That(engineer.enabled, Is.True);
            yield return SampleBothBlinkStates(engineer, 60, false);
            Assert.That(player.TryEject(false), Is.True);
            yield return null;
            AssertAvatarVisible(engineer);
            Assert.That(vault.IsInteracting, Is.False);
        }

        private IEnumerator SampleBothBlinkStates(EngineerEnemy engineer, int frames, bool upperFloor)
        {
            bool hidden = false, visible = false;
            var body = engineer.GetComponent<Renderer>();
            var avatar = engineer.GetComponent<CharacterAvatarView>();
            Assert.That(avatar, Is.Not.Null);
            var pieces = avatar.Model.GetComponentsInChildren<Renderer>(true);
            var floor = All<Level4FloorVisibility>().Single();
            for (int frame = 0; frame < frames; frame++)
            {
                yield return null;
                hidden |= avatar.WarningHidden;
                visible |= !avatar.WarningHidden;
                Assert.That(body.enabled, Is.True);
                Assert.That(body.forceRenderingOff, Is.True, "The replaced capsule stays hidden in both warning phases.");
                foreach (var piece in pieces)
                    Assert.That(piece.forceRenderingOff,
                        Is.EqualTo(CharacterAvatarView.IsBodyPiece(piece) && avatar.WarningHidden), piece.name);
                Assert.That(player.PossessedEnemy, Is.SameAs(engineer));
                Assert.That(floor.UpperVisible, Is.EqualTo(upperFloor));
            }
            Assert.That(hidden && visible, Is.True,
                "The real low countdown must alternate visible/hidden frames after floor visibility has run.");
        }

        private IEnumerator PossessEngineerAtCipher()
        {
            var cipher = All<Level4Cipher>().Single();
            yield return PossessEngineerAt(ArchiveEngineer(), cipher.RecoveryPoint.position + Vector3.left * 1.5f);
        }

        private IEnumerator PossessEngineerAt(EngineerEnemy engineer, Vector3 position)
        {
            Position(engineer, position, Vector3.right);
            spirit.RestoreAt(position + Vector3.left * 2.5f);
            engineer.enabled = true;
            yield return null;
            yield return null;
            Physics.SyncTransforms();
            AssertAvatarVisible(engineer);
            Assert.That(player.TryPossess(engineer), Is.True);
            yield return null;
        }

        private IEnumerator Load(string scene)
        {
            Time.timeScale = 1f;
            EventBus.Clear();
            yield return SceneManager.LoadSceneAsync(scene);
            spirit = All<SpiritController>().Single();
            player = spirit.GetComponent<PossessionController>();
            foreach (var enemy in All<EnemyBase>()) enemy.enabled = false;
            foreach (var attack in All<EnemyContactDamage>()) attack.enabled = false;
            foreach (var pressure in All<Level4SentryPressure>()) pressure.enabled = false;
            foreach (var reporter in All<FirebaseReporter>()) Object.DestroyImmediate(reporter.gameObject);
            foreach (var tracker in All<P5TimingTracker>()) tracker.PersistReports = false;
            yield return null;
            yield return null;
        }

        private static string HudSignature(HUDManager hud)
        {
            string signature = "";
            foreach (string name in new[] { "healthText", "levelTimerText", "objectiveText", "messageText" })
            {
                var text = Read<Text>(hud, name);
                var r = text.rectTransform;
                signature += name + r.anchorMin + r.anchorMax + r.pivot + r.anchoredPosition + r.sizeDelta
                    + text.fontSize + text.font.name + text.color;
            }
            return signature;
        }

        private static void AssertAvatarVisible(EnemyBase enemy)
        {
            var avatar = enemy.GetComponent<CharacterAvatarView>();
            Assert.That(avatar, Is.Not.Null);
            Assert.That(avatar.IsShowing, Is.True);
            Assert.That(avatar.WarningHidden, Is.False);
            Assert.That(enemy.GetComponent<Renderer>().forceRenderingOff, Is.True,
                "The visible shared avatar replaces the original capsule.");
            Assert.That(avatar.Model.GetComponentsInChildren<Renderer>(true)
                .All(r => r.enabled && r.gameObject.activeInHierarchy && !r.forceRenderingOff), Is.True,
                "The avatar must be visible on the Spirit's current floor.");
        }

        private static void AssertNoSharedStatusOverlap(RectTransform custom, HUDManager hud)
        {
            Canvas.ForceUpdateCanvases();
            var protectedRects = new[] { Read<Text>(hud, "healthText").rectTransform,
                Read<Text>(hud, "levelTimerText").rectTransform, Read<Text>(hud, "objectiveText").rectTransform,
                hud.GetComponentsInChildren<RectTransform>(true).Single(r => r.name == "ChargePanel") };
            foreach (var status in protectedRects)
                Assert.That(WorldRect(custom).Overlaps(WorldRect(status)), Is.False,
                    custom.name + " overlaps the shared " + status.name);
        }

        private static Rect WorldRect(RectTransform transform)
        {
            var corners = new Vector3[4];
            transform.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners.Min(p => p.x), corners.Min(p => p.y), corners.Max(p => p.x), corners.Max(p => p.y));
        }

        private static void AssertShadowOnFloor(Renderer shadow, Renderer body, float floorY)
        {
            Assert.That(shadow, Is.Not.Null);
            Assert.That(shadow.enabled, Is.True);
            Assert.That(shadow.GetComponent<Collider>(), Is.Null);
            Assert.That(shadow.transform.position.x, Is.EqualTo(body.bounds.center.x).Within(.03f));
            Assert.That(shadow.transform.position.z, Is.EqualTo(body.bounds.center.z).Within(.03f));
            Assert.That(shadow.transform.position.y, Is.EqualTo(floorY + .025f).Within(.01f));
        }

        private static void Position(EnemyBase enemy, Vector3 position, Vector3 forward)
        {
            var cc = enemy.GetComponent<CharacterController>();
            bool enabled = cc.enabled;
            cc.enabled = false;
            enemy.transform.SetPositionAndRotation(position, Quaternion.LookRotation(forward));
            cc.enabled = enabled;
            Physics.SyncTransforms();
        }

        private static RectTransform NamedRect(string name) => All<RectTransform>().Single(r => r.name == name);
        private static EngineerEnemy ArchiveEngineer() => All<EngineerEnemy>().Single(e => e.name == "Engineer_Archive");
        private static Vector3 P(float x, float y, float z) => Level4Layout.Map(new Vector3(x, y, z));
        private static T[] All<T>() where T : Object => Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        private static T Read<T>(object target, string name) => (T)target.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
        private static void Set(object target, string name, object value) => target.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
