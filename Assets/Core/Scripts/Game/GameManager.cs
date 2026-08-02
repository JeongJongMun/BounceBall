using Core.Events;
using Core.UI;
using UnityEngine;

namespace Core
{
    // 값이 onGameStateChanged로 int 직렬화되어 나가므로 새 상태는 항상 뒤에 추가한다.
    public enum GameState { Ready, Playing, Paused, GameOver, Cleared }

    public class GameManager : Singleton<GameManager>
    {
        [SerializeField] private IntEventChannel onGameStateChanged;
        [SerializeField] private IntEventChannel onScoreChanged;
        [Tooltip("설정 시 BackToMenu가 이 씬을 로드한다. 비우면 현재 씬 리로드 (단일 씬 게임)")]
        [SerializeField] private string menuSceneName = "";

        public GameState State { get; private set; } = GameState.Ready;
        public int Score { get; private set; }
        public int HighScore => SaveData.HighScore;

        // 스테이지 씬에 막 들어온 시점. 이전 상태에서 떠 있던 화면(클리어·결과)을 정리한다.
        // 시작 연출이 있으면 Playing이 될 때까지 시간이 걸리므로, 그동안 이전 화면이 남지 않도록 한다.
        public void EnterStage()
        {
            Time.timeScale = 1f;
            if (State != GameState.Ready) SetState(GameState.Ready);
        }

        public void StartGame()
        {
            Time.timeScale = 1f;
            Score = 0;
            onScoreChanged?.Raise(0);
            SetState(GameState.Playing);
        }

        public void Pause()
        {
            if (IsSceneLoading) return;
            if (State != GameState.Playing) return;
            Time.timeScale = 0f;
            SetState(GameState.Paused);
        }

        public void Resume()
        {
            if (IsSceneLoading) return;
            if (State != GameState.Paused) return;
            Time.timeScale = 1f;
            SetState(GameState.Playing);
        }

        public void TogglePause()
        {
            if (IsSceneLoading) return;
            if (State == GameState.Playing) Pause();
            else if (State == GameState.Paused) Resume();
        }

        private static bool IsSceneLoading =>
            SceneLoader.Instance != null && SceneLoader.Instance.IsLoading;

        public void GameOver()
        {
            if (State != GameState.Playing && State != GameState.Paused) return;
            Time.timeScale = 1f;
            if (Score > SaveData.HighScore) SaveData.HighScore = Score;
            SetState(GameState.GameOver);
        }

        // 스테이지 클리어. 플레이어·물리를 멈춰 공중에 띄운 상태로 두고, 클리어 UI를 띄운다.
        // GameOver와 달리 실패가 아니므로 하이스코어를 저장하지 않는다.
        public void StageClear()
        {
            if (State != GameState.Playing) return;
            Time.timeScale = 0f;
            SetState(GameState.Cleared);
        }

        public void RestartGame()
        {
            SceneLoader.Instance.Reload();
            StartGame();
        }

        public void BackToMenu()
        {
            Score = 0;

            if (SceneLoader.Instance == null)
            {
                Time.timeScale = 1f;
                SetState(GameState.Ready);
                return;
            }

            if (UIManager.Instance != null)
                UIManager.Instance.FadeOutForSceneTransition(SceneLoader.Instance.FadeDuration);

            void AfterFadeIn() => SetState(GameState.Ready);
            if (string.IsNullOrEmpty(menuSceneName)) SceneLoader.Instance.Reload(AfterFadeIn);
            else SceneLoader.Instance.Load(menuSceneName, AfterFadeIn);
        }

        public void AddScore(int amount)
        {
            if (State != GameState.Playing) return;
            Score += amount;
            onScoreChanged?.Raise(Score);
        }

        private void SetState(GameState state)
        {
            State = state;
            onGameStateChanged?.Raise((int)state);
        }
    }
}
