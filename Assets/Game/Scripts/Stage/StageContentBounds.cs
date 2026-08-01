using UnityEngine;
using UnityEngine.Tilemaps;

namespace Game
{
    // 스테이지에 실제로 배치된 것들의 범위 = 타일 ∪ 플레이 구조물.
    // 목표 아이템·기믹·발판처럼 타일맵이 아니라 프리팹으로 놓인 것까지 포함한다.
    // 타일맵만 세면 프리팹 발판으로 쌓아 올린 세로로 긴 맵의 높이를 크게 과소평가한다.
    public static class StageContentBounds
    {
        public static bool TryGet(out Bounds bounds)
        {
            bool found = StageTiles.TryGetWorldBounds(out bounds);

            var colliders = Object.FindObjectsByType<Collider2D>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var collider in colliders)
            {
                if (!IsStageContent(collider)) continue;

                var box = collider.bounds;
                if (box.size.x <= 0f || box.size.y <= 0f) continue;

                if (!found)
                {
                    bounds = new Bounds(box.center, Vector3.zero);
                    found = true;
                }
                bounds.Encapsulate(box);
            }

            return found;
        }

        private static bool IsStageContent(Collider2D collider)
        {
            // 타일맵은 StageTiles가 이미 셌다
            if (collider is TilemapCollider2D || collider is CompositeCollider2D) return false;

            // 투명 벽은 낙사 방지용이라 매우 높다 — 포함하면 과하게 줌아웃된다
            if (collider.GetComponentInParent<BoundaryWall>() != null) return false;

            // 플레이어는 맵에 배치된 물건이 아니라 매 프레임 움직이는 대상이다
            if (collider.GetComponentInParent<Player>() != null) return false;

            return true;
        }
    }
}
