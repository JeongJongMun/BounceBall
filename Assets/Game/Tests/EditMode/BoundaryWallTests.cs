using NUnit.Framework;

namespace Game.Tests
{
    public class BoundaryWallTests
    {
        private const float MinX = -10f;
        private const float MaxX = 10f;

        [Test]
        public void 경계_기준이면_여백이_0일_때_경계선에_선다()
        {
            Assert.AreEqual(MinX, StageController.ComputeWallX(true, BoundaryWallMode.FromBounds, MinX, MaxX, 0f, 0f, 0f));
            Assert.AreEqual(MaxX, StageController.ComputeWallX(false, BoundaryWallMode.FromBounds, MinX, MaxX, 0f, 0f, 0f));
        }

        [Test]
        public void 여백을_주면_경계보다_바깥에_선다()
        {
            Assert.AreEqual(-12f, StageController.ComputeWallX(true, BoundaryWallMode.FromBounds, MinX, MaxX, 2f, 0f, 0f));
            Assert.AreEqual(12f, StageController.ComputeWallX(false, BoundaryWallMode.FromBounds, MinX, MaxX, 2f, 0f, 0f));
        }

        [Test]
        public void 음수_여백이면_경계_안쪽에서_막힌다()
        {
            Assert.AreEqual(-8f, StageController.ComputeWallX(true, BoundaryWallMode.FromBounds, MinX, MaxX, -2f, 0f, 0f));
            Assert.AreEqual(8f, StageController.ComputeWallX(false, BoundaryWallMode.FromBounds, MinX, MaxX, -2f, 0f, 0f));
        }

        [Test]
        public void 직접_지정이면_입력한_좌표를_그대로_쓴다()
        {
            Assert.AreEqual(-3f, StageController.ComputeWallX(true, BoundaryWallMode.Explicit, MinX, MaxX, 5f, -3f, 7f));
            Assert.AreEqual(7f, StageController.ComputeWallX(false, BoundaryWallMode.Explicit, MinX, MaxX, 5f, -3f, 7f));
        }
    }
}
