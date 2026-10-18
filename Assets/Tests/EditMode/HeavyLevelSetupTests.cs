using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Possession.Tests
{
    public class HeavyLevelSetupTests
    {
        private Scene scene;

        [SetUp]
        public void SetUp()
        {
            scene = EditorSceneManager.NewPreviewScene();
        }

        [TearDown]
        public void TearDown()
        {
            if (scene.IsValid()) EditorSceneManager.ClosePreviewScene(scene);
        }

        [TestCase("Level_02", 1, 2f)]
        [TestCase("Level_03", 2, 2.2f)]
        public void ReapplyingSetupPreservesLayoutAndDoesNotDuplicateEnemies(string level, int count, float speed)
        {
            scene.name = level;
            var marker = Create("P4_LayoutSentinel", new Vector3(4f, 3f, 5f));
            var guardMarker = Create("SpawnPoint_Guard_01", new Vector3(-2f, 1.1f, 8f));
            Create("SpawnPoint_Heavy_01", new Vector3(-3f, 1.3f, 20f));
            if (count == 2) Create("SpawnPoint_Heavy_02", new Vector3(0f, 1.3f, 32f));
            CreateWall(count == 1 ? "BreakableWall" : "BreakableWall_01", 27f);
            if (count == 2) CreateWall("BreakableWall_02", 36f);
            Configure();
            var firstIds = Heavies().Select(enemy => enemy.GetInstanceID()).OrderBy(id => id).ToArray();
            var firstWaypoints = Heavies().SelectMany(enemy => new SerializedObject(enemy)
                .FindProperty("waypoints").CastReferences()).Select(point => point.GetInstanceID()).OrderBy(id => id).ToArray();
            Configure();
            var heavies = Heavies();
            Assert.That(heavies.Length, Is.EqualTo(count));
            Assert.That(heavies.Select(enemy => enemy.GetInstanceID()).OrderBy(id => id), Is.EqualTo(firstIds));
            var waypoints = heavies.SelectMany(enemy => new SerializedObject(enemy).FindProperty("waypoints")
                .CastReferences()).ToArray();
            Assert.That(waypoints.Length, Is.EqualTo(count * 2));
            Assert.That(waypoints.Select(point => point.GetInstanceID()).OrderBy(id => id), Is.EqualTo(firstWaypoints));
            Assert.That(marker.transform.position, Is.EqualTo(new Vector3(4f, 3f, 5f)));
            Assert.That(guardMarker.transform.position, Is.EqualTo(new Vector3(-2f, 1.1f, 8f)));
            Assert.That(scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<BreakableWall>(true))
                .Count(), Is.EqualTo(count));
            foreach (var heavy in heavies)
            {
                var settings = new SerializedObject(heavy);
                Assert.That(settings.FindProperty("moveSpeed").floatValue, Is.EqualTo(speed));
                Assert.That(settings.FindProperty("maxHP").intValue, Is.EqualTo(5));
                Assert.That(heavy.GetComponent<EnemyContactDamage>(), Is.Not.Null);
                Assert.That(heavy.GetComponent<CharacterController>().minMoveDistance, Is.Zero);
            }
        }

        [Test]
        public void MissingSpawnDoesNotPartiallyPopulateScene()
        {
            scene.name = "Level_02";
            CreateWall("BreakableWall", 27f);
            var exception = Assert.Throws<TargetInvocationException>(() => Configure());
            Assert.That(exception.InnerException, Is.TypeOf<InvalidOperationException>());
            Assert.That(Heavies(), Is.Empty);
            Assert.That(scene.GetRootGameObjects().Any(root => root.name == "P3_Heavies"), Is.False);
        }

        [Test]
        public void Level01StatsKeepExistingPatrolReferences()
        {
            scene.name = "Level_01";
            var heavy = Create("Heavy", new Vector3(0f, 1.3f, 30f)).AddComponent<HeavyEnemy>();
            var point = Create("Existing waypoint", new Vector3(2f, 1.3f, 31f));
            var settings = new SerializedObject(heavy);
            settings.FindProperty("waypoints").arraySize = 1;
            settings.FindProperty("waypoints").GetArrayElementAtIndex(0).objectReferenceValue = point.transform;
            settings.ApplyModifiedPropertiesWithoutUndo();
            Configure();
            settings.Update();
            Assert.That(settings.FindProperty("moveSpeed").floatValue, Is.EqualTo(1.8f));
            Assert.That(settings.FindProperty("maxHP").intValue, Is.EqualTo(5));
            Assert.That(settings.FindProperty("waypoints").arraySize, Is.EqualTo(1));
            Assert.That(settings.FindProperty("waypoints").GetArrayElementAtIndex(0).objectReferenceValue,
                Is.EqualTo(point.transform));
        }

        private HeavyEnemy[] Heavies() => scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<HeavyEnemy>(true)).ToArray();

        private GameObject Create(string name, Vector3 position)
        {
            var go = new GameObject(name);
            SceneManager.MoveGameObjectToScene(go, scene);
            go.transform.position = position;
            return go;
        }

        private void CreateWall(string name, float z)
        {
            var go = Create(name, new Vector3(0f, 3f, z));
            go.AddComponent<BoxCollider>();
            go.AddComponent<BreakableWall>();
        }

        // Default Editor assembly cannot be referenced by an asmdef test assembly.
        private void Configure() => Type.GetType("Possession.Editor.HeavyLevelSetup, Assembly-CSharp-Editor", true)
            .GetMethod("ConfigureScene", BindingFlags.Static | BindingFlags.Public).Invoke(null, new object[] { scene });
    }

    internal static class P3SerializedArray
    {
        public static Object[] CastReferences(this SerializedProperty array)
        {
            var values = new Object[array.arraySize];
            for (int i = 0; i < values.Length; i++) values[i] = array.GetArrayElementAtIndex(i).objectReferenceValue;
            return values;
        }
    }
}
