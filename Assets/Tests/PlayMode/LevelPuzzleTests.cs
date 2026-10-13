using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Possession.Tests
{
    public class LevelPuzzleTests
    {
        [UnitySetUp]
        public IEnumerator LoadLevel()
        {
            Time.timeScale = 1f;
            EventBus.Clear();
            yield return SceneManager.LoadSceneAsync("Level_01");
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = 1f;
            var level = SceneManager.GetSceneByName("Level_01");
            var empty = SceneManager.CreateScene("P4_TestCleanup");
            SceneManager.SetActiveScene(empty);
            if (level.IsValid() && level.isLoaded)
                yield return SceneManager.UnloadSceneAsync(level);
            EventBus.Clear();
        }

        [Test]
        public void BothPuzzlePassagesAreSealedAcrossTheirWidth()
        {
            Physics.SyncTransforms();
            foreach (float z in new[] { 24f, 33f })
            {
                // Test capsule-width lanes, including the former paths around the wall.
                for (float x = -5.5f; x <= 5.5f; x += 0.5f)
                {
                    Assert.That(Physics.Raycast(new Vector3(x, 1.5f, z - 0.6f),
                        Vector3.forward, 1.2f, ~0, QueryTriggerInteraction.Ignore), Is.True,
                        "Unblocked lane at x=" + x + ", z=" + z);
                }
                Assert.That(Physics.Raycast(new Vector3(0f, 4f, z - 0.6f),
                    Vector3.forward, 1.2f, ~0, QueryTriggerInteraction.Ignore), Is.True,
                    "Spirit can jump over the puzzle at z=" + z);
            }
        }

#if UNITY_EDITOR
        [UnityTest]
        public IEnumerator ProjectileOpensTargetDoor()
        {
            var target = Object.FindFirstObjectByType<Target>();
            var door = GameObject.Find("Door").GetComponent<Door>();
            Assert.That(door.IsOpen, Is.False);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Projectile.prefab");
            var projectile = Object.Instantiate(prefab,
                target.transform.position - Vector3.forward * 2f, Quaternion.identity);
            yield return new WaitForSeconds(0.3f);
            Assert.That(target.IsHit, Is.True);
            Assert.That(door.IsOpen, Is.True);
            Assert.That(door.GetComponent<Collider>().isTrigger, Is.True);
            if (projectile != null) Object.Destroy(projectile);
        }
#endif

        [Test]
        public void HeavySmashBreaksWallAndOpensPath()
        {
            var heavy = Object.FindFirstObjectByType<HeavyEnemy>();
            var wall = Object.FindFirstObjectByType<BreakableWall>();
            var controller = heavy.GetComponent<CharacterController>();
            controller.enabled = false;
            heavy.transform.SetPositionAndRotation(new Vector3(0f, 1.4f, 31.5f), Quaternion.identity);
            controller.enabled = true;
            heavy.OnPossessed();
            Physics.SyncTransforms();
            heavy.GetComponent<SmashAbility>().TryExecute();
            Assert.That(wall.CurrentHP, Is.LessThanOrEqualTo(0));
            Assert.That(wall.gameObject.activeSelf, Is.False);
            Physics.SyncTransforms();
            Assert.That(Physics.Raycast(new Vector3(0f, 1.5f, 32.5f),
                Vector3.forward, 1f, ~0, QueryTriggerInteraction.Ignore), Is.False);
        }

        [Test]
        public void ExitRequiresBothPuzzles()
        {
            var exit = Object.FindFirstObjectByType<LevelExit>();
            var target = Object.FindFirstObjectByType<Target>();
            var wall = Object.FindFirstObjectByType<BreakableWall>();
            Assert.That(exit.IsReady, Is.False);
            target.OnHit();
            Assert.That(exit.IsReady, Is.False);
            wall.TakeDamage(wall.CurrentHP);
            Assert.That(exit.IsReady, Is.True);
        }

        [UnityTest]
        public IEnumerator SpiritCannotWinBeforePuzzles()
        {
            var spirit = Object.FindFirstObjectByType<SpiritController>();
            spirit.RestoreAt(new Vector3(0f, 1.2f, 35.5f));
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(GameManager.Instance.CurrentState, Is.EqualTo(LevelState.InProgress));
        }

        [UnityTest]
        public IEnumerator SpiritWinsOnceAfterBothPuzzles()
        {
            OpenPuzzles();
            int completions = 0;
            EventBus.LevelCompleted += () => completions++;
            var spirit = Object.FindFirstObjectByType<SpiritController>();
            spirit.RestoreAt(new Vector3(0f, 1.2f, 35.5f));
            yield return new WaitForSecondsRealtime(0.1f);
            Assert.That(GameManager.Instance.CurrentState, Is.EqualTo(LevelState.Complete));
            Assert.That(completions, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator HeavyCannotCompleteTheLevel()
        {
            OpenPuzzles();
            var heavy = Object.FindFirstObjectByType<HeavyEnemy>();
            var controller = heavy.GetComponent<CharacterController>();
            controller.enabled = false;
            heavy.transform.position = new Vector3(0f, 1.4f, 35.5f);
            controller.enabled = true;
            heavy.OnPossessed();
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.That(GameManager.Instance.CurrentState, Is.EqualTo(LevelState.InProgress));
        }

        [UnityTest]
        public IEnumerator EjectingInsideExitCompletesTheLevel()
        {
            OpenPuzzles();
            var spirit = Object.FindFirstObjectByType<SpiritController>();
            var possession = spirit.GetComponent<PossessionController>();
            var heavy = Object.FindFirstObjectByType<HeavyEnemy>();
            typeof(PossessionController).GetMethod("Possess", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(possession, new object[] { heavy });
            var controller = heavy.GetComponent<CharacterController>();
            controller.enabled = false;
            heavy.transform.SetPositionAndRotation(new Vector3(0f, 1.4f, 35.2f), Quaternion.identity);
            controller.enabled = true;
            yield return new WaitForFixedUpdate();
            Assert.That(GameManager.Instance.CurrentState, Is.EqualTo(LevelState.InProgress));
            typeof(PossessionController).GetMethod("Eject", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(possession, new object[] { false });
            Assert.That(possession.CurrentState, Is.EqualTo(PlayerState.Spirit));
            yield return new WaitForSecondsRealtime(0.1f);
            Assert.That(GameManager.Instance.CurrentState, Is.EqualTo(LevelState.Complete));
        }

        private static void OpenPuzzles()
        {
            Object.FindFirstObjectByType<Target>().OnHit();
            var wall = Object.FindFirstObjectByType<BreakableWall>();
            wall.TakeDamage(wall.CurrentHP);
        }
    }
}
