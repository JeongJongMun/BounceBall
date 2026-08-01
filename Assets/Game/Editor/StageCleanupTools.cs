using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Game.EditorTools
{
    // 스테이지 씬의 팔레트 잔재를 청소한다.
    // 마커 타일이 타일맵에 남아 있으면(Default Brush로 칠한 실수) 기능 없는 장식이 되므로 제거한다.
    public static class StageCleanupTools
    {
        private const string StagesDir = "Assets/Game/Scenes/Stages";

        [MenuItem("Game/Cleanup Stray Palette Tiles")]
        public static void CleanupAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var openScenePath = EditorSceneManager.GetActiveScene().path;

            // 폐기된 샘플 타일은 지형이 비지 않게 기본 타일로 교체한다
            var ground = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Game/Tiles/GroundTile.asset");

            int cleaned = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { StagesDir }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                bool changed = false;

                var tileCounts = new System.Collections.Generic.Dictionary<string, int>();
                foreach (var tilemap in Object.FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
                {
                    tilemap.CompressBounds();
                    foreach (var cell in tilemap.cellBounds.allPositionsWithin)
                    {
                        var tile = tilemap.GetTile(cell);
                        if (tile == null) continue;
                        tileCounts[tile.name] = tileCounts.TryGetValue(tile.name, out var n) ? n + 1 : 1;

                        if (tile is PrefabMarkerTile)
                        {
                            tilemap.SetTile(cell, null);
                            changed = true;
                        }
                        else if (tile.name == "BouncyTile") // 폐기된 샘플 타일 (참조 비교가 아닌 이름 매칭 — 로드 인스턴스 불일치 대비)
                        {
                            tilemap.SetTile(cell, ground);
                            changed = true;
                        }
                    }
                }

                var summary = string.Join(", ",
                    System.Linq.Enumerable.Select(tileCounts, kv => $"{kv.Key}×{kv.Value}"));
                Debug.Log($"[Game] {path} 타일 구성: {(summary.Length > 0 ? summary : "없음")}");

                if (changed)
                {
                    // 배치 모드 저장 대비: 콜라이더 지오메트리 재생성
                    foreach (var collider in Object.FindObjectsByType<TilemapCollider2D>(FindObjectsSortMode.None))
                        collider.ProcessTilemapChanges();
                    foreach (var composite in Object.FindObjectsByType<CompositeCollider2D>(FindObjectsSortMode.None))
                        composite.GenerateGeometry();

                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    cleaned++;
                    Debug.Log($"[Game] 팔레트 잔재 제거: {path}");
                }
            }

            if (!string.IsNullOrEmpty(openScenePath)) EditorSceneManager.OpenScene(openScenePath, OpenSceneMode.Single);
            Debug.Log($"[Game] 팔레트 잔재 청소 완료 — 씬 {cleaned}개 수정");
        }
    }
}
