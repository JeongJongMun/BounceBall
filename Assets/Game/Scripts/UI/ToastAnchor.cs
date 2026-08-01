using UnityEngine;

namespace Game
{
    // 토스트를 띄울 화면 위치 (9방향)
    public enum ToastAnchor
    {
        TopLeft, TopCenter, TopRight,
        MiddleLeft, MiddleCenter, MiddleRight,
        BottomLeft, BottomCenter, BottomRight
    }

    public static class ToastAnchorExtensions
    {
        // 앵커/피벗 좌표. (0,0)이 좌하단이라 세로는 위에서부터 1 → 0.5 → 0.
        public static Vector2 ToPivot(this ToastAnchor anchor)
        {
            float x = (int)anchor % 3 * 0.5f;      // 0, 0.5, 1
            float y = 1f - (int)anchor / 3 * 0.5f; // 1, 0.5, 0
            return new Vector2(x, y);
        }

        // 화면 가장자리에서 띄울 방향. 중앙 정렬 축은 0을 유지한다.
        public static Vector2 ToEdgeOffsetDirection(this ToastAnchor anchor)
        {
            var pivot = anchor.ToPivot();
            float x = Mathf.Approximately(pivot.x, 0f) ? 1f : Mathf.Approximately(pivot.x, 1f) ? -1f : 0f;
            float y = Mathf.Approximately(pivot.y, 0f) ? 1f : Mathf.Approximately(pivot.y, 1f) ? -1f : 0f;
            return new Vector2(x, y);
        }
    }
}
