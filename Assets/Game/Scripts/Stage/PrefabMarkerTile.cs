using UnityEngine;
using UnityEngine.Tilemaps;

namespace Game
{
    // 팔레트에서 프리팹을 대신 표시하는 마커 타일.
    // StageBrush가 이 타일을 페인트하면 타일 대신 프리팹을 스폰한다 — 스테이지 타일맵에는 남지 않는다.
    [CreateAssetMenu(menuName = "Game/Prefab Marker Tile", fileName = "NewPrefabMarker")]
    public class PrefabMarkerTile : TileBase
    {
        [SerializeField] private GameObject prefab;
        [SerializeField] private Sprite previewSprite;
        [SerializeField] private Color previewColor = Color.white;

        public GameObject Prefab => prefab;

        public void SetData(GameObject targetPrefab, Sprite sprite, Color color)
        {
            prefab = targetPrefab;
            previewSprite = sprite;
            previewColor = color;
        }

        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            tileData.sprite = previewSprite;
            tileData.color = previewColor;
            tileData.colliderType = Tile.ColliderType.None;
        }
    }
}
