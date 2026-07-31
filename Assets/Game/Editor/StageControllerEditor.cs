using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Game.EditorTools
{
    [CustomEditor(typeof(StageController))]
    public class StageControllerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();

            var controller = (StageController)target;

            if (GUILayout.Button("경계 자동 계산 (Ground 타일맵 기준)"))
                AutoCalculateBounds(controller);

            if (GUILayout.Button("스테이지 검증"))
                Validate(controller);
        }

        private static void AutoCalculateBounds(StageController controller)
        {
            var groundTilemap = FindGroundTilemap();
            if (groundTilemap == null)
            {
                Debug.LogError("[Game] Ground 타일맵을 찾을 수 없습니다. Grid 아래 'Ground' 오브젝트가 필요합니다.");
                return;
            }

            groundTilemap.CompressBounds();
            var local = groundTilemap.localBounds;
            var min = groundTilemap.transform.TransformPoint(local.min);
            var max = groundTilemap.transform.TransformPoint(local.max);

            Undo.RecordObject(controller, "경계 자동 계산");
            controller.SetBounds(min.x, max.x, min.y, max.y, min.y - 2f);
            EditorUtility.SetDirty(controller);
            Debug.Log($"[Game] 경계 설정: X({min.x}~{max.x}) Y({min.y}~{max.y}) 낙사선 {min.y - 2f}");
        }

        private static void Validate(StageController controller)
        {
            int errors = 0, warnings = 0;

            // 시작 위치
            if (controller.StartPosition == null)
            {
                Debug.LogError("[검증] StartPosition이 지정되지 않았습니다.");
                errors++;
            }
            else if (controller.StartPosition.position.y <= controller.StageFallLimitY)
            {
                Debug.LogError("[검증] StartPosition이 낙사선 아래에 있습니다.");
                errors++;
            }

            // 목표 아이템 수량
            var goalItems = Object.FindObjectsByType<GoalItem>(FindObjectsSortMode.None);
            if (goalItems.Length != controller.TotalGoalItemCount)
            {
                Undo.RecordObject(controller, "목표 아이템 수량 동기화");
                controller.SetGoalCounts(goalItems.Length, controller.RequiredGoalItemCount);
                EditorUtility.SetDirty(controller);
                Debug.Log($"[검증] TotalGoalItemCount를 실제 배치 수량({goalItems.Length})으로 동기화했습니다.");
            }
            if (controller.RequiredGoalItemCount <= 0)
            {
                Debug.LogWarning("[검증] RequiredGoalItemCount가 0 이하입니다. 클리어 조건을 설정하세요.");
                warnings++;
            }
            else if (controller.RequiredGoalItemCount > goalItems.Length)
            {
                Debug.LogError($"[검증] 요구 수량({controller.RequiredGoalItemCount})이 배치 수량({goalItems.Length})보다 많습니다.");
                errors++;
            }

            // 낙사선 아래 배치물
            warnings += WarnBelowFallLine<PropertyItem>(controller);
            warnings += WarnBelowFallLine<GoalItem>(controller);
            warnings += WarnBelowFallLine<Checkpoint>(controller);

            Debug.Log($"[검증] 완료 — 오류 {errors}건, 경고 {warnings}건");
        }

        private static int WarnBelowFallLine<T>(StageController controller) where T : MonoBehaviour
        {
            int count = 0;
            foreach (var item in Object.FindObjectsByType<T>(FindObjectsSortMode.None))
            {
                if (item.transform.position.y <= controller.StageFallLimitY)
                {
                    Debug.LogWarning($"[검증] {typeof(T).Name} '{item.name}'이 낙사선 아래에 있습니다.", item);
                    count++;
                }
            }
            return count;
        }

        private static Tilemap FindGroundTilemap()
        {
            foreach (var tilemap in Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
            {
                if (tilemap.gameObject.name == "Ground") return tilemap;
            }
            return null;
        }
    }
}
