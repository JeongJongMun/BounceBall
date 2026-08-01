using NUnit.Framework;

namespace Game.Tests
{
    public class QuickSlotDragStateTests
    {
        [SetUp]
        [TearDown]
        public void Reset() => QuickSlotDragState.End();

        [Test]
        public void 기본은_드래그_중이_아니다()
        {
            Assert.IsFalse(QuickSlotDragState.IsDragging);
            Assert.IsNull(QuickSlotDragState.DraggingItemId);
        }

        [Test]
        public void 드래그를_시작하면_아이템이_기록된다()
        {
            QuickSlotDragState.Begin("Property_Jelly");
            Assert.IsTrue(QuickSlotDragState.IsDragging);
            Assert.AreEqual("Property_Jelly", QuickSlotDragState.DraggingItemId);
        }

        [Test]
        public void 종료하면_상태가_비워진다()
        {
            QuickSlotDragState.Begin("Property_Ice");
            QuickSlotDragState.End();
            Assert.IsFalse(QuickSlotDragState.IsDragging);
        }

        [Test]
        public void 시작과_종료에_알림이_온다()
        {
            bool? received = null;
            void Handler(bool dragging) => received = dragging;

            QuickSlotDragState.OnDragChanged += Handler;
            QuickSlotDragState.Begin("Property_Jelly");
            Assert.AreEqual(true, received);

            QuickSlotDragState.End();
            Assert.AreEqual(false, received);
            QuickSlotDragState.OnDragChanged -= Handler;
        }

        [Test]
        public void 빈_아이템으로는_시작하지_않는다()
        {
            QuickSlotDragState.Begin("");
            Assert.IsFalse(QuickSlotDragState.IsDragging);
        }
    }
}
