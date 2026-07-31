using DG.Tweening;
using UnityEngine;

namespace Core.UI
{
    public abstract class UIScreen : MonoBehaviour
    {
        [SerializeField] private RectTransform panel;
        [SerializeField] private float popDuration = 0.25f;

        public void Show()
        {
            gameObject.SetActive(true);
            OnShow();
            if (panel == null) return;
            panel.DOKill();
            panel.localScale = Vector3.one * 0.8f;
            panel.DOScale(1f, popDuration).SetEase(Ease.OutBack).SetUpdate(true);
        }

        public void Hide()
        {
            if (panel == null || !gameObject.activeSelf)
            {
                gameObject.SetActive(false);
                return;
            }
            panel.DOKill();
            panel.DOScale(0.8f, popDuration).SetEase(Ease.InBack).SetUpdate(true)
                .OnComplete(() => gameObject.SetActive(false));
        }

        protected virtual void OnShow() { }
    }
}
