using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEngine;

namespace Game.EditorTools
{
    // 타일 팔레트의 브러시 드롭다운에서 선택해 프리팹을 그리드 셀 단위로 배치하는 브러시.
    // 셀당 1개만 배치되며, 배치물은 씬의 GimmickContainer 아래에 정리된다.
    [CustomGridBrush(false, true, false, "Prefab Brush")]
    [CreateAssetMenu(menuName = "Game/Prefab Brush", fileName = "NewPrefabBrush")]
    public class PrefabPaletteBrush : GridBrushBase
    {
        [SerializeField] private GameObject prefab;

        public GameObject Prefab
        {
            get => prefab;
            set => prefab = value;
        }

        public override void Paint(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
        {
            if (prefab == null) return;
            if (FindInstanceAtCell(gridLayout, position) != null) return; // 셀당 1개

            var parent = GetOrCreateContainer();
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.transform);
            instance.transform.position = CellCenterWorld(gridLayout, position);
            Undo.RegisterCreatedObjectUndo(instance, "Paint Prefab");
        }

        public override void Erase(GridLayout gridLayout, GameObject brushTarget, Vector3Int position)
        {
            var instance = FindInstanceAtCell(gridLayout, position);
            if (instance != null) Undo.DestroyObjectImmediate(instance);
        }

        public override void BoxFill(GridLayout gridLayout, GameObject brushTarget, BoundsInt bounds)
        {
            foreach (var cell in bounds.allPositionsWithin)
                Paint(gridLayout, brushTarget, cell);
        }

        public override void BoxErase(GridLayout gridLayout, GameObject brushTarget, BoundsInt bounds)
        {
            foreach (var cell in bounds.allPositionsWithin)
                Erase(gridLayout, brushTarget, cell);
        }

        private static Vector3 CellCenterWorld(GridLayout gridLayout, Vector3Int cell)
        {
            return gridLayout.CellToWorld(cell) + gridLayout.cellSize * 0.5f;
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
