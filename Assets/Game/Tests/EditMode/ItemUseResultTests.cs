using NUnit.Framework;

namespace Game.Tests
{
    public class ItemUseResultTests
    {
        // 플레이어가 고칠 수 있는 실패는 이유를 알려 준다
        [Test]
        public void 안내가_필요한_실패는_문구가_있다()
        {
            Assert.IsNotEmpty(ItemUseService.MessageFor(ItemUseResult.NotInGame));
            Assert.IsNotEmpty(ItemUseService.MessageFor(ItemUseResult.NotUsable));
        }

        // 사망 연출·클리어 처리처럼 곧 지나가는 상태는 안내하면 성가시다
        [Test]
        public void 잠깐_지나가는_실패는_조용히_넘어간다()
        {
            Assert.IsEmpty(ItemUseService.MessageFor(ItemUseResult.PlayerBusy));
            Assert.IsEmpty(ItemUseService.MessageFor(ItemUseResult.StageCleared));
            Assert.IsEmpty(ItemUseService.MessageFor(ItemUseResult.Failed));
        }

        [Test]
        public void 성공은_실패_문구를_쓰지_않는다()
        {
            // 성공 안내는 아이템 이름이 들어가야 해서 Report가 따로 만든다
            Assert.IsEmpty(ItemUseService.MessageFor(ItemUseResult.Success));
        }

        [Test]
        public void 실패_사유마다_서로_다른_문구를_쓴다()
        {
            var notInGame = ItemUseService.MessageFor(ItemUseResult.NotInGame);
            var notUsable = ItemUseService.MessageFor(ItemUseResult.NotUsable);

            Assert.AreNotEqual(notInGame, notUsable);
        }

        [Test]
        public void 빈_아이템은_조용히_실패한다()
        {
            // 빈 퀵슬롯을 눌렀을 때 안내가 뜨면 성가시다
            Assert.AreEqual(ItemUseResult.Failed, ItemUseService.TryUse(null));
            Assert.AreEqual(ItemUseResult.Failed, ItemUseService.TryUse(""));
        }
    }
}
