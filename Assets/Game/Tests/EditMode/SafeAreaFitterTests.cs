using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    // 실기기 노치를 EditMode에서 재현할 수 없으므로, 픽셀 → 앵커 변환 규칙만 고정한다.
    public class SafeAreaFitterTests
    {
        [Test]
        public void 노치가_없으면_화면_전체가_안전_영역이다()
        {
            bool ok = SafeAreaFitter.TryComputeAnchors(new Rect(0f, 0f, 1920f, 1080f), 1920, 1080,
                out var min, out var max);

            Assert.IsTrue(ok);
            Assert.AreEqual(Vector2.zero, min);
            Assert.AreEqual(Vector2.one, max);
        }

        // 가로 모드에서 노치는 한쪽 옆에 온다 (홈 인디케이터는 아래)
        [Test]
        public void 좌측_노치는_최소_앵커를_밀어낸다()
        {
            bool ok = SafeAreaFitter.TryComputeAnchors(new Rect(96f, 0f, 1824f, 1080f), 1920, 1080,
                out var min, out var max);

            Assert.IsTrue(ok);
            Assert.AreEqual(0.05f, min.x, 0.0001f);
            Assert.AreEqual(0f, min.y, 0.0001f);
            Assert.AreEqual(1f, max.x, 0.0001f);
        }

        [Test]
        public void 아래_홈_인디케이터는_하단_앵커를_올린다()
        {
            bool ok = SafeAreaFitter.TryComputeAnchors(new Rect(0f, 54f, 1920f, 1026f), 1920, 1080,
                out var min, out var max);

            Assert.IsTrue(ok);
            Assert.AreEqual(0.05f, min.y, 0.0001f);
            Assert.AreEqual(1f, max.y, 0.0001f);
        }

        // 0으로 나누면 UI가 한 점으로 찌그러진 채 복구되지 않는다
        [Test]
        public void 화면_크기가_0이면_앵커를_바꾸지_않는다()
        {
            Assert.IsFalse(SafeAreaFitter.TryComputeAnchors(new Rect(0f, 0f, 100f, 100f), 0, 1080, out _, out _));
            Assert.IsFalse(SafeAreaFitter.TryComputeAnchors(new Rect(0f, 0f, 100f, 100f), 1920, 0, out _, out _));
        }

        [Test]
        public void 안전_영역이_비어_있으면_앵커를_바꾸지_않는다()
        {
            Assert.IsFalse(SafeAreaFitter.TryComputeAnchors(new Rect(0f, 0f, 0f, 1080f), 1920, 1080, out _, out _));
            Assert.IsFalse(SafeAreaFitter.TryComputeAnchors(new Rect(0f, 0f, 1920f, 0f), 1920, 1080, out _, out _));
        }

        [Test]
        public void 실패하면_전체_화면_앵커를_돌려준다()
        {
            SafeAreaFitter.TryComputeAnchors(new Rect(0f, 0f, 0f, 0f), 0, 0, out var min, out var max);

            Assert.AreEqual(Vector2.zero, min);
            Assert.AreEqual(Vector2.one, max);
        }
    }
}
