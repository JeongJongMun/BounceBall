using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    public class ShopConsumableTests
    {
        // ── 대시 방향 결정 (문서 §4.2) ──

        [Test]
        public void 입력이_있으면_입력_방향으로_대시한다()
        {
            Assert.AreEqual(-1f, PlayerDash.ResolveDirection(-1f, 1f));
            Assert.AreEqual(1f, PlayerDash.ResolveDirection(1f, -1f));
        }

        [Test]
        public void 입력이_없으면_바라보는_방향으로_대시한다()
        {
            Assert.AreEqual(1f, PlayerDash.ResolveDirection(0f, 1f));
            Assert.AreEqual(-1f, PlayerDash.ResolveDirection(0f, -1f));
        }

        [Test]
        public void 동시_입력은_합이_0이라_바라보는_방향을_쓴다()
        {
            // PlayerMovement가 A+D 동시 입력을 0으로 만든다 (기획 §8.2)
            Assert.AreEqual(-1f, PlayerDash.ResolveDirection(0f, -1f));
        }

        // ── 대시 벽 충돌 판정 (문서 §4.8) ──

        [Test]
        public void 진행_방향을_막는_벽에서만_대시가_끝난다()
        {
            // 오른쪽 대시: 왼쪽을 향한 법선(벽의 왼면)이 막는다
            Assert.IsTrue(PlayerDash.IsBlockingWall(Vector2.left, 1f));
            Assert.IsFalse(PlayerDash.IsBlockingWall(Vector2.right, 1f)); // 등 뒤
            Assert.IsFalse(PlayerDash.IsBlockingWall(Vector2.up, 1f));    // 바닥
            Assert.IsFalse(PlayerDash.IsBlockingWall(Vector2.down, 1f));  // 천장
        }

        [Test]
        public void 비스듬한_바닥은_벽으로_치지_않는다()
        {
            // 법선 수평 성분이 절반 이하면 경사면·모서리 — 대시를 유지한다
            var slope = new Vector2(-0.3f, 0.95f).normalized;
            Assert.IsFalse(PlayerDash.IsBlockingWall(slope, 1f));
        }

        // ── 실드 밀어내기 (문서 §5.7) ──

        [Test]
        public void 아래_발판을_방어하면_위로만_밀린다()
        {
            // 바닥 사망 발판: 법선이 위 → 수평 성분 없음
            var velocity = PlayerShield.ComputeKnockback(Vector2.up, 6f, 9f);
            Assert.AreEqual(0f, velocity.x, 0.001f);
            Assert.AreEqual(9f, velocity.y, 0.001f);
        }

        [Test]
        public void 옆면_발판을_방어하면_접촉면_반대로_밀린다()
        {
            // 오른쪽 벽 발판(법선이 왼쪽) → 왼쪽으로 밀림
            var velocity = PlayerShield.ComputeKnockback(Vector2.left, 6f, 9f);
            Assert.AreEqual(-6f, velocity.x, 0.001f);
            Assert.AreEqual(9f, velocity.y, 0.001f);
        }

        // ── 실드 외형 크기 ──

        [Test]
        public void 실드_외형은_원본_크기와_무관하게_지정_지름이_된다()
        {
            // 실드 원본이 23유닛(2302px)이라 그대로 쓰면 화면을 덮는다.
            // 큰 쪽 변 기준으로 배율을 구해 지정 지름에 맞춘다
            var sprite = Sprite.Create(Texture2D.whiteTexture,
                new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 2f); // 2x2 유닛
            try
            {
                Assert.AreEqual(0.7f, PlayerShield.ComputeVisualScale(sprite, 1.4f), 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(sprite);
            }
        }

        [Test]
        public void 실드_외형_배율은_잘못된_입력에서_1을_돌려준다()
        {
            Assert.AreEqual(1f, PlayerShield.ComputeVisualScale(null, 1.4f), 0.001f);
        }

        [Test]
        public void 천장_발판을_방어해도_위로_반발한다()
        {
            // 문서 §5.7: 수직 초기화 후 위로 반발 — 아래로 밀지 않는다.
            // 무시 시간 동안 발판에서 떨어져 나오는 것이 목적이다
            var velocity = PlayerShield.ComputeKnockback(Vector2.down, 6f, 9f);
            Assert.AreEqual(9f, velocity.y, 0.001f);
        }
    }
}
