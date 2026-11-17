using NUnit.Framework;
using UnityEngine;

namespace Possession.Tests
{
    public class CampaignProgressTests
    {
        private const string ProgressKey = "Possession.LevelsUnlocked";
        private bool hadSavedProgress;
        private int savedProgress;

        [SetUp] public void SetUp()
        {
            hadSavedProgress = PlayerPrefs.HasKey(ProgressKey);
            savedProgress = PlayerPrefs.GetInt(ProgressKey);
            CampaignProgress.Reset();
        }
        [TearDown] public void TearDown()
        {
            if (hadSavedProgress) PlayerPrefs.SetInt(ProgressKey, savedProgress);
            else PlayerPrefs.DeleteKey(ProgressKey);
            PlayerPrefs.Save();
        }

        [Test] public void OnlyFirstLevelOpenByDefault()
        {
            Assert.IsTrue(CampaignProgress.IsUnlocked(0));
            Assert.IsFalse(CampaignProgress.IsUnlocked(1));
            Assert.IsFalse(CampaignProgress.IsUnlocked(2));
            Assert.IsFalse(CampaignProgress.IsUnlocked(3));
        }

        [Test] public void CompletingALevelUnlocksTheNext()
        {
            CampaignProgress.OnLevelCompleted(CampaignLevels.FirstScene);
            Assert.IsTrue(CampaignProgress.IsUnlocked(1));
            Assert.IsFalse(CampaignProgress.IsUnlocked(2));
        }

        [Test] public void ReplayingAnEarlierLevelNeverRelocks()
        {
            CampaignProgress.OnLevelCompleted(CampaignLevels.SecondScene);
            CampaignProgress.OnLevelCompleted(CampaignLevels.FirstScene);
            Assert.IsTrue(CampaignProgress.IsUnlocked(2));
        }

        [TestCase(TutorialScene.Name)]
        [TestCase("MainMenu")]
        [TestCase("Level_01")]
        [TestCase("MissingScene")]
        public void NonCampaignScenesDoNotUnlock(string scene)
        {
            CampaignProgress.OnLevelCompleted(scene);
            Assert.AreEqual(1, CampaignProgress.Unlocked);
            Assert.IsFalse(CampaignProgress.IsUnlocked(1));
        }

        [Test] public void CompletingThirdLevelUnlocksFourthAndPersistsAfterReplay()
        {
            CampaignProgress.OnLevelCompleted(CampaignLevels.ThirdScene);
            Assert.AreEqual(4, CampaignProgress.Unlocked);
            Assert.IsTrue(CampaignProgress.IsUnlocked(3));
            Assert.AreEqual(4, PlayerPrefs.GetInt(ProgressKey));
            CampaignProgress.OnLevelCompleted(CampaignLevels.FirstScene);
            Assert.AreEqual(4, CampaignProgress.Unlocked);
        }

        [Test] public void FinalLevelCannotOverflow()
        {
            CampaignProgress.OnLevelCompleted(CampaignLevels.FourthScene);
            CampaignProgress.OnLevelCompleted(CampaignLevels.FourthScene);
            CampaignProgress.Set(999);
            Assert.AreEqual(CampaignLevels.PlayableSceneNames.Length, CampaignProgress.Unlocked);
            Assert.AreEqual(4, PlayerPrefs.GetInt(ProgressKey));
            Assert.IsFalse(CampaignProgress.IsUnlocked(4));
        }

        [TestCase(-2)]
        [TestCase(-1)]
        [TestCase(4)]
        [TestCase(5)]
        public void InvalidMenuIndicesStayLockedEvenWhenTheCampaignIsOpen(int index)
        {
            CampaignProgress.Set(CampaignLevels.PlayableSceneNames.Length);
            Assert.IsFalse(CampaignProgress.IsUnlocked(index));
        }
    }
}
