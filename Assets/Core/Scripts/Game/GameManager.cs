using Core.Events;
using UnityEngine;

namespace Core
{
    public enum GameState { Ready, Playing, Paused, GameOver }

    public class GameManager : Singleton<GameManager>
    {
        [SerializeField] private IntEventChannel onGameStateChanged;
        [SerializeField] private IntEventChannel onScoreChanged;
        [Tooltip("설정 시 BackToMenu가 이 씬을 로드한다. 비우면 현재 씬 리로드 (단일 씬 게임)")]
        [SerializeField] private string menuSceneName = "";

        public GameState State { get; private set; } = GameState.Ready;
        public int Score { get; private set; }
        public int HighScore => SaveData.HighScore;

        public void StartGame()
        {
            Time.timeScale = 1f;
            Score = 0;
            onScoreChanged?.Raise(0);
            SetState(GameState.Playing);
        }

        public void Pause()
        {
            if (State != GameState.Playing) return;
            Time.timeScale = 0f;
            SetState(GameState.Paused);
        }

        public void Resume()
        {
            if (State != GameState.Paused) return;
            Time.timeScale = 1f;
            SetState(GameState.Playing);
        }

        public void TogglePause()
        {
            if (State == GameState.Playing) Pause();
            else if (State == GameState.Paused) Resume();
        }

        public void GameOver()
        {
            if (State != GameState.Playing && State != GameState.Paused) return;
            Time.timeScale = 1f;
            if (Score > SaveData.HighScore) SaveData.HighScore = Score;
            SetState(GameState.GameOver);
        }

        public void RestartGame()
        {
            SceneLoader.Instance.Reload();
            StartGame();
        }

        public void BackToMenu()
        {
            Time.timeScale = 1f;
            Score = 0;
            SetState(GameState.Ready);

            if (SceneLoader.Instance == null) return;
            if (string.IsNullOrEmpty(menuSceneName)) SceneLoader.Instance.Reload();
            else SceneLoader.Instance.Load(menuSceneName);
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
