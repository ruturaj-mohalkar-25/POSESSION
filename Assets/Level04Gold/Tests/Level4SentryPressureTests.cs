using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Possession.Level04Gold.Tests
{
    public sealed class Level4SentryPressureTests
    {
        private readonly List<GameObject> objects = new List<GameObject>();
        private SpiritController spirit;
        private PossessionController possession;
        private GuardEnemy sentry;
        private EnemyBase host;
        private Level4SentryPressure pressure;
        private Projectile template;

        [SetUp]
        public void Setup()
        {
            Time.timeScale = 1f;
            EventBus.Clear();
            var floor = Make("Pressure fixture floor", new Vector3(0f, -.5f, 0f));
            floor.AddComponent<BoxCollider>().size = new Vector3(30f, 1f, 30f);
        }

        [TearDown]
        public void Cleanup()
        {
            Time.timeScale = 1f;
            foreach (var shot in Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None))
                Object.DestroyImmediate(shot.gameObject);
            for (int i = objects.Count - 1; i >= 0; i--)
                if (objects[i] != null) Object.DestroyImmediate(objects[i]);
            objects.Clear();
            EventBus.Clear();
        }

        [UnityTest]
        public IEnumerator SharedProjectileDamagesTheControlledEngineerAfterTheVisibleWarning()
        {
            yield return PrepareControlledHost<EngineerEnemy>();
            int bodyHP = host.CurrentHP, spiritHP = spirit.CurrentHP;
            yield return new WaitForSeconds(.2f);
            Assert.That(pressure.TargetInSight, Is.True);
            Assert.That(pressure.IsWindingUp, Is.True);
            Assert.That(LiveShots(), Is.Empty, "A visible warning must precede the real projectile.");
            Assert.That(host.CurrentHP, Is.EqualTo(bodyHP));
            yield return new WaitForSeconds(1.3f);
            Assert.That(host.CurrentHP, Is.EqualTo(bodyHP - 1));
            Assert.That(spirit.CurrentHP, Is.EqualTo(spiritHP), "The exposed controlled body takes this shot.");
            Assert.That(possession.PossessedEnemy, Is.SameAs(host));
        }

        [UnityTest]
        public IEnumerator SolidCoverInterceptsTheRealShotAndStopsFurtherLockOn()
        {
            yield return PrepareControlledHost<EngineerEnemy>();
            int hp = host.CurrentHP;
            yield return new WaitForSeconds(.95f);
            Assert.That(LiveShots(), Is.Not.Empty, "This test must intercept a shot already in flight.");
            var cover = Make("New solid cover in the bullet path", new Vector3(0f, 1.5f, 4f));
            cover.AddComponent<BoxCollider>().size = new Vector3(5f, 3f, .3f);
            Physics.SyncTransforms();
            yield return new WaitForSeconds(.8f);
            Assert.That(host.CurrentHP, Is.EqualTo(hp));
            Assert.That(LiveShots(), Is.Empty, "The shared swept projectile must stop at the new cover.");
            Assert.That(pressure.TargetInSight, Is.False);
            Assert.That(pressure.IsWindingUp, Is.False);
        }

        [UnityTest]
        public IEnumerator EjectingTheThreatenedEngineerCancelsLockOnAndLeavesSpiritUntargeted()
        {
            yield return PrepareControlledHost<EngineerEnemy>();
            int hp = host.CurrentHP, spiritHP = spirit.CurrentHP;
            yield return new WaitForSeconds(.2f);
            Assert.That(possession.CurrentState, Is.EqualTo(PlayerState.Possessed_Engineer));
            Assert.That(pressure.TargetInSight, Is.True);
            Assert.That(pressure.IsWindingUp, Is.True);
            Assert.That(LiveShots(), Is.Empty);
            Assert.That(host.CurrentHP, Is.EqualTo(hp));
            Assert.That(possession.TryEject(false), Is.True);
            yield return null;
            Assert.That(pressure.IsWindingUp, Is.False);
            yield return new WaitForSeconds(1.2f);
            Assert.That(possession.CurrentState, Is.EqualTo(PlayerState.Spirit));
            Assert.That(pressure.TargetInSight, Is.False);
            Assert.That(pressure.IsWindingUp, Is.False);
            Assert.That(LiveShots(), Is.Empty);
            Assert.That(spirit.CurrentHP, Is.EqualTo(spiritHP));
        }

        [UnityTest]
        public IEnumerator PauseCancelsTheWarningAndADeadSentryCannotResumeIt()
        {
            yield return PrepareControlledHost<EngineerEnemy>();
            int hp = host.CurrentHP;
            yield return new WaitForSeconds(.15f);
            Assert.That(pressure.IsWindingUp, Is.True);
            Time.timeScale = 0f;
            yield return null;
            Assert.That(pressure.IsWindingUp, Is.False);
            Assert.That(pressure.TargetInSight, Is.False);
            yield return new WaitForSecondsRealtime(.8f);
            Assert.That(LiveShots(), Is.Empty);
            Assert.That(host.CurrentHP, Is.EqualTo(hp));
            Time.timeScale = 1f;
            yield return null;
            Assert.That(pressure.IsWindingUp, Is.True);
            sentry.TakeDamage(sentry.CurrentHP);
            yield return new WaitForSeconds(1.1f);
            Assert.That(pressure.IsWindingUp, Is.False);
            Assert.That(pressure.TargetInSight, Is.False);
            Assert.That(LiveShots(), Is.Empty);
            Assert.That(host.CurrentHP, Is.EqualTo(hp));
        }

        private IEnumerator PrepareControlledHost<T>() where T : EnemyBase
        {
            sentry = Host<GuardEnemy>("Warning sentry", new Vector3(0f, 1.1f, 0f));
            Set(sentry, "detectionRange", 12f);
            Set(sentry, "visionAngle", 90f);
            var muzzle = Make("Pressure muzzle", new Vector3(0f, 1.1f, .8f));
            muzzle.transform.SetParent(sentry.transform, true);
            var prefab = Make("Shared pressure projectile", new Vector3(-100f, 2f, -100f));
            template = prefab.AddComponent<Projectile>();
            pressure = sentry.gameObject.AddComponent<Level4SentryPressure>();
            Set(pressure, "firePoint", muzzle.transform);
            Set(pressure, "projectilePrefab", prefab);
            host = Host<T>("Controlled pressure target", new Vector3(0f, 1.1f, 7f));
            var player = Make("Pressure fixture Spirit", new Vector3(0f, 1.1f, 4.5f));
            player.SetActive(false);
            player.AddComponent<CapsuleCollider>().height = 2f;
            player.AddComponent<Rigidbody>().useGravity = false;
            spirit = player.AddComponent<SpiritController>();
            possession = player.AddComponent<PossessionController>();
            Set(possession, "enemyLayer", (LayerMask)(1 << 6));
            player.SetActive(true);
            yield return null;
            Physics.SyncTransforms();
            Assert.That(possession.TryPossess(host), Is.True,
                "The pressure target must be a body controlled through the real possession API.");
            yield return null;
        }

        private T Host<T>(string name, Vector3 position) where T : EnemyBase
        {
            var go = Make(name, position);
            go.SetActive(false);
            go.layer = 6;
            var enemy = go.AddComponent<T>();
            var cc = go.GetComponent<CharacterController>();
            cc.height = 2f; cc.radius = .5f; cc.minMoveDistance = 0f;
            enemy.MoveSpeed = 0f;
            go.SetActive(true);
            return enemy;
        }

        private Projectile[] LiveShots() => Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None)
            .Where(p => p != template).ToArray();

        private GameObject Make(string name, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            objects.Add(go);
            return go;
        }

        private static void Set(object target, string name, object value)
        {
            for (var type = target.GetType(); type != null; type = type.BaseType)
            {
                var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (field == null) continue;
                field.SetValue(target, value);
                return;
            }
            throw new MissingFieldException(target.GetType().Name, name);
        }
    }
}
