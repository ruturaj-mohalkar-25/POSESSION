using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Possession.Tests
{
    public sealed class Level4HostTests
    {
        private readonly List<GameObject> objects = new List<GameObject>();

        [SetUp]
        public void Setup()
        {
            Time.timeScale = 1f;
            EventBus.Clear();
            var ground = Make("Host test floor", new Vector3(0f, -.5f, 0f));
            ground.AddComponent<BoxCollider>().size = new Vector3(40f, 1f, 40f);
        }

        [TearDown]
        public void Cleanup()
        {
            Time.timeScale = 1f;
            for (int i = objects.Count - 1; i >= 0; i--)
                if (objects[i] != null) Object.DestroyImmediate(objects[i]);
            objects.Clear();
            EventBus.Clear();
        }

        [Test]
        public void EngineerPreservesExistingSerializedStateValues()
        {
            Assert.That((int)PlayerState.Spirit, Is.EqualTo(0));
            Assert.That((int)PlayerState.Possessed_Guard, Is.EqualTo(1));
            Assert.That((int)PlayerState.Possessed_Heavy, Is.EqualTo(2));
            Assert.That((int)PlayerState.Possessed_Drone, Is.EqualTo(3));
            Assert.That((int)PlayerState.Possessed_Engineer, Is.EqualTo(5));
            Assert.That(Enum.IsDefined(typeof(PlayerState), 4), Is.False);
        }

        [UnityTest]
        public IEnumerator EngineerUsesSharedPossessionChargesAndProtection()
        {
            yield return CheckSharedPossession<EngineerEnemy>(PlayerState.Possessed_Engineer);
        }

        private IEnumerator CheckSharedPossession<T>(PlayerState expected) where T : EnemyBase
        {
            var host = Host<T>(new Vector3(0f, 1.1f, 0f));
            var spirit = Spirit(new Vector3(-3f, 1.1f, 0f));
            var possession = spirit.GetComponent<PossessionController>();
            var charges = spirit.GetComponent<PossessionCharge>();
            yield return null;
            Physics.SyncTransforms();
            Assert.That(charges.Count, Is.EqualTo(1));
            Assert.That(possession.TryPossess(host), Is.True);
            Assert.That(possession.CurrentState, Is.EqualTo(expected));
            Assert.That(charges.Count, Is.Zero);
            Assert.That(charges.MaxCharges, Is.EqualTo(3));
            Assert.That(host.GetComponent<ShootAbility>(), Is.Null);
            Assert.That(host.GetComponent<SmashAbility>(), Is.Null);
            Assert.That(possession.TryEject(false), Is.True);
            Assert.That(possession.CurrentState, Is.EqualTo(PlayerState.Spirit));
            Assert.That(possession.IsEjectShielded, Is.True);
            Assert.That(spirit.DamageProtectionRemaining, Is.GreaterThan(1.9f));
            Assert.That(host.CanBePossessed, Is.True);
        }

        [UnityTest]
        public IEnumerator PossessedEngineerStillCollidesWithOrdinarySolidWalls()
        {
            var wall = Wall();
            var engineer = Host<EngineerEnemy>(new Vector3(-1.5f, 1.1f, 0f));
            var spirit = Spirit(new Vector3(-4f, 1.1f, 0f));
            yield return null;
            Physics.SyncTransforms();
            Assert.That(spirit.GetComponent<PossessionController>().TryPossess(engineer), Is.True);
            var body = engineer.GetComponent<CharacterController>();
            Assert.That(Physics.GetIgnoreCollision(body, wall), Is.False);
            body.Move(Vector3.right * 4f);
            Assert.That(engineer.transform.position.x, Is.LessThan(-.6f));
            Assert.That(Physics.ComputePenetration(body, body.transform.position, body.transform.rotation,
                wall, wall.transform.position, wall.transform.rotation, out _, out _), Is.False);
        }

        [UnityTest]
        public IEnumerator ManualEjectionNearAWallKeepsBothBodiesClearAndProtectsSpirit()
        {
            yield return CheckEjectionNearWall(false);
        }

        [UnityTest]
        public IEnumerator ForcedEjectionNearAWallKeepsClearanceAndTheSharedShield()
        {
            yield return CheckEjectionNearWall(true);
        }

        [UnityTest]
        public IEnumerator EngineerDeathRestoresSpiritThroughTheSharedController()
        {
            var engineer = Host<EngineerEnemy>(new Vector3(0f, 1.1f, 0f));
            var spirit = Spirit(new Vector3(-3f, 1.1f, 0f));
            var possession = spirit.GetComponent<PossessionController>();
            yield return null;
            Assert.That(possession.TryPossess(engineer), Is.True);
            int hp = engineer.CurrentHP;
            engineer.TakeDamage(1);
            Assert.That(engineer.CurrentHP, Is.EqualTo(hp - 1));
            engineer.TakeDamage(engineer.CurrentHP);
            yield return null;
            Assert.That(engineer.gameObject.activeSelf, Is.False);
            Assert.That(possession.CurrentState, Is.EqualTo(PlayerState.Spirit));
            Assert.That(possession.PossessedEnemy, Is.Null);
            Assert.That(spirit.GetComponent<Collider>().enabled, Is.True);
            Assert.That(spirit.IsDamageProtected, Is.True);
        }

        private IEnumerator CheckEjectionNearWall(bool forced)
        {
            var wall = Wall();
            var engineer = Host<EngineerEnemy>(new Vector3(-.85f, 1.1f, 0f));
            var spirit = Spirit(new Vector3(-3.5f, 1.1f, -1.5f));
            var possession = spirit.GetComponent<PossessionController>();
            yield return null;
            Physics.SyncTransforms();
            Assert.That(possession.TryPossess(engineer), Is.True);
            var body = engineer.GetComponent<CharacterController>();
            body.Move(Vector3.right);
            Assert.That(possession.TryEject(forced), Is.True);
            yield return null;
            Physics.SyncTransforms();
            var spiritBody = spirit.GetComponent<CapsuleCollider>();
            Assert.That(Physics.ComputePenetration(body, body.transform.position, body.transform.rotation,
                wall, wall.transform.position, wall.transform.rotation, out _, out _), Is.False);
            Assert.That(Physics.ComputePenetration(spiritBody, spirit.transform.position, spirit.transform.rotation,
                wall, wall.transform.position, wall.transform.rotation, out _, out _), Is.False);
            Assert.That(body.bounds.Intersects(spiritBody.bounds), Is.False);
            Assert.That(Physics.GetIgnoreCollision(body, wall), Is.False);
            Assert.That(engineer.IsHostile, Is.EqualTo(forced));
            Assert.That(spirit.transform.position.y, Is.InRange(1f, 1.4f));
            Assert.That(spirit.IsDamageProtected, Is.True);
            int hp = spirit.CurrentHP;
            spirit.TakeDamage(1);
            Assert.That(spirit.CurrentHP, Is.EqualTo(hp));
        }

        private BoxCollider Wall()
        {
            var go = Make("Ordinary solid wall", new Vector3(0f, 1.5f, 0f));
            var collider = go.AddComponent<BoxCollider>();
            collider.size = new Vector3(.35f, 3f, 10f);
            return collider;
        }

        private T Host<T>(Vector3 position) where T : EnemyBase
        {
            var go = Make(typeof(T).Name, position);
            go.SetActive(false);
            go.layer = 6;
            var host = go.AddComponent<T>();
            var cc = go.GetComponent<CharacterController>();
            cc.height = 2f;
            cc.radius = .5f;
            cc.minMoveDistance = 0f;
            go.SetActive(true);
            return host;
        }

        private SpiritController Spirit(Vector3 position)
        {
            var go = Make("Spirit", position);
            go.SetActive(false);
            go.AddComponent<CapsuleCollider>().height = 2f;
            go.AddComponent<Rigidbody>().useGravity = false;
            var spirit = go.AddComponent<SpiritController>();
            var possession = go.AddComponent<PossessionController>();
            Set(possession, "enemyLayer", (LayerMask)(1 << 6));
            go.AddComponent<PossessionCharge>();
            go.SetActive(true);
            return spirit;
        }

        private GameObject Make(string name, Vector3 position)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            objects.Add(go);
            return go;
        }

        private static void Set(object target, string name, object value) => target.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
