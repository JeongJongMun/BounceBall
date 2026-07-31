using NUnit.Framework;

namespace Game.Tests
{
    // 기획 §3.1 상호작용 표 전수 검증
    public class PropertyInteractionTableTests
    {
        private static PropertyInteractionType Resolve(PlayerPropertyType player, TilePropertyType tile)
            => PropertyInteractionTable.Resolve(player, tile);

        [Test]
        public void 기본_성질의_타일별_반응()
        {
            Assert.AreEqual(PropertyInteractionType.NormalJump, Resolve(PlayerPropertyType.Default, TilePropertyType.Default));
            Assert.AreEqual(PropertyInteractionType.LowJump, Resolve(PlayerPropertyType.Default, TilePropertyType.Jelly));
            Assert.AreEqual(PropertyInteractionType.LowJump, Resolve(PlayerPropertyType.Default, TilePropertyType.Ice));
        }

        [Test]
        public void 젤리_성질의_타일별_반응()
        {
            Assert.AreEqual(PropertyInteractionType.HighJump, Resolve(PlayerPropertyType.Jelly, TilePropertyType.Default));
            Assert.AreEqual(PropertyInteractionType.Attach, Resolve(PlayerPropertyType.Jelly, TilePropertyType.Jelly));
            Assert.AreEqual(PropertyInteractionType.NormalJump, Resolve(PlayerPropertyType.Jelly, TilePropertyType.Ice));
        }

        [Test]
        public void 얼음_성질의_타일별_반응()
        {
            Assert.AreEqual(PropertyInteractionType.LowJump, Resolve(PlayerPropertyType.Ice, TilePropertyType.Default));
            Assert.AreEqual(PropertyInteractionType.NormalJump, Resolve(PlayerPropertyType.Ice, TilePropertyType.Jelly));
            Assert.AreEqual(PropertyInteractionType.Slide, Resolve(PlayerPropertyType.Ice, TilePropertyType.Ice));
        }

        [Test]
        public void 같은_결과인_조합은_같은_점프력을_쓴다()
        {
            // 기획 §4: 성질과 타일이 달라도 결과가 같으면 동일한 공용 점프값을 적용한다
            Assert.AreEqual(PropertyInteractionType.NormalJump, Resolve(PlayerPropertyType.Default, TilePropertyType.Default));
            Assert.AreEqual(PropertyInteractionType.NormalJump, Resolve(PlayerPropertyType.Jelly, TilePropertyType.Ice));
            Assert.AreEqual(PropertyInteractionType.NormalJump, Resolve(PlayerPropertyType.Ice, TilePropertyType.Jelly));

            Assert.AreEqual(PropertyInteractionType.LowJump, Resolve(PlayerPropertyType.Default, TilePropertyType.Jelly));
            Assert.AreEqual(PropertyInteractionType.LowJump, Resolve(PlayerPropertyType.Default, TilePropertyType.Ice));
            Assert.AreEqual(PropertyInteractionType.LowJump, Resolve(PlayerPropertyType.Ice, TilePropertyType.Default));
        }

        [Test]
        public void 부착과_미끄러짐은_각각_한_조합에서만_발생한다()
        {
            int attachCount = 0, slideCount = 0;
            foreach (PlayerPropertyType player in System.Enum.GetValues(typeof(PlayerPropertyType)))
            foreach (TilePropertyType tile in System.Enum.GetValues(typeof(TilePropertyType)))
            {
                var result = Resolve(player, tile);
                if (result == PropertyInteractionType.Attach) attachCount++;
                if (result == PropertyInteractionType.Slide) slideCount++;
            }

            Assert.AreEqual(1, attachCount, "Attach는 젤리×젤리에서만 발생해야 합니다.");
            Assert.AreEqual(1, slideCount, "Slide는 얼음×얼음에서만 발생해야 합니다.");
        }
    }
}
