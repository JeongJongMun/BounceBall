using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    // 타이틀 배경 컷 애니메이션. 프레임 스프라이트를 순서대로 갈아끼워 재생한다.
    //
    // 게임을 실행하면 도입부(0 ~ introLastFrame)를 한 번 재생하고 그 프레임에서 멈춰 대기하다가,
    // 시작 버튼을 누르면 나머지 구간을 이어 재생하고, 혀가 한창인 중간 프레임에서
    // 스테이지 선택으로 넘어가도록 콜백을 알린다.
    public class TitleBackgroundAnimator : MonoBehaviour
    {
        [SerializeField] private Image target;
        [Tooltip("Title_0000부터 번호 순서대로 넣는다")]
        [SerializeField] private Sprite[] frames;
        [Tooltip("도입부의 마지막 프레임 번호. 대기 중에는 이 프레임이 보이고, 시작 연출은 다음 프레임부터 이어진다")]
        [SerializeField] private int introLastFrame = 16;
        [Tooltip("시작 연출 중 이 프레임에 도달하면 스테이지 선택으로 넘어간다. 혀가 가장 나온 중간 지점")]
        [SerializeField] private int startTransitionFrame = 22;
        [SerializeField] private float framesPerSecond = 12f;

        private Coroutine _playing;

        private int LastIndex => frames == null ? -1 : frames.Length - 1;

        private void Awake() => ShowFrame(0);

        // 에디터 툴링/테스트에서 수치를 지정할 때 사용.
        public void SetData(Image image, Sprite[] frameSprites, int introLast, float fps, int startTransition = -1)
        {
            target = image;
            frames = frameSprites;
            introLastFrame = introLast;
            framesPerSecond = fps;
            if (startTransition >= 0) startTransitionFrame = startTransition;
        }

        private void OnEnable()
        {
            if (MenuNavigation.OpenStageSelectOnLoad) return;
            Play(0, introLastFrame, null);
        }

        // 캔버스가 꺼지면 코루틴도 함께 죽는다. 남은 핸들을 정리해 두지 않으면 다음 재생에서 잘못 멈춘다.
        private void OnDisable() => _playing = null;

        // 시작 버튼 연출. 혀가 한창인 중간 프레임까지 재생한 뒤 onComplete를 호출한다.
        // 프레임이 비어 있거나 재생할 구간이 없어도 onComplete는 반드시 호출한다 —
        // 여기서 빠뜨리면 버튼을 눌러도 게임이 시작되지 않는다.
        public void PlayStart(Action onComplete)
        {
            int from = introLastFrame + 1;
            int to = Mathf.Clamp(startTransitionFrame, from, LastIndex);
            Play(from, to, onComplete);
        }

        private void Play(int from, int to, Action onComplete)
        {
            if (_playing != null)
            {
                StopCoroutine(_playing);
                _playing = null;
            }

            int last = LastIndex;
            from = Mathf.Clamp(from, 0, last);
            to = Mathf.Clamp(to, 0, last);

            // 재생이 불가능한 상황에서는 결과 프레임만 세워 두고 즉시 완료로 처리한다.
            if (target == null || last < 0 || from > to || !isActiveAndEnabled)
            {
                ShowFrame(to);
                onComplete?.Invoke();
                return;
            }

            _playing = StartCoroutine(PlayRoutine(from, to, onComplete));
        }

        private IEnumerator PlayRoutine(int from, int to, Action onComplete)
        {
            // 메뉴 연출이라 timeScale에 좌우되지 않게 실제 시간으로 넘긴다.
            float interval = framesPerSecond > 0f ? 1f / framesPerSecond : 0f;

            for (int i = from; i <= to; i++)
            {
                ShowFrame(i);
                if (interval > 0f) yield return new WaitForSecondsRealtime(interval);
            }

            _playing = null;
            onComplete?.Invoke();
        }

        private void ShowFrame(int index)
        {
            if (target == null || frames == null) return;
            if (index < 0 || index > LastIndex) return;
            if (frames[index] != null) target.sprite = frames[index];
        }
    }
}
