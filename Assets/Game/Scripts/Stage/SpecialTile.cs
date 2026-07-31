using UnityEngine;
using UnityEngine.Tilemaps;

namespace Game
{
    // 성질을 가진 타일 (기획 §1.3). 플레이어 성질과의 조합으로 반응이 달라진다.
    // 이 컴포넌트가 없는 일반 타일은 TilePropertyType.Default로 간주한다.
    [CreateAssetMenu(menuName = "Game/Special Tile", fileName = "NewSpecialTile")]
    public class SpecialTile : Tile
    {
        [SerializeField] private TilePropertyType tileProperty = TilePropertyType.Default;

        public TilePropertyType TileProperty => tileProperty;

        public void SetTileProperty(TilePropertyType value) => tileProperty = value;
    }
}
