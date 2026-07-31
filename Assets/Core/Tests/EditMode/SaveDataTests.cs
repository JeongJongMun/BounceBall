using NUnit.Framework;

namespace Core.Tests
{
    public class SaveDataTests
    {
        [SetUp] public void SetUp() => SaveData.ResetAll();
        [TearDown] public void TearDown() => SaveData.ResetAll();

        [Test]
        public void HighScore_기본값은_0()
        {
            Assert.AreEqual(0, SaveData.HighScore);
        }

        [Test]
        public void HighScore_저장하면_읽을_수_있다()
        {
            SaveData.HighScore = 1234;
            Assert.AreEqual(1234, SaveData.HighScore);
        }

        [Test]
        public void 볼륨_기본값은_1()
        {
            Assert.AreEqual(1f, SaveData.MasterVolume);
            Assert.AreEqual(1f, SaveData.BgmVolume);
            Assert.AreEqual(1f, SaveData.SfxVolume);
        }

        [Test]
        public void ResetAll_후_기본값으로_복귀()
        {
            SaveData.HighScore = 99;
            SaveData.SfxVolume = 0.5f;
            SaveData.ResetAll();
            Assert.AreEqual(0, SaveData.HighScore);
            Assert.AreEqual(1f, SaveData.SfxVolume);
        }
    }
}
