using NUnit.Framework;

namespace Game.Tests
{
    public class InventoryTests
    {
        private const string Jelly = "Property_Jelly";
        private const string Ice = "Property_Ice";

        [SetUp]
        [TearDown]
        public void Reset() => Inventory.ResetAll();

        [Test]
        public void 초기_인벤토리는_비어_있다()
        {
            Assert.AreEqual(0, Inventory.EntryCount);
            Assert.AreEqual(0, Inventory.GetCount(Jelly));
        }

        [Test]
        public void 같은_아이템은_스택으로_쌓인다()
        {
            Inventory.Add(Jelly, 2);
            Inventory.Add(Jelly, 3);

            Assert.AreEqual(5, Inventory.GetCount(Jelly));
            Assert.AreEqual(1, Inventory.EntryCount, "같은 아이템은 슬롯 하나에 쌓여야 한다.");
        }

        [Test]
        public void 다른_아이템은_슬롯이_나뉜다()
        {
            Inventory.Add(Jelly);
            Inventory.Add(Ice);
            Assert.AreEqual(2, Inventory.EntryCount);
        }

        [Test]
        public void 사용하면_수량이_1_줄어든다()
        {
            Inventory.Add(Jelly, 3);
            Assert.IsTrue(Inventory.TryConsume(Jelly));
            Assert.AreEqual(2, Inventory.GetCount(Jelly));
        }

        [Test]
        public void 수량이_0이_되면_슬롯에서_제거된다()
        {
            Inventory.Add(Jelly);
            Inventory.TryConsume(Jelly);

            Assert.AreEqual(0, Inventory.GetCount(Jelly));
            Assert.AreEqual(0, Inventory.EntryCount, "수량 0인 아이템은 슬롯에서 빠져야 한다.");
        }

        [Test]
        public void 수량이_부족하면_차감하지_않는다()
        {
            Inventory.Add(Jelly, 1);
            Assert.IsFalse(Inventory.TryConsume(Jelly, 2));
            Assert.AreEqual(1, Inventory.GetCount(Jelly), "사용 실패 시 수량이 줄면 안 된다.");
        }

        [Test]
        public void 보유하지_않은_아이템은_사용할_수_없다()
        {
            Assert.IsFalse(Inventory.TryConsume(Jelly));
        }

        [Test]
        public void 변경_이벤트가_발생한다()
        {
            int calls = 0;
            void Handler() => calls++;

            Inventory.OnChanged += Handler;
            Inventory.Add(Jelly);
            Inventory.TryConsume(Jelly);
            Inventory.OnChanged -= Handler;

            Assert.AreEqual(2, calls);
        }
    }
}
