using UnityEngine;

namespace Game
{
    // 좌우 투명 벽 표식. StageController가 런타임에 만들 때 붙인다.
    // 맵 크기를 잴 때 제외하기 위한 것으로, 벽은 낙사 방지용이라 매우 높아서
    // (벽 높이 여유 기본 30) 포함하면 모든 맵이 과하게 줌아웃된다.
    public class BoundaryWall : MonoBehaviour
    {
    }
}
