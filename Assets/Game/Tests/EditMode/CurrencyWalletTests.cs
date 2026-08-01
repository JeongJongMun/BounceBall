using NUnit.Framework;

namespace Game.Tests
{
    public class CurrencyWalletTests
    {
        [SetUp]
        [TearDown]
        public void Reset() => CurrencyWallet.ResetAll();

        [Test]
        public void 초기_보유_코인은_0이다()
        {
            Assert.AreEqual(0, CurrencyWallet.Coin);
        }

        [Test]
        public void 코인을_획득하면_누적된다()
        {
            CurrencyWallet.Add(30);
            CurrencyWallet.Add(20);
            Assert.AreEqual(50, CurrencyWallet.Coin);
        }

        [Test]
        public void 잔액이_충분하면_차감된다()
        {
            CurrencyWallet.Add(100);
            Assert.IsTrue(CurrencyWallet.TrySpend(40));
            Assert.AreEqual(60, CurrencyWallet.Coin);
        }

        [Test]
        public void 잔액이_부족하면_차감하지_않는다()
        {
            CurrencyWallet.Add(30);
            Assert.IsFalse(CurrencyWallet.TrySpend(50));
            Assert.AreEqual(30, CurrencyWallet.Coin, "구매 실패 시 코인이 차감되면 안 된다.");
        }

        [Test]
        public void 체크포인트_복구는_저장_시점_잔액으로_되돌린다()
        {
            CurrencyWallet.Add(100);
            int saved = CurrencyWallet.Coin;
            CurrencyWallet.Add(70); // 체크포인트 이후 획득분

            CurrencyWallet.RestoreTo(saved);
            Assert.AreEqual(100, CurrencyWallet.Coin);
        }

        [Test]
        public void 코인_변경_이벤트가_발생한다()
        {
            int received = -1;
            void Handler(int value) => received = value;

            CurrencyWallet.OnCoinChanged += Handler;
            CurrencyWallet.Add(25);
            CurrencyWallet.OnCoinChanged -= Handler;

            Assert.AreEqual(25, received);
        }
    }
}
