using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    public class ShopTests
    {
        private ItemData _item;

        [SetUp]
        public void SetUp()
        {
            CurrencyWallet.ResetAll();
            Inventory.ResetAll();

            _item = ScriptableObject.CreateInstance<ItemData>();
            _item.SetData("Property_Jelly", "젤리 성질 아이템", "", ItemCategory.PropertyConsumable, 40, null, null);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_item);
            CurrencyWallet.ResetAll();
            Inventory.ResetAll();
        }

        [Test]
        public void 총액은_개당_가격_곱하기_수량이다()
        {
            Assert.AreEqual(120, Shop.TotalPrice(_item, 3));
        }

        [Test]
        public void 구매하면_코인이_차감되고_인벤토리에_추가된다()
        {
            CurrencyWallet.Add(100);

            Assert.IsTrue(Shop.TryPurchase(_item, 2));
            Assert.AreEqual(20, CurrencyWallet.Coin);
            Assert.AreEqual(2, Inventory.GetCount("Property_Jelly"));
        }

        [Test]
        public void 코인이_부족하면_구매되지_않는다()
        {
            CurrencyWallet.Add(30);

            Assert.IsFalse(Shop.TryPurchase(_item, 1));
            Assert.AreEqual(30, CurrencyWallet.Coin, "실패 시 코인을 차감하면 안 된다.");
            Assert.AreEqual(0, Inventory.GetCount("Property_Jelly"), "실패 시 아이템이 들어가면 안 된다.");
        }

        [Test]
        public void 잔액이_정확히_같으면_구매된다()
        {
            CurrencyWallet.Add(80);
            Assert.IsTrue(Shop.TryPurchase(_item, 2));
            Assert.AreEqual(0, CurrencyWallet.Coin);
        }

        [Test]
        public void 구매_가능_여부를_판정한다()
        {
            CurrencyWallet.Add(80);
            Assert.IsTrue(Shop.CanAfford(_item, 2));
            Assert.IsFalse(Shop.CanAfford(_item, 3));
        }

        [Test]
        public void 수량이_0이하면_구매되지_않는다()
        {
            CurrencyWallet.Add(100);
            Assert.IsFalse(Shop.TryPurchase(_item, 0));
            Assert.AreEqual(100, CurrencyWallet.Coin);
        }

        [Test]
        public void 여러_번_구매하면_스택이_쌓인다()
        {
            CurrencyWallet.Add(200);
            Shop.TryPurchase(_item, 1);
            Shop.TryPurchase(_item, 2);
            Assert.AreEqual(3, Inventory.GetCount("Property_Jelly"));
        }
    }
}
