using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    // 토스트 한 장. ToastManager가 풀에서 꺼내 재사용한다.
    // 글자가 길어지면 최대 폭까지 가로로 늘어나고, 그보다 길면 줄바꿈되며 세로로 늘어난다.
    [RequireComponent(typeof(CanvasGroup))]
    public class ToastView : MonoBehaviour
    {
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private LayoutElement layoutElement;
        [SerializeField] private CanvasGroup canvasGroup;

        [Tooltip("이 폭을 넘으면 줄바꿈한다")]
        [SerializeField] private float maxWidth = 700f;
        [SerializeField] private float fadeDuration = 0.2f;

        private Coroutine _routine;

        public void Show(string message, float duration, System.Action<ToastView> onFinished)
        {
            if (messageText != null)
            {
                messageText.text = message;

                // 한 줄로 그렸을 때의 폭을 재고, 최대 폭을 넘으면 줄바꿈으로 전환한다.
                messageText.textWrappingMode = TextWrappingModes.NoWrap;
                float preferred = messageText.GetPreferredValues(message).x;
                bool wrap = preferred > maxWidth;
                messageText.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
                if (layoutElement != null) layoutElement.preferredWidth = wrap ? maxWidth : preferred;
            }

            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(ShowRoutine(duration, onFinished));
        }

        private IEnumerator ShowRoutine(float duration, System.Action<ToastView> onFinished)
        {
            var group = canvasGroup != null ? canvasGroup : GetComponent<CanvasGroup>();
            group.alpha = 0f;

            yield return Fade(group, 0f, 1f, fadeDuration);
            // 일시정지 중에도 흐르도록 unscaled 시간을 쓴다
            yield return new WaitForSecondsRealtime(duration);
            yield return Fade(group, 1f, 0f, fadeDuration);

            _routine = null;
            onFinished?.Invoke(this);
        }

        private static IEnumerator Fade(CanvasGroup group, float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }
            group.alpha = to;
        }

        // 에디터 툴링에서 배선할 때 사용.
        public void SetReferences(TMP_Text text, LayoutElement layout, CanvasGroup group)
        {
            messageText = text;
            layoutElement = layout;
            canvasGroup = group;
        }
    }
}
