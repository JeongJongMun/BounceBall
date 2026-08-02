using DG.Tweening;
using UnityEngine;

namespace Core.UI
{
    public abstract class UIScreen : MonoBehaviour
    {
        [SerializeField] private RectTransform panel;
        [SerializeField] private float popDuration = 0.25f;
        [Tooltip("있으면 열리고 닫힐 때 알파 페이드한다. 딤이 순간적으로 꺼졌다 켜지며 깜빡이는 걸 막는다")]
        [SerializeField] private CanvasGroup canvasGroup;

        public void Show()
        {
            // 완전히 숨겨진 뒤에 다시 열 때만 0부터. 닫히는 중에 다시 열리면 현재 알파에서 이어간다.
            if (canvasGroup != null)
            {
                canvasGroup.DOKill();
                if (!gameObject.activeSelf) canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true;
            }

            gameObject.SetActive(true);
            OnShow();

            if (canvasGroup != null)
                canvasGroup.DOFade(1f, popDuration).SetUpdate(true);

            if (panel == null) return;
            panel.DOKill();
            panel.localScale = Vector3.one * 0.8f;
            panel.DOScale(1f, popDuration).SetEase(Ease.OutBack).SetUpdate(true);
        }

        public void Hide()
        {
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(false);
                return;
            }

            if (panel == null && canvasGroup == null)
            {
                gameObject.SetActive(false);
                return;
            }

            if (canvasGroup != null)
            {
                canvasGroup.DOKill();
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
                canvasGroup.DOFade(0f, popDuration).SetUpdate(true)
                    .OnComplete(() => gameObject.SetActive(false));
            }

            if (panel == null) return;
            panel.DOKill();
            var scale = panel.DOScale(0.8f, popDuration).SetEase(Ease.InBack).SetUpdate(true);
            // 페이드가 있으면 그쪽 OnComplete가 비활성화한다.
            if (canvasGroup == null)
                scale.OnComplete(() => gameObject.SetActive(false));
        }

        protected virtual void OnShow() { }
    }
}
