using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Game.EditorTools
{
    // 메인 메뉴 씬(타이틀 + 스테이지 버튼 목록 + 종료)을 생성한다.
    public static class MenuSceneGenerator
    {
        public const string MenuScenePath = "Assets/Game/Scenes/MainMenu.unity";

        [MenuItem("Game/Create Main Menu Scene")]
        public static void CreateMenuScene()
        {
            if (System.IO.File.Exists(MenuScenePath))
            {
                Debug.Log($"[Game] 이미 존재합니다: {MenuScenePath}");
                return;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.10f, 0.10f, 0.14f);
            cam.transform.position = new Vector3(0f, 0f, -10f);
            camGo.AddComponent<AudioListener>();

            // 캔버스
            var canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            // 타이틀
            var title = CreateText(canvasGo.transform, "Title", "BOUNCE BALL", 96);
            var titleRt = title.rectTransform;
            titleRt.anchorMin = titleRt.anchorMax = new Vector2(0.5f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -120f);
            titleRt.sizeDelta = new Vector2(1200f, 140f);

            // 스테이지 버튼 컨테이너 (세로 배열)
            var containerRt = new GameObject("StageButtons", typeof(RectTransform)).GetComponent<RectTransform>();
            containerRt.SetParent(canvasGo.transform, false);
            containerRt.anchoredPosition = new Vector2(0f, -40f);
            containerRt.sizeDelta = new Vector2(420f, 600f);
            var layout = containerRt.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = false;
            layout.childControlWidth = false;

            // 버튼 템플릿 (비활성 상태로 두면 런타임에 복제됨)
            var template = CreateButton(containerRt, "StageButtonTemplate", "Stage");
            template.gameObject.SetActive(false);

            // 종료 버튼
            var quit = CreateButton(canvasGo.transform, "QuitButton", "Quit");
            var quitRt = quit.GetComponent<RectTransform>();
            quitRt.anchorMin = quitRt.anchorMax = new Vector2(0.5f, 0f);
            quitRt.pivot = new Vector2(0.5f, 0f);
            quitRt.anchoredPosition = new Vector2(0f, 60f);

            // MainMenuUI 바인딩
            var ui = canvasGo.AddComponent<MainMenuUI>();
            var so = new SerializedObject(ui);
            so.FindProperty("buttonContainer").objectReferenceValue = containerRt;
            so.FindProperty("stageButtonTemplate").objectReferenceValue = template;
            so.FindProperty("quitButton").objectReferenceValue = quit;
            so.ApplyModifiedPropertiesWithoutUndo();

            EnsureFolder("Assets/Game/Scenes");
            EditorSceneManager.SaveScene(scene, MenuScenePath);
            Debug.Log($"[Game] 메인 메뉴 씬 생성 완료: {MenuScenePath}");
        }

        internal static TextMeshProUGUI CreateText(Transform parent, string name, string content, float fontSize)
        {
            var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = content;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            return tmp;
        }

        internal static Button CreateButton(Transform parent, string name, string label)
        {
            var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.sizeDelta = new Vector2(400f, 90f);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            img.type = Image.Type.Sliced;
            img.color = new Color(0.25f, 0.55f, 0.95f);
            var btn = rt.gameObject.AddComponent<Button>();
            var text = CreateText(rt, "Label", label, 40);
            var textRt = text.rectTransform;
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            return btn;
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
