using System.IO;
using System.Linq;
using Core.Events;
using Core.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Core.EditorTools
{
    public static class CoreTemplateGenerator
    {
        private const string EventsDir = "Assets/Core/Events";
        private const string ResourcesDir = "Assets/Core/Resources";
        private const string PrefabsDir = "Assets/Core/Prefabs";
        private const string ScenesDir = "Assets/Core/Scenes";
        private const string SystemsPrefabPath = "Assets/Core/Resources/Systems.prefab";
        private const string DemoPrefabPath = "Assets/Core/Prefabs/DemoPooled.prefab";
        private const string DemoScenePath = "Assets/Core/Scenes/Demo.unity";

        // CLI 진입점: Unity.exe -batchmode -executeMethod Core.EditorTools.CoreTemplateGenerator.GenerateEverything
        public static void GenerateEverything()
        {
            GenerateAll();
            GenerateDemoScene();
        }

        [MenuItem("Core/Generate Template Assets")]
        public static void GenerateAll()
        {
            EnsureFolder(EventsDir);
            EnsureFolder(ResourcesDir);
            EnsureFolder(PrefabsDir);

            var stateChannel = GetOrCreate<IntEventChannel>($"{EventsDir}/OnGameStateChanged.asset");
            var scoreChannel = GetOrCreate<IntEventChannel>($"{EventsDir}/OnScoreChanged.asset");
            GetOrCreate<VoidEventChannel>($"{EventsDir}/OnPlayerDied.asset");

            GetOrCreateDemoPrefab();

            var root = BuildSystems(stateChannel, scoreChannel);
            PrefabUtility.SaveAsPrefabAsset(root, SystemsPrefabPath);
            Object.DestroyImmediate(root);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Core] 템플릿 에셋 생성 완료: Systems.prefab + 이벤트 채널 3종");
        }

        [MenuItem("Core/Generate Demo Scene")]
        public static void GenerateDemoScene()
        {
            EnsureFolder(ScenesDir);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.10f, 0.10f, 0.13f);
            cam.transform.position = new Vector3(0f, 0f, -10f);
            camGo.AddComponent<AudioListener>();

            var demo = new GameObject("Demo").AddComponent<Core.Demo.DemoController>();
            Bind(demo,
                ("demoSfx", FindClip(preferMusic: false)),
                ("demoBgm", FindClip(preferMusic: true)),
                ("demoPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(DemoPrefabPath)));

            EditorSceneManager.SaveScene(scene, DemoScenePath);

            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.All(s => s.path != DemoScenePath))
            {
                scenes.Add(new EditorBuildSettingsScene(DemoScenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }
            Debug.Log("[Core] 데모 씬 생성 완료: " + DemoScenePath);
        }

        // ---------- Systems 프리팹 ----------

        private static GameObject BuildSystems(IntEventChannel stateChannel, IntEventChannel scoreChannel)
        {
            var root = new GameObject("Systems");

            var gm = NewChild(root.transform, "GameManager").AddComponent<GameManager>();
            Bind(gm, ("onGameStateChanged", stateChannel), ("onScoreChanged", scoreChannel));

            NewChild(root.transform, "PoolManager").AddComponent<PoolManager>();

            BuildAudio(root.transform);
            BuildSceneLoader(root.transform);
            BuildUI(root.transform, stateChannel, scoreChannel);
            BuildEventSystem(root.transform);

            return root;
        }

        private static void BuildAudio(Transform root)
        {
            var audioGo = NewChild(root, "AudioManager");
            var am = audioGo.AddComponent<AudioManager>();

            AudioSource NewSource(string name)
            {
                var src = NewChild(audioGo.transform, name).AddComponent<AudioSource>();
                src.playOnAwake = false;
                return src;
            }

            Bind(am,
                ("sfxSource", NewSource("SFX")),
                ("bgmSourceA", NewSource("BGM A")),
                ("bgmSourceB", NewSource("BGM B")));
        }

        private static void BuildSceneLoader(Transform root)
        {
            var loaderGo = NewChild(root, "SceneLoader");
            var loader = loaderGo.AddComponent<SceneLoader>();

            var canvasGo = NewChild(loaderGo.transform, "FadeCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 2000; // Pause(1100)·Iris(1000)보다 위
            var group = canvasGo.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;

            var img = CreateFullStretch("Black", canvasGo.transform).gameObject.AddComponent<Image>();
            img.color = Color.black;

            Bind(loader, ("fadeGroup", group));
        }

        private static void BuildUI(Transform root, IntEventChannel stateChannel, IntEventChannel scoreChannel)
        {
            var uiGo = NewChild(root, "UI");
            var canvas = uiGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = uiGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            uiGo.AddComponent<GraphicRaycaster>();
            var uiManager = uiGo.AddComponent<UIManager>();

            // 메인 메뉴
            var (menuGo, menuPanel) = CreateScreenBase(uiGo.transform, "MainMenu", new Vector2(600, 460));
            CreateText(menuPanel, "Title", "GAME TITLE", 64, new Vector2(0, 140), new Vector2(560, 100));
            var playBtn = CreateButton(menuPanel, "PlayButton", "Play", new Vector2(0, -20));
            var quitBtn = CreateButton(menuPanel, "QuitButton", "Quit", new Vector2(0, -120));
            var menu = menuGo.AddComponent<MainMenuScreen>();
            Bind(menu, ("panel", menuPanel), ("playButton", playBtn), ("quitButton", quitBtn));

            // 일시정지
            var (pauseGo, pausePanel) = CreateScreenBase(uiGo.transform, "Pause", new Vector2(600, 400));
            CreateText(pausePanel, "Title", "PAUSED", 56, new Vector2(0, 110), new Vector2(560, 80));
            var resumeBtn = CreateButton(pausePanel, "ResumeButton", "Resume", new Vector2(0, -10));
            var pauseMenuBtn = CreateButton(pausePanel, "MenuButton", "Menu", new Vector2(0, -110));
            var pauseScreen = pauseGo.AddComponent<PauseScreen>();
            Bind(pauseScreen, ("panel", pausePanel), ("resumeButton", resumeBtn), ("menuButton", pauseMenuBtn));

            // 결과창
            var (resultGo, resultPanel) = CreateScreenBase(uiGo.transform, "Result", new Vector2(600, 520));
            CreateText(resultPanel, "Title", "GAME OVER", 56, new Vector2(0, 180), new Vector2(560, 80));
            var scoreText = CreateText(resultPanel, "ScoreText", "Score  0", 40, new Vector2(0, 80), new Vector2(560, 60));
            var highScoreText = CreateText(resultPanel, "HighScoreText", "Best  0", 32, new Vector2(0, 25), new Vector2(560, 50));
            var restartBtn = CreateButton(resultPanel, "RestartButton", "Restart", new Vector2(0, -70));
            var resultMenuBtn = CreateButton(resultPanel, "MenuButton", "Menu", new Vector2(0, -170));
            var resultScreen = resultGo.AddComponent<ResultScreen>();
            Bind(resultScreen,
                ("panel", resultPanel), ("scoreText", scoreText), ("highScoreText", highScoreText),
                ("restartButton", restartBtn), ("menuButton", resultMenuBtn));

            // 점수 HUD (배경 없음, 상단 중앙)
            var hudRt = CreateFullStretch("Hud", uiGo.transform);
            var hudText = CreateText(hudRt, "ScoreText", "0", 56, Vector2.zero, new Vector2(400, 80));
            var hudTextRt = hudText.rectTransform;
            hudTextRt.anchorMin = hudTextRt.anchorMax = new Vector2(0.5f, 1f);
            hudTextRt.pivot = new Vector2(0.5f, 1f);
            hudTextRt.anchoredPosition = new Vector2(0, -40);
            var hud = hudRt.gameObject.AddComponent<ScoreHud>();
            Bind(hud, ("panel", hudTextRt), ("scoreText", hudText), ("onScoreChanged", scoreChannel));

            Bind(uiManager,
                ("onGameStateChanged", stateChannel),
                ("mainMenu", menu), ("pause", pauseScreen), ("result", resultScreen), ("hud", hud));

            menuGo.SetActive(false);
            pauseGo.SetActive(false);
            resultGo.SetActive(false);
            hudRt.gameObject.SetActive(false);
        }

        private static void BuildEventSystem(Transform root)
        {
            var esGo = NewChild(root, "EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<InputSystemUIInputModule>();
        }

        // ---------- 데모 에셋 ----------

        private static GameObject GetOrCreateDemoPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(DemoPrefabPath);
            if (existing != null) return existing;

            var go = new GameObject("DemoPooled");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            sr.color = new Color(1f, 0.6f, 0.2f);
            go.AddComponent<PooledObject>();
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, DemoPrefabPath);
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static AudioClip FindClip(bool preferMusic)
        {
            var searchRoot = AssetDatabase.IsValidFolder("Assets/Audio") ? "Assets/Audio" : "Assets";
            var paths = AssetDatabase.FindAssets("t:AudioClip", new[] { searchRoot })
                .Select(AssetDatabase.GUIDToAssetPath)
                .ToArray();
            if (paths.Length == 0) return null;

            var preferred = paths.FirstOrDefault(p =>
                p.ToLowerInvariant().Contains("music") == preferMusic);
            return AssetDatabase.LoadAssetAtPath<AudioClip>(preferred ?? paths[0]);
        }

        // ---------- 공용 헬퍼 ----------

        private static GameObject NewChild(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go;
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        private static RectTransform CreateFullStretch(string name, Transform parent)
        {
            var rt = CreateRect(name, parent);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        private static (GameObject screen, RectTransform panel) CreateScreenBase(
            Transform parent, string name, Vector2 panelSize)
        {
            var screenRt = CreateFullStretch(name, parent);
            var bg = screenRt.gameObject.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.6f);

            var panel = CreateRect("Panel", screenRt);
            panel.sizeDelta = panelSize;
            var panelImg = panel.gameObject.AddComponent<Image>();
            panelImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            panelImg.type = Image.Type.Sliced;
            panelImg.color = new Color(0.13f, 0.13f, 0.17f, 0.98f);

            return (screenRt.gameObject, panel);
        }

        private static TextMeshProUGUI CreateText(
            Transform parent, string name, string content, float fontSize, Vector2 pos, Vector2 size)
        {
            var rt = CreateRect(name, parent);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.text = content;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            return tmp;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 pos)
        {
            var rt = CreateRect(name, parent);
            rt.sizeDelta = new Vector2(320, 80);
            rt.anchoredPosition = pos;
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            img.type = Image.Type.Sliced;
            img.color = new Color(0.25f, 0.55f, 0.95f);
            var btn = rt.gameObject.AddComponent<Button>();
            CreateText(rt, "Label", label, 36, Vector2.zero, new Vector2(320, 80));
            return btn;
        }

        private static void Bind(Component target, params (string field, Object value)[] bindings)
        {
            var so = new SerializedObject(target);
            foreach (var (field, value) in bindings)
            {
                var prop = so.FindProperty(field);
                if (prop == null)
                {
                    Debug.LogError($"[Core] 필드 없음: {target.GetType().Name}.{field}");
                    continue;
                }
                prop.objectReferenceValue = value;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path)!.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static T GetOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
