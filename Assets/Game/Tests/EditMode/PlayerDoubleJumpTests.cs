using NUnit.Framework;

namespace Game.Tests
{
    public class PlayerDoubleJumpTests
    {
        // CanUse(lastUseTime, now, useInterval) — 중복 입력 방지 (문서 §3.8)
        [Test]
        public void 간격이_지나면_사용할_수_있다()
        {
            Assert.IsTrue(PlayerDoubleJump.CanUse(1.0f, 1.3f, 0.2f));
        }

        [Test]
        public void 간격_안에서는_사용할_수_없다()
        {
            // 키를 꾹 누르거나 연타해도 한 번만 소모돼야 한다
            Assert.IsFalse(PlayerDoubleJump.CanUse(1.0f, 1.1f, 0.2f));
        }

        [Test]
        public void 정확히_간격만큼_지나면_사용할_수_있다()
        {
            Assert.IsTrue(PlayerDoubleJump.CanUse(1.0f, 1.2f, 0.2f));
        }

        [Test]
        public void 처음_사용은_항상_가능하다()
        {
            // 마지막 사용 시각이 -∞ 이므로 어떤 시각이든 통과한다
            Assert.IsTrue(PlayerDoubleJump.CanUse(float.NegativeInfinity, 0f, 0.2f));
        }

        [Test]
        public void 간격이_0이면_연속_사용을_막지_않는다()
        {
            // 아이템이 충분하면 한 체공에 여러 번 점프를 허용한다 (문서 §3.8)
            Assert.IsTrue(PlayerDoubleJump.CanUse(1.0f, 1.0f, 0f));
        }
    }
}
