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
        private const string BackgroundPrefabPath = "Assets/Game/Prefabs/Background.prefab";

        // 스테이지 배경색 (#EBE8DD)
        private static readonly Color BackgroundColor = new Color32(0xEB, 0xE8, 0xDD, 0xFF);

        // 타일 사이를 살짝 겹쳐 타일 경계의 빈 줄(seam)을 없앤다
        private static readonly Vector3 CellGap = new Vector3(-0.05f, -0.05f, 0f);

        [MenuItem("Game/New Stage...")]
        public static void NewStage()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var path = EditorUtility.SaveFilePanelInProject(
                "새 스테이지", "Stage01", "unity", "스테이지 씬 저장 위치", StagesDir);
            if (string.IsNullOrEmpty(path)) return;

            CreateStageScene(path);
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
            cam.backgroundColor = BackgroundColor;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            camGo.AddComponent<AudioListener>();

            // 그리드 + 타일맵 레이어
            var gridGo = new GameObject("Grid");
            var grid = gridGo.AddComponent<Grid>();
            grid.cellGap = CellGap;

            var ground = new GameObject("Ground");
            ground.transform.SetParent(gridGo.transform, false);
            var groundTilemap = ground.AddComponent<Tilemap>();
            ground.AddComponent<TilemapRenderer>();
            var tilemapCollider = ground.AddComponent<TilemapCollider2D>();
            tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
            var composite = ground.AddComponent<CompositeCollider2D>();
            composite.attachedRigidbody.bodyType = RigidbodyType2D.Static;

            // 장식 레이어는 만들지 않는다 — 배경은 SpriteRenderer 오브젝트로 배치한다 (맵 배치 기획 §4).

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

            // 패럴랙스 배경. 지평선은 실행 시 스테이지 경계에 맞춰 자동 정렬된다.
            CreateBackground();

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

            GameFlowSetup.WireStageChannels(controller);

            EditorSceneManager.SaveScene(scene, path);
            Debug.Log($"[Game] 스테이지 씬 생성 완료: {path}");
        }

        // 배경을 랜덤 시드로 다시 생성해 스테이지마다 다른 배치가 나오게 한다.
        // 프리팹에 연결된 채로는 생성된 프롭(자식)을 지울 수 없으므로 연결을 해제하고 굽는다.
        private static void CreateBackground()
        {
            var backgroundPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BackgroundPrefabPath);
            if (backgroundPrefab == null)
            {
                Debug.LogWarning($"[Game] 배경 프리팹을 찾을 수 없습니다: {BackgroundPrefabPath}");
                return;
            }

            var background = (GameObject)PrefabUtility.InstantiatePrefab(backgroundPrefab);
            PrefabUtility.UnpackPrefabInstance(background, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            var parallax = background.GetComponent<ParallaxBackground>();
            if (parallax == null) return;

            int seed = Random.Range(1, 100000);
            var backgroundSo = new SerializedObject(parallax);
            backgroundSo.FindProperty("seed").intValue = seed;
            backgroundSo.ApplyModifiedPropertiesWithoutUndo();

            ParallaxBackgroundEditor.Rebuild(parallax);
            Debug.Log($"[Game] 배경 생성 (시드 {seed})");
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
