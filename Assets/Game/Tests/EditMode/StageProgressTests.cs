using NUnit.Framework;

namespace Game.Tests
{
    public class StageProgressTests
    {
        private const string StageA = "__TestStageA";
        private const string StageB = "__TestStageB";

        [SetUp]
        [TearDown]
        public void ResetAll()
        {
            StageProgress.ResetAll(new[] { StageA, StageB });
        }

        [Test]
        public void 기본값은_미클리어다()
        {
            Assert.IsFalse(StageProgress.IsCleared(StageA));
        }

        [Test]
        public void SetCleared_하면_클리어_상태가_저장된다()
        {
            StageProgress.SetCleared(StageA);
            Assert.IsTrue(StageProgress.IsCleared(StageA));
        }

        [Test]
        public void 스테이지별로_독립적으로_저장된다()
        {
            StageProgress.SetCleared(StageA);
            Assert.IsTrue(StageProgress.IsCleared(StageA));
            Assert.IsFalse(StageProgress.IsCleared(StageB));
        }
    }
}
