using NUnit.Framework;

namespace Game.Tests
{
    public class UIPopupStateTests
    {
        private readonly object _a = new();
        private readonly object _b = new();

        [SetUp]
        [TearDown]
        public void Reset() => UIPopupState.Clear();

        [Test]
        public void 기본은_열린_팝업이_없다()
        {
            Assert.IsFalse(UIPopupState.IsAnyOpen);
        }

        [Test]
        public void 팝업이_열리면_열림_상태다()
        {
            UIPopupState.SetOpen(_a, true);
            Assert.IsTrue(UIPopupState.IsAnyOpen);
        }

        [Test]
        public void 여러_팝업_중_하나라도_열려_있으면_열림이다()
        {
            UIPopupState.SetOpen(_a, true);
            UIPopupState.SetOpen(_b, true);
            UIPopupState.SetOpen(_a, false);

            Assert.IsTrue(UIPopupState.IsAnyOpen, "아직 b가 열려 있다.");

            UIPopupState.SetOpen(_b, false);
            Assert.IsFalse(UIPopupState.IsAnyOpen);
        }

        [Test]
        public void 같은_팝업을_두_번_닫아도_음수가_되지_않는다()
        {
            UIPopupState.SetOpen(_a, true);
            UIPopupState.SetOpen(_a, false);
            UIPopupState.SetOpen(_a, false);
            Assert.IsFalse(UIPopupState.IsAnyOpen);
        }
    }
}
