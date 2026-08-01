using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    public class ParallaxTests
    {
        // Wrap(value, center, width) — 카메라(center) 기준 ±width/2 안으로 되감는다
        [Test]
        public void 범위_안이면_그대로_둔다()
        {
            Assert.AreEqual(3f, ParallaxLayer.Wrap(3f, 0f, 40f), 0.001f);
            Assert.AreEqual(-19f, ParallaxLayer.Wrap(-19f, 0f, 40f), 0.001f);
        }

        [Test]
        public void 오른쪽으로_벗어나면_왼쪽으로_옮긴다()
        {
            // 25는 +20을 넘었으므로 25 - 40 = -15
            Assert.AreEqual(-15f, ParallaxLayer.Wrap(25f, 0f, 40f), 0.001f);
        }

        [Test]
        public void 왼쪽으로_벗어나면_오른쪽으로_옮긴다()
        {
            Assert.AreEqual(15f, ParallaxLayer.Wrap(-25f, 0f, 40f), 0.001f);
        }

        [Test]
        public void 여러_주기_벗어나도_한_번에_되감는다()
        {
            // 카메라가 순간이동해도 프롭이 한 번에 제자리로 온다 (부활·인트로 대비)
            Assert.AreEqual(5f, ParallaxLayer.Wrap(125f, 0f, 40f), 0.001f);
            Assert.AreEqual(-5f, ParallaxLayer.Wrap(-125f, 0f, 40f), 0.001f);
        }

        [Test]
        public void 카메라가_움직이면_그_주변으로_되감는다()
        {
            // 카메라 100 기준 ±20 → 0에 있던 프롭은 100 근처로 온다
            float wrapped = ParallaxLayer.Wrap(0f, 100f, 40f);
            Assert.GreaterOrEqual(wrapped, 80f);
            Assert.Less(wrapped, 120f);
        }

        [Test]
        public void 반복폭이_0이면_되감지_않는다()
        {
            Assert.AreEqual(123f, ParallaxLayer.Wrap(123f, 0f, 0f), 0.001f);
        }

        // PropX — 반복 폭을 개수만큼 나눠 균등 배치
        [Test]
        public void 프롭은_반복폭_안에_균등하게_퍼진다()
        {
            // 지터 0.5(중앙)면 슬롯 한가운데
            Assert.AreEqual(-15f, ParallaxBackground.PropX(0, 4, 40f, 0.5f), 0.001f);
            Assert.AreEqual(-5f, ParallaxBackground.PropX(1, 4, 40f, 0.5f), 0.001f);
            Assert.AreEqual(15f, ParallaxBackground.PropX(3, 4, 40f, 0.5f), 0.001f);
        }

        [Test]
        public void 지터를_줘도_반복폭을_벗어나지_않는다()
        {
            // 슬롯 밖으로 나가면 프롭이 뭉치거나 되감기 주기와 어긋난다.
            // 양 끝(±width/2)은 되감기가 주기적이라 어느 쪽에 놓여도 같은 그림이다.
            for (int i = 0; i < 8; i++)
            {
                Assert.GreaterOrEqual(ParallaxBackground.PropX(i, 8, 40f, 0f), -20f);
                Assert.LessOrEqual(ParallaxBackground.PropX(i, 8, 40f, 1f), 20f);
            }
        }

        [Test]
        public void 프롭_간격은_반복폭을_개수로_나눈_값이다()
        {
            // 되감김 지점에서도 간격이 유지되려면 슬롯이 균등해야 한다
            float first = ParallaxBackground.PropX(0, 8, 40f, 0.5f);
            float second = ParallaxBackground.PropX(1, 8, 40f, 0.5f);
            Assert.AreEqual(5f, second - first, 0.001f);
        }

        // ComputeZoomDrop — 인트로 줌아웃 때 화면 최하단이 내려간 만큼 배경도 따라 내려간다
        [Test]
        public void 줌아웃하면_커진_만큼_배경이_내려간다()
        {
            // 기준 5 → 인트로 6.84: 화면 최하단이 1.84 내려가므로 배경도 1.84 내린다
            Assert.AreEqual(1.84f, ParallaxLayer.ComputeZoomDrop(6.84f, 5f), 0.001f);
        }

        [Test]
        public void 줌인이나_기준_크기에서는_보정하지_않는다()
        {
            Assert.AreEqual(0f, ParallaxLayer.ComputeZoomDrop(5f, 5f), 0.001f);
            Assert.AreEqual(0f, ParallaxLayer.ComputeZoomDrop(4f, 5f), 0.001f); // 줌인 방향은 밑동이 화면 밖으로 내려갈 뿐
        }

        // ComputeReferenceCamera — 시차 기준점은 스테이지에서 계산한 고정 지점이어야 한다.
        // (첫 프레임의 카메라 위치를 쓰면 인트로 재생 여부에 따라 배경 높이가 달라진다)
        [Test]
        public void 기준_카메라는_최저_중심과_스테이지_중앙이다()
        {
            var stage = new UnityEngine.GameObject("stage").AddComponent<StageController>();
            try
            {
                // 경계 X -12~12, Y -2.87~14.9 (Stage02 형태), 카메라 크기 5
                stage.SetBounds(-12f, 12f, -2.87f, 14.9f, -5f);
                var so = new UnityEditor.SerializedObject(stage);
                so.FindProperty("cameraZoom").floatValue = 5f;
                so.ApplyModifiedPropertiesWithoutUndo();

                var reference = ParallaxLayer.ComputeReferenceCamera(stage, new Vector3(9f, 9f, -10f));

                Assert.AreEqual(0f, reference.x, 0.001f);      // 스테이지 중앙
                Assert.AreEqual(2.13f, reference.y, 0.001f);   // 최저 중심 = MinY + zoom
                Assert.AreEqual(-10f, reference.z, 0.001f);    // z는 카메라 값 유지
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(stage.gameObject);
            }
        }

        [Test]
        public void 맵이_화면보다_낮으면_기준은_세로_중앙_고정_지점이다()
        {
            var stage = new UnityEngine.GameObject("stage").AddComponent<StageController>();
            try
            {
                // 높이 9.13 < 화면 10 (Stage01 형태) → 카메라가 중앙에 고정된다
                stage.SetBounds(-10f, 10f, -4.35f, 4.78f, -7f);
                var so = new UnityEditor.SerializedObject(stage);
                so.FindProperty("cameraZoom").floatValue = 5f;
                so.ApplyModifiedPropertiesWithoutUndo();

                var reference = ParallaxLayer.ComputeReferenceCamera(stage, Vector3.zero);
                Assert.AreEqual(0.215f, reference.y, 0.001f);  // (min+max)/2
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(stage.gameObject);
            }
        }

        [Test]
        public void 스테이지가_없으면_카메라_위치를_그대로_쓴다()
        {
            var fallback = new Vector3(3f, 7f, -10f);
            Assert.AreEqual(fallback, ParallaxLayer.ComputeReferenceCamera(null, fallback));
        }

        // ComputeHorizon — 카메라가 보여줄 수 있는 가장 낮은 지점
        [Test]
        public void 맵이_화면보다_높으면_경계_아래쪽이_지평선이다()
        {
            // 경계 높이 11.9 > 화면 높이 10 → 카메라가 경계까지 내려갈 수 있다
            Assert.AreEqual(-6f, ParallaxBackground.ComputeHorizon(-6f, 5.9f, 5f, 0f), 0.001f);
        }

        [Test]
        public void 맵이_화면보다_낮으면_경계보다_더_아래가_지평선이다()
        {
            // 경계 높이 9.2 < 화면 높이 10 → 카메라가 세로 중앙(0.2)에 고정되어 -4.8까지 보인다.
            // 경계(-4.4)를 그대로 쓰면 나무가 0.4만큼 떠 보인다.
            Assert.AreEqual(-4.8f, ParallaxBackground.ComputeHorizon(-4.4f, 4.8f, 5f, 0f), 0.001f);
        }

        [Test]
        public void 지평선_오프셋이_더해진다()
        {
            Assert.AreEqual(-5.5f, ParallaxBackground.ComputeHorizon(-6f, 5.9f, 5f, 0.5f), 0.001f);
        }
    }
}
