using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Game.EditorTools
{
    // Grid/타일맵 레이어/StageController/카메라가 세팅된 스테이지 씬을 생성한다.
    public static class StageScaffolder
    {
        private const string StagesDir = "Assets/Game/Scenes/Stages";
        private const string GroundTilePath = "Assets/Game/Tiles/GroundTile.asset";

        [MenuItem("Game/New Stage...")]
        public static void NewStage()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var path = EditorUtility.SaveFilePanelInProject(
                "새 스테이지", "Stage01", "unity", "스테이지 씬 저장 위치", StagesDir);
            if (string.IsNullOrEmpty(path)) return;

            CreateStageScene(path);
        }

        // CLI 검증용: Unity.exe -executeMethod Game.EditorTools.StageScaffolder.CreateStageForTest
        public static void CreateStageForTest()
        {
            CreateStageScene($"{StagesDir}/StageTest.unity");
        }

        private static void CreateStageScene(string path)
        {
            EnsureFolder(StagesDir);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 카메라
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.13f, 0.18f);
            cam.transform.position = new Vector3(0f, 0f, -10f);
            camGo.AddComponent<AudioListener>();

            // 그리드 + 타일맵 레이어
            var gridGo = new GameObject("Grid");
            gridGo.AddComponent<Grid>();

            var ground = new GameObject("Ground");
            ground.transform.SetParent(gridGo.transform, false);
            var groundTilemap = ground.AddComponent<Tilemap>();
            ground.AddComponent<TilemapRenderer>();
            var tilemapCollider = ground.AddComponent<TilemapCollider2D>();
            tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
            var composite = ground.AddComponent<CompositeCollider2D>();
            composite.attachedRigidbody.bodyType = RigidbodyType2D.Static;

            var deco = new GameObject("Deco");
            deco.transform.SetParent(gridGo.transform, false);
            deco.AddComponent<Tilemap>();
            var decoRenderer = deco.AddComponent<TilemapRenderer>();
            decoRenderer.sortingOrder = -10;

            // 시작용 바닥 타일 (있을 때만)
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(GroundTilePath);
            if (tile != null)
            {
                for (int x = -6; x <= 6; x++)
                    groundTilemap.SetTile(new Vector3Int(x, -3, 0), tile);
            }

            // 배치 모드에서는 물리 스텝이 돌지 않아 컴포지트 지오메트리가 빈 채로 저장된다.
            // 저장 전에 명시적으로 생성하지 않으면 런타임에 바닥 충돌이 없어 플레이어가 뚫고 떨어진다.
            tilemapCollider.ProcessTilemapChanges();
            composite.GenerateGeometry();

            // 기믹 컨테이너
            var gimmicks = new GameObject("Gimmicks");
            gimmicks.AddComponent<GimmickContainer>();

            // 스테이지 컨트롤러 + 시작 위치
            var stageGo = new GameObject("Stage");
            var controller = stageGo.AddComponent<StageController>();
            var start = new GameObject("StartPosition");
            start.transform.SetParent(stageGo.transform, false);
            start.transform.position = new Vector3(0f, 0f, 0f);

            var so = new SerializedObject(controller);
            so.FindProperty("startPosition").objectReferenceValue = start.transform;
            so.FindProperty("stageId").stringValue = System.IO.Path.GetFileNameWithoutExtension(path);
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[Game] 스테이지 씬 생성 완료: {path}");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = System.IO.Path.GetDirectoryName(path)!.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
    }
}
