using Core;
using Core.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.UI
{
    // 스테이지 클리어 화면 (기획 §22.2 — 다음 스테이지 또는 재시작 기능 제공).
    public class StageClearScreen : UIScreen
    {
        [SerializeField] private Button nextButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button menuButton;

        private void Awake()
        {
            if (nextButton != null) nextButton.onClick.AddListener(LoadNextStage);
            if (restartButton != null) restartButton.onClick.AddListener(() => GameManager.Instance.RestartGame());
            if (menuButton != null) menuButton.onClick.AddListener(() => GameManager.Instance.BackToMenu());
        }

        // 마지막 스테이지면 다음 스테이지가 없으므로 버튼을 숨긴다.
        protected override void OnShow()
        {
            if (nextButton != null) nextButton.gameObject.SetActive(GetNextStageScene() != null);
        }

        private static string GetNextStageScene()
        {
            var database = Resources.Load<StageDatabase>("StageDatabase");
            if (database == null) return null;
            return database.GetNextStageScene(SceneManager.GetActiveScene().name);
        }

        private void LoadNextStage()
        {
            var next = GetNextStageScene();
            if (next == null || SceneLoader.Instance == null) return;
            SceneLoader.Instance.Load(next);
        }

        // 에디터 툴링에서 배선할 때 사용.
        public void SetButtons(Button next, Button restart, Button menu)
        {
            nextButton = next;
            restartButton = restart;
            menuButton = menu;
        }
    }
}
