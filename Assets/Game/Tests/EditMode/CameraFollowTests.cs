using NUnit.Framework;

namespace Game.Tests
{
    public class CameraFollowTests
    {
        // ComputeAxisTarget: 데드존 반경 2, 카메라 0 기준
        [Test]
        public void 데드존_안에서는_카메라가_움직이지_않는다()
        {
            Assert.AreEqual(0f, CameraFollow.ComputeAxisTarget(0f, 1.5f, 2f));
            Assert.AreEqual(0f, CameraFollow.ComputeAxisTarget(0f, -1.9f, 2f));
        }

        [Test]
        public void 데드존을_넘으면_넘은_만큼만_추적한다()
        {
            // 플레이어 5, 데드존 2 → 카메라 목표 3 (플레이어가 데드존 오른쪽 경계에 오도록)
            Assert.AreEqual(3f, CameraFollow.ComputeAxisTarget(0f, 5f, 2f));
            Assert.AreEqual(-3f, CameraFollow.ComputeAxisTarget(0f, -5f, 2f));
        }

        [Test]
        public void 데드존으로_돌아와도_카메라는_되돌아가지_않는다()
        {
            // 카메라 3에서 플레이어가 2로 복귀 → 데드존 안이므로 3 유지
            Assert.AreEqual(3f, CameraFollow.ComputeAxisTarget(3f, 2f, 2f));
        }

        // ClampAxis: 경계 -10~10, 카메라 반폭 8
        [Test]
        public void 경계_안쪽으로_클램프된다()
        {
            Assert.AreEqual(2f, CameraFollow.ClampAxis(5f, -10f, 10f, 8f));
            Assert.AreEqual(-2f, CameraFollow.ClampAxis(-5f, -10f, 10f, 8f));
            Assert.AreEqual(0f, CameraFollow.ClampAxis(0f, -10f, 10f, 8f));
        }

        [Test]
        public void 스테이지가_화면보다_작으면_중앙_고정()
        {
            // 경계 폭 10 < 화면 폭 16 → 중앙 0
            Assert.AreEqual(0f, CameraFollow.ClampAxis(4f, -5f, 5f, 8f));
            // 비대칭 경계도 중앙
            Assert.AreEqual(5f, CameraFollow.ClampAxis(9f, 0f, 10f, 8f));
        }
    }
}
