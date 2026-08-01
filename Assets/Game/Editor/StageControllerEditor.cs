using UnityEditor;
using UnityEditor.SceneManagement;
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

            // 타일 끝에서 여백만큼 더 보여주고 카메라가 멈춘다. 투명 벽도 여백 바깥에 선다.
            float pad = controller.BoundsPadding;
            Undo.RecordObject(controller, "경계 자동 계산");
            controller.SetBounds(min.x - pad, max.x + pad, min.y - pad, max.y + pad, min.y - 2f);
            EditorUtility.SetDirty(controller);
            Debug.Log($"[Game] 경계 설정 (여백 {pad}): X({min.x - pad}~{max.x + pad}) Y({min.y - pad}~{max.y + pad}) 낙사선 {min.y - 2f}");
        }

        // CLI 일괄 재계산: Unity.exe -batchmode -executeMethod Game.EditorTools.StageControllerEditor.RecalcAllStageBounds
        public static void RecalcAllStageBounds()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Game/Scenes/Stages" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                foreach (var controller in Object.FindObjectsByType<StageController>(FindObjectsSortMode.None))
                    AutoCalculateBounds(controller);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            Debug.Log("[Game] 전체 스테이지 경계 재계산 완료");
        }

        private static void Validate(StageController controller)
        {
            int errors = 0, warnings = 0;

            // 채널 에셋보다 먼저 만들어진 씬은 이벤트 배선이 비어 있다 (HUD가 갱신되지 않음). 조용히 채운다.
            if (GameFlowSetup.WireStageChannels(controller))
            {
                EditorUtility.SetDirty(controller);
                EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
                Debug.Log("[검증] 비어 있던 이벤트 채널(목표 진행도/클리어/실패)을 배선했습니다.");
            }

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

            // 타일맵에 남은 마커 타일 (기본 브러시로 칠한 경우) → 실제 프리팹으로 자동 변환
            FixStrayMarkerTiles();

            // 경계 밖 타일: 카메라가 안 보여주고 투명 벽이 막아서 갈 수 없는 영역이 된다
            var ground = FindGroundTilemap();
            if (ground != null)
            {
                ground.CompressBounds();
                var local = ground.localBounds;
                var tileMin = ground.transform.TransformPoint(local.min);
                var tileMax = ground.transform.TransformPoint(local.max);
                if (tileMin.x < controller.StageMinX || tileMax.x > controller.StageMaxX ||
                    tileMax.y > controller.StageMaxY)
                {
                    Debug.LogError("[검증] Ground 타일이 경계 밖에 있습니다 — 카메라에 안 보이고 투명 벽에 막혀 갈 수 없는 영역입니다. [경계 자동 계산]을 다시 눌러주세요.");
                    errors++;
                }
            }

            // 투명 벽
            if (!controller.UseBoundaryWalls)
            {
                Debug.LogWarning("[검증] 투명 벽이 꺼져 있습니다. 플레이어가 화면 밖으로 나갈 수 있습니다.");
                warnings++;
            }
            else
            {
                errors += WarnOutsideWalls<GoalItem>(controller);
                errors += WarnOutsideWalls<PropertyItem>(controller);
                errors += WarnOutsideWalls<Checkpoint>(controller);
            }

            // 낙사선 아래 배치물
            warnings += WarnBelowFallLine<PropertyItem>(controller);
            warnings += WarnBelowFallLine<GoalItem>(controller);
            warnings += WarnBelowFallLine<Checkpoint>(controller);

            Debug.Log($"[검증] 완료 — 오류 {errors}건, 경고 {warnings}건");
        }

        // 마커 타일은 StageBrush로 칠해야 프리팹이 스폰된다. 기본 브러시로 칠하면 기능 없는 타일만 남는다.
        // 배치 의도는 명확하므로 타일을 지우고 그 자리에 실제 프리팹을 스폰해 준다.
        private static void FixStrayMarkerTiles()
        {
            foreach (var tilemap in Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
            {
                tilemap.CompressBounds();
                foreach (var cell in tilemap.cellBounds.allPositionsWithin)
                {
                    if (tilemap.GetTile(cell) is not PrefabMarkerTile marker) continue;

                    tilemap.SetTile(cell, null);
                    StageBrush.SpawnPrefab(tilemap.layoutGrid, marker, cell);
                    EditorSceneManager.MarkSceneDirty(tilemap.gameObject.scene);
                    Debug.Log(
                        $"[검증] '{tilemap.name}' {cell}의 마커 타일 '{marker.name}'을 실제 아이템으로 변환했습니다. " +
                        "(기본 브러시로 칠해진 실수 — Tile Palette 브러시를 'StageBrush'로 바꿔주세요)",
                        tilemap);
                }
            }
        }

        // 벽 바깥에 있는 배치물은 플레이어가 닿을 수 없다.
        private static int WarnOutsideWalls<T>(StageController controller) where T : MonoBehaviour
        {
            int count = 0;
            foreach (var item in Object.FindObjectsByType<T>(FindObjectsSortMode.None))
            {
                float x = item.transform.position.x;
                if (x < controller.LeftWallX || x > controller.RightWallX)
                {
                    Debug.LogError($"[검증] {typeof(T).Name} '{item.name}'이 투명 벽 바깥에 있어 닿을 수 없습니다.", item);
                    count++;
                }
            }
            return count;
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
