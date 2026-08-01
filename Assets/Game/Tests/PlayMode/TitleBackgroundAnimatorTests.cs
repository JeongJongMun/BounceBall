using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Game.Tests
{
    // 타이틀 배경 컷 애니메이션. 도입부는 실행 직후 자동 재생, 나머지는 시작 버튼 연출이다.
    public class TitleBackgroundAnimatorTests
    {
        private const int IntroLastFrame = 16;
        private const int StartTransitionFrame = 22; // 혀가 한창인 중간 프레임
        private const int FrameCount = 28; // Title_0000 ~ Title_0027
        private const float Fps = 100f;    // 테스트를 빨리 끝내기 위해 실제 값보다 크게 잡는다

        private GameObject _go;

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_go != null) Object.Destroy(_go);
            yield return null;
        }

        // Start에서 도입부가 자동 재생되므로, 데이터를 넣고 나서 활성화해야 한다.
        private TitleBackgroundAnimator Create(Sprite[] frames, out Image image)
        {
            _go = new GameObject("TitleBackground");
            _go.SetActive(false);
            image = _go.AddComponent<Image>();
            var anim = _go.AddComponent<TitleBackgroundAnimator>();
            anim.SetData(image, frames, IntroLastFrame, Fps, StartTransitionFrame);
            _go.SetActive(true);
            return anim;
        }

        private static Sprite[] CreateFrames(int count)
        {
            var texture = new Texture2D(1, 1);
            var frames = new Sprite[count];
            for (int i = 0; i < count; i++)
            {
                frames[i] = Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);
                frames[i].name = "Title_" + i.ToString("0000");
            }
            return frames;
        }

        [UnityTest]
        public IEnumerator 실행하면_도입부를_재생하고_마지막_프레임에_멈춘다()
        {
            var frames = CreateFrames(FrameCount);
            Image image;
            Create(frames, out image);

            yield return new WaitForSecondsRealtime(0.6f);

            Assert.AreSame(frames[IntroLastFrame], image.sprite,
                "도입부가 끝나면 " + IntroLastFrame + "번 프레임에서 멈춰 대기해야 합니다.");
        }

        [UnityTest]
        public IEnumerator 시작_연출은_중간_프레임에서_스테이지_선택으로_넘어간다()
        {
            var frames = CreateFrames(FrameCount);
            Image image;
            var anim = Create(frames, out image);
            yield return new WaitForSecondsRealtime(0.6f);

            bool done = false;
            anim.PlayStart(() => done = true);

            Assert.AreSame(frames[IntroLastFrame + 1], image.sprite, "시작 연출은 17번 프레임부터 시작해야 합니다.");
            Assert.IsFalse(done, "중간 프레임에 도달하기 전에 게임이 시작되면 안 됩니다.");

            yield return new WaitForSecondsRealtime(0.6f);

            Assert.IsTrue(done, "중간 프레임에 도달했는데 완료가 전달되지 않았습니다.");
            Assert.AreSame(frames[StartTransitionFrame], image.sprite,
                "혀가 한창인 " + StartTransitionFrame + "번 프레임에서 넘어가야 합니다.");
        }

        // 배선이 빠져도 게임은 시작돼야 한다 — 여기서 콜백을 빠뜨리면 시작 버튼이 먹통이 된다.
        [UnityTest]
        public IEnumerator 프레임이_비어_있어도_완료를_알린다()
        {
            Image image;
            var anim = Create(new Sprite[0], out image);
            yield return null;

            bool done = false;
            anim.PlayStart(() => done = true);

            Assert.IsTrue(done, "재생할 프레임이 없으면 즉시 완료 처리해야 합니다.");
        }

        // 스테이지 선택에서 로비로 돌아오면 도입부를 처음부터 다시 보여준다.
        [UnityTest]
        public IEnumerator 로비로_돌아오면_도입부를_처음부터_다시_재생한다()
        {
            var frames = CreateFrames(FrameCount);
            Image image;
            var anim = Create(frames, out image);
            yield return new WaitForSecondsRealtime(0.6f);

            anim.PlayStart(null);
            yield return new WaitForSecondsRealtime(0.6f);
            Assert.AreSame(frames[StartTransitionFrame], image.sprite);

            // 스테이지 선택으로 넘어갔다가 되돌아오는 상황
            _go.SetActive(false);
            _go.SetActive(true);

            Assert.AreSame(frames[0], image.sprite, "돌아왔으면 도입부를 0번 프레임부터 다시 시작해야 합니다.");

            yield return new WaitForSecondsRealtime(0.6f);
            Assert.AreSame(frames[IntroLastFrame], image.sprite, "다시 재생한 도입부도 대기 프레임에서 멈춰야 합니다.");
        }
    }
}
