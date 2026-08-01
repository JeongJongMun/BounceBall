using NUnit.Framework;

namespace Game.Tests
{
    public class StageSelectUITests
    {
        [Test]
        public void 전부_미클리어면_0번이_첫_미클리어다()
        {
            Assert.AreEqual(0, StageUI.FindFirstUnclearedIndex(new[] { false, false, false }));
        }

        [Test]
        public void 앞쪽이_클리어된_만큼_다음_인덱스가_첫_미클리어다()
        {
            Assert.AreEqual(2, StageUI.FindFirstUnclearedIndex(new[] { true, true, false, false }));
        }

        [Test]
        public void 전부_클리어면_음수를_반환한다()
        {
            Assert.AreEqual(-1, StageUI.FindFirstUnclearedIndex(new[] { true, true, true }));
        }
    }
}
