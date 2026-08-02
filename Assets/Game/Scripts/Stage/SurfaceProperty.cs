using UnityEngine;

namespace Game
{
    // 프리팹 콜라이더의 표면 성질 표식 (사망 없음). 젤리·얼음 블록 프리팹의 몸통에 붙여
    // 타일맵 타일처럼 부착·미끄러짐 상호작용을 받게 한다.
    // 사망 콜라이더에는 이 대신 HazardSurface를 쓴다.
    [RequireComponent(typeof(Collider2D))]
    public class SurfaceProperty : MonoBehaviour
    {
        [Label("표면 성질")]
        [Tooltip("젤리면 부착, 얼음이면 미끄러짐 상호작용이 일어난다")]
        [SerializeField] private TilePropertyType tileProperty = TilePropertyType.Default;

        public TilePropertyType TileProperty => tileProperty;

        public void SetTileProperty(TilePropertyType value) => tileProperty = value;
    }
}
