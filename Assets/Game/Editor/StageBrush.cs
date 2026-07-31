using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEngine;

namespace Game.EditorTools
{
    // 통합 스테이지 브러시. 팔레트에서 고른 것이:
    // - 일반/특수 타일이면 → 타일 페인트 (GridBrush 기본 동작)
    // - PrefabMarkerTile이면 → 타일 대신 프리팹을 Gimmicks 컨테이너에 스폰 (셀당 1개)
    // 지우개는 해당 셀의 타일과 프리팹을 모두 제거한다.
    [CustomGridBrush(false, true, false, "Stage Brush")]
    [CreateAssetMenu(menuName = "Game/Stage Brush", fileName = "StageBrush")]
    public class StageBrush : GridBrush
    {
        public override void Paint(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
        {
            var marker = SelectedMarker();
            if (marker != null)
            {
                SpawnPrefab(gridLayout, marker, position);
                return;
            }
            base.Paint(gridLayout, brushTarget, position);
        }

        public override void Erase(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
        {
            ErasePrefabAt(gridLayout, position);
            base.Erase(gridLayout, brushTarget, position);
        }

        public override void BoxFill(GridLayout gridLayout, GameObject brushTarget, BoundsInt bounds)
        {
            var marker = SelectedMarker();
            if (marker != null)
            {
                foreach (var cell in bounds.allPositionsWithin)
                    SpawnPrefab(gridLayout, marker, cell);
                return;
            }
            base.BoxFill(gridLayout, brushTarget, bounds);
        }

        public override void BoxErase(GridLayout gridLayout, GameObject brushTarget, BoundsInt bounds)
        {
            foreach (var cell in bounds.allPositionsWithin)
                ErasePrefabAt(gridLayout, cell);
            base.BoxErase(gridLayout, brushTarget, bounds);
        }

        private PrefabMarkerTile SelectedMarker()
        {
            return cellCount == 1 ? cells[0].tile as PrefabMarkerTile : null;
        }

        private static void SpawnPrefab(GridLayout gridLayout, PrefabMarkerTile marker, Vector3Int cell)
        {
            if (marker.Prefab == null) return;
            if (FindInstanceAtCell(gridLayout, cell) != null) return; // 셀당 1개

            var container = GetOrCreateContainer();
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(marker.Prefab, container.transform);
            instance.transform.position = gridLayout.CellToWorld(cell) + gridLayout.cellSize * 0.5f;
            Undo.RegisterCreatedObjectUndo(instance, "Paint Prefab");
        }

        private static void ErasePrefabAt(GridLayout gridLayout, Vector3Int cell)
        {
            var instance = FindInstanceAtCell(gridLayout, cell);
            if (instance != null) Undo.DestroyObjectImmediate(instance);
        }

        private static GameObject FindInstanceAtCell(GridLayout gridLayout, Vector3Int cell)
        {
            var container = Object.FindFirstObjectByType<GimmickContainer>();
            if (container == null) return null;

            foreach (Transform child in container.transform)
            {
                if (gridLayout.WorldToCell(child.position) == cell)
                    return child.gameObject;
            }
            return null;
        }

        private static GimmickContainer GetOrCreateContainer()
        {
            var container = Object.FindFirstObjectByType<GimmickContainer>();
            if (container != null) return container;

            var go = new GameObject("Gimmicks");
            container = go.AddComponent<GimmickContainer>();
            Undo.RegisterCreatedObjectUndo(go, "Create Gimmick Container");
            return container;
        }
    }
}
