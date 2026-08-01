using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    public class StageIntroCameraTests
    {
        // ComputeFitSize(width, height, aspect, padding, maxSize)
        // 화면 비율 16:9(약 1.778) 기준. 여백 배율 1이면 맵에 딱 맞는다.
        private const float Aspect = 16f / 9f;

        [Test]
        public void 가로가_긴_맵은_가로가_기준이_된다()
        {
            // 40 x 10 맵: 세로 기준 5, 가로 기준 20/1.778 = 11.25 → 큰 쪽인 11.25
            float size = StageIntroCamera.ComputeFitSize(40f, 10f, Aspect, 1f, 0f);
            Assert.AreEqual(11.25f, size, 0.001f);
        }

        [Test]
        public void 세로가_긴_맵은_세로가_기준이_된다()
        {
            // 10 x 40 맵: 세로 기준 20, 가로 기준 5/1.778 = 2.8125 → 큰 쪽인 20
            float size = StageIntroCamera.ComputeFitSize(10f, 40f, Aspect, 1f, 0f);
            Assert.AreEqual(20f, size, 0.001f);
        }

        [Test]
        public void 여백_배율이_크기에_비례해_반영된다()
        {
            float tight = StageIntroCamera.ComputeFitSize(10f, 40f, Aspect, 1f, 0f);
            float padded = StageIntroCamera.ComputeFitSize(10f, 40f, Aspect, 1.08f, 0f);

            Assert.AreEqual(tight * 1.08f, padded, 0.001f);
        }

        [Test]
        public void 화면이_좁아지면_가로_기준_크기가_커진다()
        {
            // 같은 가로로 긴 맵이라도 세로 화면(9:16)에서는 훨씬 더 축소해야 다 들어온다
            float wide = StageIntroCamera.ComputeFitSize(40f, 10f, Aspect, 1f, 0f);
            float narrow = StageIntroCamera.ComputeFitSize(40f, 10f, 9f / 16f, 1f, 0f);

            Assert.Greater(narrow, wide);
            Assert.AreEqual(20f / (9f / 16f), narrow, 0.001f);
        }

        [Test]
        public void 최대_축소_한계가_적용된다()
        {
            // 한계를 넘으면 잘리고, 한계 안이면 그대로 둔다
            Assert.AreEqual(12f, StageIntroCamera.ComputeFitSize(10f, 40f, Aspect, 1f, 12f), 0.001f);
            Assert.AreEqual(20f, StageIntroCamera.ComputeFitSize(10f, 40f, Aspect, 1f, 30f), 0.001f);
        }

        [Test]
        public void 한계가_0이면_제한하지_않는다()
        {
            Assert.AreEqual(20f, StageIntroCamera.ComputeFitSize(10f, 40f, Aspect, 1f, 0f), 0.001f);
        }

        // ComputeIntroBounds(stageBounds, hasTiles, tiles) — 경계 ∩ 타일
        private static Bounds Rect(float minX, float maxX, float minY, float maxY) =>
            new Bounds(new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0f),
                       new Vector3(maxX - minX, maxY - minY, 0f));

        [Test]
        public void 타일이_경계_안에_있으면_타일_범위를_쓴다()
        {
            // 정상 스테이지: 경계는 타일 끝에서 여백만큼 더 넓다
            var result = StageIntroCamera.ComputeIntroBounds(
                Rect(-13f, 13f, -6f, 6f), true, Rect(-10f, 10f, -3f, 3f));

            Assert.AreEqual(20f, result.size.x, 0.001f);
            Assert.AreEqual(6f, result.size.y, 0.001f);
        }

        [Test]
        public void 타일이_없으면_경계를_쓴다()
        {
            var stage = Rect(-13f, 13f, -6f, 6f);
            var result = StageIntroCamera.ComputeIntroBounds(stage, false, default);

            Assert.AreEqual(26f, result.size.x, 0.001f);
            Assert.AreEqual(12f, result.size.y, 0.001f);
        }

        [Test]
        public void 경계가_타일보다_좁으면_경계까지만_담는다()
        {
            // 경계를 아직 계산하지 않은 스테이지. 플레이 가능한 영역만 보여준다
            var result = StageIntroCamera.ComputeIntroBounds(
                Rect(-10f, 10f, -6f, 6f), true, Rect(-16f, 16f, -3f, 3f));

            Assert.AreEqual(20f, result.size.x, 0.001f);
            Assert.AreEqual(6f, result.size.y, 0.001f);
        }

        [Test]
        public void 축마다_좁은_쪽이_따로_적용된다()
        {
            // 가로는 경계가, 세로는 타일이 좁은 경우
            var result = StageIntroCamera.ComputeIntroBounds(
                Rect(-8f, 8f, -6f, 6f), true, Rect(-16f, 16f, -2f, 2f));

            Assert.AreEqual(16f, result.size.x, 0.001f);
            Assert.AreEqual(4f, result.size.y, 0.001f);
        }

        [Test]
        public void 겹치는_곳이_없으면_경계로_되돌린다()
        {
            // 좌표가 어긋난 스테이지 — 빈 영역을 비추지 않도록 경계를 쓴다
            var result = StageIntroCamera.ComputeIntroBounds(
                Rect(-10f, 10f, -6f, 6f), true, Rect(50f, 70f, -3f, 3f));

            Assert.AreEqual(20f, result.size.x, 0.001f);
            Assert.AreEqual(12f, result.size.y, 0.001f);
            Assert.AreEqual(0f, result.center.x, 0.001f);
        }

        // ShouldPlayIntro(tileWidth, tileHeight, aspect, playSize, minZoomRatio)
        // 카메라 크기 5 = 화면 세로 10칸, 가로 약 17.8칸. 최소 확대 비율 1.2.
        private const float PlaySize = 5f;
        private const float MinRatio = 1.2f;

        [Test]
        public void 타일이_화면_안에_다_들어오면_연출하지_않는다()
        {
            Assert.IsFalse(StageIntroCamera.ShouldPlayIntro(16f, 8f, Aspect, PlaySize, MinRatio));
        }

        [Test]
        public void 타일이_화면보다_충분히_크면_연출한다()
        {
            // 세로 30칸 → 필요 크기 15, 기준 5 * 1.2 = 6 → 재생
            Assert.IsTrue(StageIntroCamera.ShouldPlayIntro(20f, 30f, Aspect, PlaySize, MinRatio));
        }

        [Test]
        public void 아슬아슬하게_큰_맵은_최소_비율에_걸려_건너뛴다()
        {
            // 세로 11칸 → 필요 크기 5.5. 화면(5)보다 크지만 기준 6에는 못 미쳐 재생하지 않는다
            Assert.IsFalse(StageIntroCamera.ShouldPlayIntro(18f, 11f, Aspect, PlaySize, MinRatio));

            // 최소 비율을 1로 낮추면 조금이라도 크므로 재생한다
            Assert.IsTrue(StageIntroCamera.ShouldPlayIntro(18f, 11f, Aspect, PlaySize, 1f));
        }

        [Test]
        public void 타일이_없으면_연출하지_않는다()
        {
            Assert.IsFalse(StageIntroCamera.ShouldPlayIntro(0f, 0f, Aspect, PlaySize, MinRatio));
        }

        [Test]
        public void 세로로_긴_화면에서는_같은_맵도_연출될_수_있다()
        {
            // 가로 30칸 맵: 16:9에서는 필요 크기 8.44 → 기준 6 초과라 재생.
            // 9:16에서는 26.67 → 당연히 재생. 화면이 좁을수록 더 확실히 재생된다.
            Assert.IsTrue(StageIntroCamera.ShouldPlayIntro(30f, 8f, Aspect, PlaySize, MinRatio));
            Assert.IsTrue(StageIntroCamera.ShouldPlayIntro(30f, 8f, 9f / 16f, PlaySize, MinRatio));

            // 가로 22칸 맵: 넓은 화면에서는 6.19 → 재생하지만
            Assert.IsTrue(StageIntroCamera.ShouldPlayIntro(22f, 8f, Aspect, PlaySize, MinRatio));
            // 화면이 아주 넓으면(21:9) 4.71 → 다 보이므로 건너뛴다
            Assert.IsFalse(StageIntroCamera.ShouldPlayIntro(22f, 8f, 21f / 9f, PlaySize, MinRatio));
        }
    }
}
