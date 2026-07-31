using Core.Events;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Core.UI
{
    public class UIManager : Singleton<UIManager>
    {
        [SerializeField] private IntEventChannel onGameStateChanged;
        [SerializeField] private MainMenuScreen mainMenu;
        [SerializeField] private PauseScreen pause;
        [SerializeField] private ResultScreen result;
        [SerializeField] private ScoreHud hud;

        private void OnEnable()
        {
            if (onGameStateChanged != null) onGameStateChanged.OnRaised += HandleStateChanged;
        }

        private void OnDisable()
        {
            if (onGameStateChanged != null) onGameStateChanged.OnRaised -= HandleStateChanged;
        }

        private void Start() => HandleStateChanged((int)GameManager.Instance.State);

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                GameManager.Instance.TogglePause();
        }

        private void HandleStateChanged(int stateValue)
        {
            var state = (GameState)stateValue;
            switch (state)
            {
                case GameState.Ready:
                    mainMenu.Show(); pause.Hide(); result.Hide(); hud.Hide();
                    break;
                case GameState.Playing:
                    mainMenu.Hide(); pause.Hide(); result.Hide(); hud.Show();
                    break;
                case GameState.Paused:
                    pause.Show();
                    break;
                case GameState.GameOver:
                    pause.Hide(); hud.Hide(); result.Show();
                    break;
            }
        }
    }
}
