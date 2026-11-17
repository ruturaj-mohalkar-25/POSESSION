using NUnit.Framework;

namespace Possession.Tests
{
    public class CampaignProgressTests
    {
        [SetUp] public void SetUp() => CampaignProgress.Reset();
        [TearDown] public void TearDown() => CampaignProgress.Reset();

        [Test] public void OnlyFirstLevelOpenByDefault()
        {
            Assert.IsTrue(CampaignProgress.IsUnlocked(0));
            Assert.IsFalse(CampaignProgress.IsUnlocked(1));
            Assert.IsFalse(CampaignProgress.IsUnlocked(2));
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

        [Test] public void NonCampaignScenesDoNotUnlock()
        {
            CampaignProgress.OnLevelCompleted("Tutorial");
            Assert.IsFalse(CampaignProgress.IsUnlocked(1));
        }

        [Test] public void FinalLevelCannotOverflow()
        {
            CampaignProgress.OnLevelCompleted(CampaignLevels.ThirdScene);
            Assert.AreEqual(CampaignLevels.SceneNames.Length, CampaignProgress.Unlocked);
        }
    }
}
