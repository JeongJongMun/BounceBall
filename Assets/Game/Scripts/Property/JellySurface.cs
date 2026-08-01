using UnityEngine;

namespace Game
{
    // 젤리 부착 방향 (기획 §10.6)
    public enum JellyAttachDirection { None, Floor, LeftWall, RightWall, Ceiling }

    // 부착 방향과 표면 기하 규칙 (기획 §5.3~§5.5). 물리에 의존하지 않는 순수 함수라 단위 테스트로 검증한다.
    public static class JellySurface
    {
        // 접촉 법선(표면 → 플레이어)으로 부착 방향을 정한다.
        // 법선이 위를 향하면 플레이어가 타일 위에 있는 것 = 바닥 부착.
        public static JellyAttachDirection FromNormal(Vector2 normal)
        {
            if (normal.sqrMagnitude < 0.0001f) return JellyAttachDirection.None;

            if (Mathf.Abs(normal.y) >= Mathf.Abs(normal.x))
                return normal.y > 0f ? JellyAttachDirection.Floor : JellyAttachDirection.Ceiling;

            // 법선이 오른쪽 = 타일이 플레이어의 왼쪽에 있다 → 왼쪽 벽 부착 (기획 §5.3)
            return normal.x > 0f ? JellyAttachDirection.LeftWall : JellyAttachDirection.RightWall;
        }

        public static Vector2 NormalOf(JellyAttachDirection direction)
        {
            switch (direction)
            {
                case JellyAttachDirection.Floor: return Vector2.up;
                case JellyAttachDirection.Ceiling: return Vector2.down;
                case JellyAttachDirection.LeftWall: return Vector2.right;
                case JellyAttachDirection.RightWall: return Vector2.left;
                default: return Vector2.zero;
            }
        }

        // A/D 입력을 표면을 따라가는 이동 벡터로 바꾼다 (기획 §5.4).
        //
        // 바닥·천장은 화면 기준 좌우를 그대로 쓴다.
        // 벽은 붙은 면에 따라 반전한다 — 벽의 좌측면(= 벽이 플레이어 오른쪽, RightWall)은 A=아래·D=위,
        // 우측면(= 벽이 플레이어 왼쪽, LeftWall)은 A=위·D=아래.
        // 이렇게 해야 바닥에서 모서리를 돌아 벽으로 올라탈 때 같은 키를 누른 채로 진행 방향이 이어진다.
        public static Vector2 MoveDirection(JellyAttachDirection direction, float input)
        {
            switch (direction)
            {
                case JellyAttachDirection.Floor:
                case JellyAttachDirection.Ceiling:
                    return new Vector2(input, 0f);
                case JellyAttachDirection.RightWall:
                    return new Vector2(0f, input);
                case JellyAttachDirection.LeftWall:
                    return new Vector2(0f, -input);
                default:
                    return Vector2.zero;
            }
        }

        // 오목 모서리를 돌 때의 새 법선·이동방향 (기획 §5.5: 바닥 → 벽 → 천장).
        // 진행 방향을 막은 면으로 올라타고, 원래 법선 방향으로 계속 진행한다.
        //
        // 볼록 모서리(표면이 그냥 끊기는 쪽)는 감아 돌지 않는다 —
        // 기획 §5.6이 "젤리 타일의 끝을 벗어난 경우" 부착을 해제하라고 규정한다.
        public static void TurnConcaveCorner(Vector2 normal, Vector2 moveDir,
            out Vector2 newNormal, out Vector2 newMoveDir)
        {
            newNormal = -moveDir;
            newMoveDir = normal;
        }
    }
}
