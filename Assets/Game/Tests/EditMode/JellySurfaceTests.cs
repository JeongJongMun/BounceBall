using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    // 젤리 표면 기하 규칙 (기획 §5.3~§5.5)
    public class JellySurfaceTests
    {
        [Test]
        public void 접촉_법선으로_부착_방향을_정한다()
        {
            Assert.AreEqual(JellyAttachDirection.Floor, JellySurface.FromNormal(Vector2.up));
            Assert.AreEqual(JellyAttachDirection.Ceiling, JellySurface.FromNormal(Vector2.down));
            // 법선이 오른쪽 = 타일이 플레이어 왼쪽 → 왼쪽 벽 부착 (기획 §5.3)
            Assert.AreEqual(JellyAttachDirection.LeftWall, JellySurface.FromNormal(Vector2.right));
            Assert.AreEqual(JellyAttachDirection.RightWall, JellySurface.FromNormal(Vector2.left));
        }

        [Test]
        public void 비스듬한_법선은_가장_가까운_축으로_판정한다()
        {
            Assert.AreEqual(JellyAttachDirection.Floor, JellySurface.FromNormal(new Vector2(0.3f, 0.95f)));
            Assert.AreEqual(JellyAttachDirection.LeftWall, JellySurface.FromNormal(new Vector2(0.95f, 0.3f)));
        }

        [Test]
        public void 영벡터는_방향이_없다()
        {
            Assert.AreEqual(JellyAttachDirection.None, JellySurface.FromNormal(Vector2.zero));
        }

        [Test]
        public void 바닥과_천장은_화면_기준_좌우를_유지한다()
        {
            // 기획 §5.4: 천장에서도 A는 왼쪽, D는 오른쪽
            Assert.AreEqual(new Vector2(-1f, 0f), JellySurface.MoveDirection(JellyAttachDirection.Floor, -1f));
            Assert.AreEqual(new Vector2(1f, 0f), JellySurface.MoveDirection(JellyAttachDirection.Floor, 1f));
            Assert.AreEqual(new Vector2(-1f, 0f), JellySurface.MoveDirection(JellyAttachDirection.Ceiling, -1f));
            Assert.AreEqual(new Vector2(1f, 0f), JellySurface.MoveDirection(JellyAttachDirection.Ceiling, 1f));
        }

        // enum 이름은 "벽이 플레이어의 어느 쪽에 있는가"다. RightWall = 벽이 오른쪽 = 플레이어는 벽의 좌측면.
        [Test]
        public void 벽_좌측면에서는_A가_아래_D가_위다()
        {
            Assert.AreEqual(new Vector2(0f, -1f), JellySurface.MoveDirection(JellyAttachDirection.RightWall, -1f));
            Assert.AreEqual(new Vector2(0f, 1f), JellySurface.MoveDirection(JellyAttachDirection.RightWall, 1f));
        }

        [Test]
        public void 벽_우측면에서는_A가_위_D가_아래다()
        {
            Assert.AreEqual(new Vector2(0f, 1f), JellySurface.MoveDirection(JellyAttachDirection.LeftWall, -1f));
            Assert.AreEqual(new Vector2(0f, -1f), JellySurface.MoveDirection(JellyAttachDirection.LeftWall, 1f));
        }

        // 바닥에서 모서리를 돌아 벽으로 올라탈 때, 같은 키를 누른 채로 진행 방향이 이어져야 한다.
        // 벽 두 면이 같은 식을 쓰면 왼쪽으로 기어가다 모서리를 도는 순간 역주행한다.
        [TestCase(1f, TestName = "D로 오른쪽 벽을 타고 올라간다")]
        [TestCase(-1f, TestName = "A로 왼쪽 벽을 타고 올라간다")]
        public void 바닥에서_벽으로_돌아도_계속_올라간다(float input)
        {
            var move = JellySurface.MoveDirection(JellyAttachDirection.Floor, input);
            JellySurface.TurnConcaveCorner(Vector2.up, move, out var newNormal, out _);
            var wall = JellySurface.FromNormal(newNormal);

            var next = JellySurface.MoveDirection(wall, input);
            Assert.AreEqual(new Vector2(0f, 1f), next,
                "모서리를 돈 뒤 같은 키로 계속 올라가야 하는데 " + wall + "에서 " + next + "로 갔습니다.");
        }

        [Test]
        public void 오목_모서리는_막은_면으로_올라탄다()
        {
            // 바닥을 오른쪽으로 가다 벽에 막힘 → 그 벽(플레이어 오른쪽)에 붙고 위로 진행
            JellySurface.TurnConcaveCorner(Vector2.up, Vector2.right, out var normal, out var moveDir);

            Assert.AreEqual(Vector2.left, normal, "막은 벽의 법선은 왼쪽을 향해야 합니다.");
            Assert.AreEqual(JellyAttachDirection.RightWall, JellySurface.FromNormal(normal));
            Assert.AreEqual(Vector2.up, moveDir);
        }

        [Test]
        public void 오목_모서리를_돌면_ㄷ자_안쪽을_따라간다()
        {
            // 기획 §5.5의 예시: 젤리 바닥 → 젤리 벽 → 젤리 천장
            var normal = Vector2.up;
            var moveDir = Vector2.right;

            JellySurface.TurnConcaveCorner(normal, moveDir, out normal, out moveDir);
            Assert.AreEqual(JellyAttachDirection.RightWall, JellySurface.FromNormal(normal));

            JellySurface.TurnConcaveCorner(normal, moveDir, out normal, out moveDir);
            Assert.AreEqual(JellyAttachDirection.Ceiling, JellySurface.FromNormal(normal));
            Assert.AreEqual(Vector2.left, moveDir, "천장에서는 왼쪽으로 되돌아가야 합니다.");
        }
    }
}
