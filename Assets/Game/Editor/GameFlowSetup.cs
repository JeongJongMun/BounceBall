using Core;
using Core.Events;
using Core.UI;
using Game.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.EditorTools
{
    // 씬 기반 메뉴 흐름에 맞게 Systems.prefab을 조정하고 메뉴 씬/스테이지 DB를 준비한다.
    public static class GameFlowSetup
    {
        private const string SystemsPrefabPath = "Assets/Core/Resources/Systems.prefab";
        private const string EventsDir = "Assets/Game/Events";
        private const string StagesDir = "Assets/Game/Scenes/Stages";
        private const string GoalProgressChannelPath = EventsDir + "/OnGoalProgressChanged.asset";
        private const string StageClearedChannelPath = EventsDir + "/OnStageCleared.asset";
        private const string PlayerFailedChannelPath = EventsDir + "/OnPlayerFailed.asset";
        private const string CheckpointActivatedChannelPath = EventsDir + "/OnCheckpointActivated.asset";

        // CLI 진입점: Unity.exe -batchmode -executeMethod Game.EditorTools.GameFlowSetup.ApplyAll
        [MenuItem("Game/Apply Game Flow Setup")]
        public static void ApplyAll()
        {
            CreateEventChannels();
            AdjustSystemsPrefab();
            MenuSceneGenerator.CreateMenuScene();
            StageDatabaseTools.Sync();
            WireAllStageScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("[Game] 게임 흐름 셋업 완료");
        }

        // 채널을 새로 추가하면 기존 스테이지 씬은 배선이 비어 있게 된다.
        // 스테이지마다 검증을 누르게 하지 않고 여기서 한 번에 채운다.
        private static void WireAllStageScenes()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var openScenePath = EditorSceneManager.GetActiveScene().path;
            int wired = 0;

            foreach (var guid in AssetDatabase.FindAssets("t:Scene", new[] { StagesDir }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

                bool changed = false;
                foreach (var controller in UnityEngine.Object.FindObjectsByType<StageController>(FindObjectsSortMode.None))
                {
                    if (WireStageChannels(controller)) changed = true;
                }

                if (changed)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    wired++;
                }
            }

            // 원래 열려 있던 씬으로 복귀
            if (!string.IsNullOrEmpty(openScenePath)) EditorSceneManager.OpenScene(openScenePath, OpenSceneMode.Single);

            if (wired > 0) Debug.Log($"[Game] 스테이지 씬 {wired}개의 이벤트 채널을 배선했습니다.");
        }

        // 게임 전용 이벤트 채널. Core 것과 섞이지 않게 Assets/Game/Events에 둔다.
        private static void CreateEventChannels()
        {
            EnsureFolder(EventsDir);
            CreateChannel<StringEventChannel>(GoalProgressChannelPath);
            CreateChannel<VoidEventChannel>(StageClearedChannelPath);
            CreateChannel<VoidEventChannel>(PlayerFailedChannelPath);
            CreateChannel<VoidEventChannel>(CheckpointActivatedChannelPath);
        }

        private static T CreateChannel<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing;

            var channel = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(channel, path);
            return channel;
        }

        // StageController의 이벤트 채널을 배선한다. 비어 있는 필드만 채우고, 바뀐 게 있으면 true.
        // 스테이지 씬은 채널 에셋보다 먼저 만들어질 수 있어 씬 생성/검증 양쪽에서 호출한다.
        internal static bool WireStageChannels(StageController controller)
        {
            var so = new SerializedObject(controller);
            bool changed = false;
            changed |= AssignIfEmpty(so, "onGoalProgressChanged",
                AssetDatabase.LoadAssetAtPath<StringEventChannel>(GoalProgressChannelPath));
            changed |= AssignIfEmpty(so, "onStageCleared",
                AssetDatabase.LoadAssetAtPath<VoidEventChannel>(StageClearedChannelPath));
            changed |= AssignIfEmpty(so, "onPlayerFailed",
                AssetDatabase.LoadAssetAtPath<VoidEventChannel>(PlayerFailedChannelPath));
            changed |= AssignIfEmpty(so, "onCheckpointActivated",
                AssetDatabase.LoadAssetAtPath<VoidEventChannel>(CheckpointActivatedChannelPath));

            if (changed) so.ApplyModifiedPropertiesWithoutUndo();
            return changed;
        }

        private static bool AssignIfEmpty(SerializedObject so, string fieldName, UnityEngine.Object value)
        {
            var property = so.FindProperty(fieldName);
            if (property == null || property.objectReferenceValue != null || value == null) return false;
            property.objectReferenceValue = value;
            return true;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = System.IO.Path.GetDirectoryName(path)!.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }

        // Systems.prefab: 오버레이 메인 메뉴 제거 + BackToMenu가 MainMenu 씬을 로드하도록 설정.
        // 재실행해도 안전하다 (이미 적용된 항목은 건너뜀).
        private static void AdjustSystemsPrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(SystemsPrefabPath);
            try
            {
                var uiManager = root.GetComponentInChildren<UIManager>(true);
                if (uiManager != null)
                {
                    var so = new SerializedObject(uiManager);
                    var mainMenuProp = so.FindProperty("mainMenu");
                    var menuScreen = mainMenuProp.objectReferenceValue as MainMenuScreen;
                    mainMenuProp.objectReferenceValue = null;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    if (menuScreen != null) Object.DestroyImmediate(menuScreen.gameObject);
                }

                var gameManager = root.GetComponentInChildren<GameManager>(true);
                if (gameManager != null)
                {
                    var so = new SerializedObject(gameManager);
                    so.FindProperty("menuSceneName").stringValue = "MainMenu";
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                if (uiManager != null)
                {
                    ReplaceScoreHudWithGoalHud(uiManager);
                    EnsureStageClearScreen(uiManager);
                }

                PrefabUtility.SaveAsPrefabAsset(root, SystemsPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
            Debug.Log("[Game] Systems.prefab 조정 완료 (메뉴 제거, 목표 HUD, 클리어 화면)");
        }

        // 점수 HUD를 목표 수량 HUD로 교체한다. 이 게임엔 점수 개념이 없다 (기획 §21.4).
        private static void ReplaceScoreHudWithGoalHud(UIManager uiManager)
        {
            if (uiManager.GetComponentInChildren<GoalHud>(true) != null) return;

            var scoreHud = uiManager.GetComponentInChildren<ScoreHud>(true);
            if (scoreHud == null) return;

            var hudGo = scoreHud.gameObject;
            var hudSo = new SerializedObject(scoreHud);
            var panel = hudSo.FindProperty("panel").objectReferenceValue as RectTransform;
            var text = hudSo.FindProperty("scoreText").objectReferenceValue as TMPro.TMP_Text;

            Object.DestroyImmediate(scoreHud);

            var goalHud = hudGo.AddComponent<GoalHud>();
            goalHud.SetReferences(text, AssetDatabase.LoadAssetAtPath<StringEventChannel>(GoalProgressChannelPath));
            if (text != null) text.text = "0 / 0";

            var goalSo = new SerializedObject(goalHud);
            goalSo.FindProperty("panel").objectReferenceValue = panel != null ? panel : hudGo.GetComponent<RectTransform>();
            goalSo.ApplyModifiedPropertiesWithoutUndo();

            SetScreenReference(uiManager, "hud", goalHud);
        }

        // 클리어 화면(제목 + 다음/다시하기/메뉴)을 UI 캔버스 아래에 만든다.
        private static void EnsureStageClearScreen(UIManager uiManager)
        {
            var canvas = uiManager.transform;
            if (canvas.Find("Clear") != null) return;

            var screenGo = new GameObject("Clear", typeof(RectTransform));
            screenGo.transform.SetParent(canvas, false);
            StretchFull(screenGo.GetComponent<RectTransform>());

            var panel = new GameObject("Panel", typeof(RectTransform)).GetComponent<RectTransform>();
            panel.SetParent(screenGo.transform, false);
            panel.sizeDelta = new Vector2(600f, 560f);
            var panelImage = panel.gameObject.AddComponent<UnityEngine.UI.Image>();
            panelImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            panelImage.type = UnityEngine.UI.Image.Type.Sliced;
            panelImage.color = new Color(0.12f, 0.14f, 0.2f, 0.95f);

            var title = MenuSceneGenerator.CreateText(panel, "Title", "STAGE CLEAR", 72);
            var titleRt = title.rectTransform;
            titleRt.anchorMin = titleRt.anchorMax = new Vector2(0.5f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -60f);
            titleRt.sizeDelta = new Vector2(560f, 100f);

            // 기본 TMP 폰트에 한글 글리프가 없어 라벨은 영문으로 둔다 (기존 UI와도 일치).
            var next = MenuSceneGenerator.CreateButton(panel, "NextButton", "Next Stage");
            var restart = MenuSceneGenerator.CreateButton(panel, "RestartButton", "Retry");
            var menu = MenuSceneGenerator.CreateButton(panel, "MenuButton", "Menu");
            PlaceButton(next, -40f);
            PlaceButton(restart, -150f);
            PlaceButton(menu, -260f);

            var screen = screenGo.AddComponent<StageClearScreen>();
            screen.SetButtons(next, restart, menu);
            var so = new SerializedObject(screen);
            so.FindProperty("panel").objectReferenceValue = panel;
            so.ApplyModifiedPropertiesWithoutUndo();

            screenGo.SetActive(false);
            SetScreenReference(uiManager, "clear", screen);
        }

        private static void PlaceButton(UnityEngine.UI.Button button, float y)
        {
            var rt = button.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, y);
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void SetScreenReference(UIManager uiManager, string fieldName, UIScreen screen)
        {
            var so = new SerializedObject(uiManager);
            so.FindProperty(fieldName).objectReferenceValue = screen;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
