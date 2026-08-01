using System.Collections.Generic;
using Core;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace Game
{
    // 토스트 메시지 (짧은 안내). Systems 프리팹에 얹혀 어느 씬에서든 쓸 수 있다.
    // 메시지 오브젝트는 ObjectPool로 재사용해 반복 생성/파괴를 피한다.
    public class ToastManager : Singleton<ToastManager>
    {
        [SerializeField] private RectTransform canvasRoot;
        [SerializeField] private ToastView toastTemplate;

        [Header("기본값")]
        [SerializeField] private ToastAnchor defaultAnchor = ToastAnchor.BottomCenter;
        [SerializeField] private float defaultDuration = 1.5f;
        [Tooltip("화면 가장자리에서 띄우는 여백")]
        [SerializeField] private float edgePadding = 80f;
        [SerializeField] private float spacing = 12f;

        private ObjectPool<ToastView> _pool;
        private readonly Dictionary<ToastAnchor, RectTransform> _containers = new();

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;

            if (toastTemplate != null) toastTemplate.gameObject.SetActive(false);

            _pool = new ObjectPool<ToastView>(
                createFunc: () => Instantiate(toastTemplate, canvasRoot),
                actionOnGet: toast => toast.gameObject.SetActive(true),
                actionOnRelease: toast =>
                {
                    toast.gameObject.SetActive(false);
                    toast.transform.SetParent(canvasRoot, false);
                },
                actionOnDestroy: toast => { if (toast != null) Destroy(toast.gameObject); },
                defaultCapacity: 4);
        }

        // 어디서든 호출하는 짧은 진입점. 매니저가 없으면 조용히 무시한다.
        public static void Show(string message)
        {
            if (Instance != null) Instance.ShowToast(message, Instance.defaultAnchor, Instance.defaultDuration);
        }

        public static void Show(string message, ToastAnchor anchor)
        {
            if (Instance != null) Instance.ShowToast(message, anchor, Instance.defaultDuration);
        }

        public static void Show(string message, ToastAnchor anchor, float duration)
        {
            if (Instance != null) Instance.ShowToast(message, anchor, duration);
        }

        public void ShowToast(string message, ToastAnchor anchor, float duration)
        {
            if (string.IsNullOrEmpty(message) || toastTemplate == null || _pool == null) return;

            var toast = _pool.Get();
            toast.transform.SetParent(GetContainer(anchor), false);
            toast.transform.SetAsLastSibling();
            toast.Show(message, duration, Release);
        }

        private void Release(ToastView toast) => _pool.Release(toast);

        // 같은 위치의 토스트가 겹치지 않도록 방향별 컨테이너를 만들어 세로로 쌓는다.
        private RectTransform GetContainer(ToastAnchor anchor)
        {
            if (_containers.TryGetValue(anchor, out var existing) && existing != null) return existing;

            var go = new GameObject($"Toasts_{anchor}", typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(canvasRoot, false);

            var pivot = anchor.ToPivot();
            rect.anchorMin = pivot;
            rect.anchorMax = pivot;
            rect.pivot = pivot;
            rect.sizeDelta = Vector2.zero;

            var direction = anchor.ToEdgeOffsetDirection();
            rect.anchoredPosition = new Vector2(direction.x * edgePadding, direction.y * edgePadding);

            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            layout.childAlignment = ToAlignment(pivot);

            var fitter = go.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _containers[anchor] = rect;
            return rect;
        }

        private static TextAnchor ToAlignment(Vector2 pivot)
        {
            if (Mathf.Approximately(pivot.x, 0f)) return TextAnchor.MiddleLeft;
            if (Mathf.Approximately(pivot.x, 1f)) return TextAnchor.MiddleRight;
            return TextAnchor.MiddleCenter;
        }

        // 에디터 툴링에서 배선할 때 사용.
        public void SetReferences(RectTransform root, ToastView template)
        {
            canvasRoot = root;
            toastTemplate = template;
        }
    }
}
