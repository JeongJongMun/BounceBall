using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private Button startButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        [SerializeField] private Canvas stageCanvas;

        [Tooltip("시작 버튼 연출을 재생할 타이틀 배경. 비워두면 연출 없이 바로 넘어간다")]
        [SerializeField] private TitleBackgroundAnimator titleAnimator;

        private bool _starting;

        // 타이틀에서만 이 오브젝트가 켜진다. 스테이지 선택으로 넘어가면 꺼진다.
        public static bool IsTitleActive => FindFirstObjectByType<MainMenuUI>() != null;

        // 스테이지 선택·설정에서 돌아오면 다시 눌릴 수 있어야 한다.
        private void OnEnable()
        {
            _starting = false;
            SetButtonsInteractable(true);
        }

        private void Awake()
        {
            if (startButton)
            {
                startButton.onClick.AddListener(() =>
                {
                    // 연출이 도는 동안 다시 눌리면 중복 진입한다.
                    if (_starting) return;
                    _starting = true;
                    SetButtonsInteractable(false);

                    if (titleAnimator != null) titleAnimator.PlayStart(EnterStageSelect);
                    else EnterStageSelect();
                });
            }

            if (settingsButton)
            {
                settingsButton.onClick.AddListener(() =>
                {
                    var window = FindSettingsWindow();
                    if (window != null) window.Open();
                });
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(QuitGame);
            }
        }

        // 스테이지에서 돌아온 경우에는 메인 화면을 건너뛰고 스테이지 선택 화면을 연다.
        private void Start()
        {
            if (!MenuNavigation.ConsumeStageSelectRequest()) return;
            if (stageCanvas == null) return;

            stageCanvas.gameObject.SetActive(true);
            gameObject.SetActive(false);
        }

        // 시작 연출이 끝난 뒤 실제로 게임(스테이지 선택)으로 넘어간다.
        private void EnterStageSelect()
        {
            if (stageCanvas != null) stageCanvas.gameObject.SetActive(true);
            gameObject.SetActive(false);
        }

        private void SetButtonsInteractable(bool value)
        {
            if (startButton != null) startButton.interactable = value;
            if (settingsButton != null) settingsButton.interactable = value;
            if (quitButton != null) quitButton.interactable = value;
        }

        // 설정 창은 평소 꺼져 있으므로 비활성 오브젝트까지 찾는다.
        private static SettingsWindow FindSettingsWindow()
        {
            var windows = FindObjectsByType<SettingsWindow>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            return windows.Length > 0 ? windows[0] : null;
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
