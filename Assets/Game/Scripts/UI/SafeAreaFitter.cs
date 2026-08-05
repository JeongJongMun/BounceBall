using UnityEngine;

namespace Game
{
    // 노치·펀치홀·홈 인디케이터를 피해 UI를 안전 영역 안으로 밀어 넣는다.
    //
    // 전체 화면 배경에는 붙이지 않는다 — 배경까지 줄이면 노치 주변이 비어 보인다.
    // 화면 가장자리에 붙는 조작 UI(상단 바·퀵슬롯)만 감싸는 것이 맞다.
    // 터치 이동 영역에도 붙이지 않는다. 가장자리를 눌러도 이동이 먹혀야 한다.
    [RequireComponent(typeof(RectTransform))]
    [DisallowMultipleComponent]
    public class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform _rect;
        private Rect _appliedSafeArea;
        private int _appliedWidth;
        private int _appliedHeight;

        private RectTransform Rect => _rect != null ? _rect : (_rect = GetComponent<RectTransform>());

        private void OnEnable() => Apply();

        // 화면 회전, 브라우저 창 크기 변경, 기기 방향 전환에 따라 안전 영역이 바뀐다.
        private void Update()
        {
            if (Screen.safeArea == _appliedSafeArea
                && Screen.width == _appliedWidth
                && Screen.height == _appliedHeight) return;
            Apply();
        }

        private void Apply()
        {
            var safeArea = Screen.safeArea;
            if (!TryComputeAnchors(safeArea, Screen.width, Screen.height, out var min, out var max)) return;

            Rect.anchorMin = min;
            Rect.anchorMax = max;
            Rect.offsetMin = Vector2.zero;
            Rect.offsetMax = Vector2.zero;

            _appliedSafeArea = safeArea;
            _appliedWidth = Screen.width;
            _appliedHeight = Screen.height;
        }

        // 안전 영역(픽셀)을 앵커 비율로 바꾼다.
        // 값이 이상하면(초기화 전 0 크기 등) 앵커를 건드리지 않도록 false를 돌려준다 —
        // 여기서 0으로 나누면 UI가 한 점으로 찌그러진 채 복구되지 않는다.
        public static bool TryComputeAnchors(Rect safeArea, int screenWidth, int screenHeight,
            out Vector2 anchorMin, out Vector2 anchorMax)
        {
            anchorMin = Vector2.zero;
            anchorMax = Vector2.one;

            if (screenWidth <= 0 || screenHeight <= 0) return false;
            if (safeArea.width <= 0f || safeArea.height <= 0f) return false;

            anchorMin = new Vector2(safeArea.xMin / screenWidth, safeArea.yMin / screenHeight);
            anchorMax = new Vector2(safeArea.xMax / screenWidth, safeArea.yMax / screenHeight);
            return true;
        }
    }
}
