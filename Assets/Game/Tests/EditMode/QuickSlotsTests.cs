using NUnit.Framework;

namespace Game.Tests
{
    public class QuickSlotsTests
    {
        private const string Jelly = "Property_Jelly";
        private const string Ice = "Property_Ice";

        [SetUp]
        [TearDown]
        public void Reset()
        {
            QuickSlots.ResetAll();
            Inventory.ResetAll();
        }

        [Test]
        public void 초기_퀵슬롯은_비어_있다()
        {
            for (int i = 0; i < QuickSlots.SlotCount; i++) Assert.IsTrue(QuickSlots.IsEmpty(i));
        }

        [Test]
        public void 등록하면_해당_칸에서_조회된다()
        {
            Inventory.Add(Jelly, 2);
            QuickSlots.Register(0, Jelly);

            Assert.AreEqual(Jelly, QuickSlots.GetItemId(0));
            Assert.AreEqual(0, QuickSlots.IndexOf(Jelly));
        }

        [Test]
        public void 등록해도_인벤토리_수량은_줄지_않는다()
        {
            Inventory.Add(Jelly, 4);
            QuickSlots.Register(1, Jelly);
            Assert.AreEqual(4, Inventory.GetCount(Jelly));
        }

        [Test]
        public void 같은_아이템을_다른_칸에_등록하면_기존_칸이_비워진다()
        {
            Inventory.Add(Jelly, 1);
            QuickSlots.Register(0, Jelly);
            QuickSlots.Register(2, Jelly);

            Assert.IsTrue(QuickSlots.IsEmpty(0), "이전 칸은 비워져야 한다 (문서 §5.6.4).");
            Assert.AreEqual(Jelly, QuickSlots.GetItemId(2));
        }

        [Test]
        public void 수량이_0이_되면_자동으로_해제된다()
        {
            Inventory.Add(Jelly, 1);
            QuickSlots.Register(0, Jelly);

            Inventory.TryConsume(Jelly);

            Assert.IsNull(QuickSlots.GetItemId(0), "수량 0인 아이템의 퀵슬롯은 비어야 한다 (문서 §5.3).");
        }

        [Test]
        public void 해제하면_빈_칸이_된다()
        {
            Inventory.Add(Ice, 1);
            QuickSlots.Register(1, Ice);
            QuickSlots.Clear(1);

            Assert.IsTrue(QuickSlots.IsEmpty(1));
        }

        [Test]
        public void 범위_밖_인덱스는_무시한다()
        {
            Inventory.Add(Jelly, 1);
            QuickSlots.Register(-1, Jelly);
            QuickSlots.Register(QuickSlots.SlotCount, Jelly);

            Assert.AreEqual(-1, QuickSlots.IndexOf(Jelly));
            Assert.IsNull(QuickSlots.GetItemId(QuickSlots.SlotCount));
        }

        [Test]
        public void 슬롯_키_라벨은_1부터_시작하고_열번째는_0이다()
        {
            Assert.AreEqual("1", QuickSlotView.KeyLabel(0));
            Assert.AreEqual("3", QuickSlotView.KeyLabel(2));
            Assert.AreEqual("0", QuickSlotView.KeyLabel(9));
        }

        [Test]
        public void 빈_칸이_된_등록을_한_번에_정리한다()
        {
            Inventory.Add(Jelly, 1);
            Inventory.Add(Ice, 1);
            QuickSlots.Register(0, Jelly);
            QuickSlots.Register(1, Ice);

            Inventory.TryConsume(Jelly);
            QuickSlots.PruneEmpty();

            Assert.IsTrue(QuickSlots.IsEmpty(0));
            Assert.AreEqual(Ice, QuickSlots.GetItemId(1));
        }
    }
}
