using UnityEngine;
using UnityEngine.UI;

namespace Core.UI
{
    // 일시정지 화면. 버튼은 있는 것만 배선하므로 게임마다 일부만 써도 된다.
    public class PauseScreen : UIScreen
    {
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button menuButton;

        [Tooltip("설정 버튼으로 열 화면 (없으면 버튼을 숨긴다)")]
        [SerializeField] private GameObject settingsRoot;

        private void Awake()
        {
            if (resumeButton != null) resumeButton.onClick.AddListener(() => GameManager.Instance.Resume());
            if (restartButton != null) restartButton.onClick.AddListener(() => GameManager.Instance.RestartGame());
            if (menuButton != null) menuButton.onClick.AddListener(() => GameManager.Instance.BackToMenu());

            if (settingsButton != null)
            {
                if (settingsRoot == null) settingsButton.gameObject.SetActive(false);
                else settingsButton.onClick.AddListener(() => settingsRoot.SetActive(true));
            }
        }

        // 에디터 툴링에서 배선할 때 사용.
        public void SetButtons(Button resume, Button restart, Button settings, Button menu)
        {
            resumeButton = resume;
            restartButton = restart;
            settingsButton = settings;
            menuButton = menu;
        }
    }
}
