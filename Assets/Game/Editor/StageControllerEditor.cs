using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Game.EditorTools
{
    [CustomEditor(typeof(StageController))]
    public class StageControllerEditor : Editor
    {
        // 접힘 상태는 에디터에 기억시켜 다음에 열 때도 유지한다.
        private const string FoldoutPrefix = "Game.StageController.Foldout.";

        private readonly HashSet<string> _drawn = new();

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            _drawn.Clear();

            DrawGroup("스테이지", true, "stageId", "startPosition");
            DrawCameraGroup();
            DrawGroup("경계 · 낙사", true,
                "stageMinX", "stageMaxX", "stageMinY", "stageMaxY", "stageFallLimitY", "boundsPadding");
            DrawWallGroup();
            DrawGroup("목표 아이템", true, "totalGoalItemCount", "requiredGoalItemCount", "clearRewardCoin");
            DrawGroup("이벤트", false,
                "onGoalProgressChanged", "onStageCleared", "onPlayerFailed", "onCheckpointActivated");

            // 그룹에 넣지 않은 필드가 인스펙터에서 사라지지 않도록 남은 것을 모아 보여준다.
            DrawRemaining();

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space();
            var controller = (StageController)target;

            if (GUILayout.Button("경계 자동 계산 (Ground 타일맵 기준)"))
                AutoCalculateBounds(controller);

            if (GUILayout.Button("스테이지 검증"))
                Validate(controller);
        }

        private void DrawCameraGroup()
        {
            if (!BeginGroup("카메라", true)) return;

            DrawField("cameraZoom");
            DrawField("cameraVerticalOffset");
            DrawField("lockCameraX");
            DrawField("lockCameraY");
            DrawField("useIntroCamera");
            if (serializedObject.FindProperty("useIntroCamera").boolValue)
            {
                DrawField("introMaxSize");
            }
            else
            {
                // 숨겨도 값은 유지되므로, 다시 켜면 이전 설정이 그대로 돌아온다.
                MarkDrawn("introMaxSize");
            }

            DrawField("cameraSettingsOverride");

            if (serializedObject.FindProperty("cameraSettingsOverride").objectReferenceValue != null)
                EditorGUILayout.HelpBox("글로벌 CameraSettings 대신 이 프리셋을 사용합니다.", MessageType.Info);

            EndGroup();
        }

        // 쓰지 않는 값은 숨겨서 헷갈리지 않게 한다.
        private void DrawWallGroup()
        {
            if (!BeginGroup("투명 벽", true)) return;

            DrawField("useBoundaryWalls");
            if (serializedObject.FindProperty("useBoundaryWalls").boolValue)
            {
                DrawField("wallMode");

                var mode = (BoundaryWallMode)serializedObject.FindProperty("wallMode").enumValueIndex;
                if (mode == BoundaryWallMode.FromBounds)
                {
                    DrawField("wallOffsetFromBounds");
                }
                else
                {
                    DrawField("leftWallX");
                    DrawField("rightWallX");
                }

                DrawField("wallHeadroom");
            }
            else
            {
                EditorGUILayout.HelpBox("투명 벽이 꺼져 있어 플레이어가 화면 밖으로 나갈 수 있습니다.", MessageType.Warning);
                // 숨겨도 값은 유지되므로, 다시 켜면 이전 설정이 그대로 돌아온다.
                MarkDrawn("wallMode", "wallOffsetFromBounds", "leftWallX", "rightWallX", "wallHeadroom");
            }

            EndGroup();
        }

        private void DrawGroup(string title, bool openByDefault, params string[] fields)
        {
            if (!BeginGroup(title, openByDefault))
            {
                MarkDrawn(fields);
                return;
            }

            foreach (var field in fields) DrawField(field);
            EndGroup();
        }

        private bool BeginGroup(string title, bool openByDefault)
        {
            string key = FoldoutPrefix + title;
            bool open = EditorPrefs.GetBool(key, openByDefault);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            bool newOpen = EditorGUILayout.Foldout(open, title, true, EditorStyles.foldoutHeader);
            if (newOpen != open) EditorPrefs.SetBool(key, newOpen);

            if (!newOpen) EditorGUILayout.EndVertical();
            return newOpen;
        }

        private void EndGroup() => EditorGUILayout.EndVertical();

        private void DrawField(string name)
        {
            var property = serializedObject.FindProperty(name);
            if (property == null) return;

            EditorGUILayout.PropertyField(property, true);
            _drawn.Add(name);
        }

        private void MarkDrawn(params string[] names)
        {
            foreach (var name in names) _drawn.Add(name);
        }

        private void DrawRemaining()
        {
            var iterator = serializedObject.GetIterator();
            var leftover = new List<SerializedProperty>();

            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (iterator.name == "m_Script" || _drawn.Contains(iterator.name)) continue;
                leftover.Add(iterator.Copy());
            }

            if (leftover.Count == 0) return;

            if (!BeginGroup("기타", true)) return;
            foreach (var property in leftover) EditorGUILayout.PropertyField(property, true);
            EndGroup();
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
