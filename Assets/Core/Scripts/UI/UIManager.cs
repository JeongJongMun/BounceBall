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
        // hud/clear는 base 타입 — 게임별로 자체 화면을 꽂을 수 있게 한다 (Core가 게임 레이어를 참조하지 않도록).
        [SerializeField] private UIScreen hud;
        [SerializeField] private UIScreen clear;

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

        public void FadeOutForSceneTransition(float sceneFadeDuration)
        {
            var duration = sceneFadeDuration * 1.8f;
            if (pause != null && pause.gameObject.activeSelf) pause.FadeOut(duration);
            if (result != null && result.gameObject.activeSelf) result.FadeOut(duration);
            if (clear != null && clear.gameObject.activeSelf) clear.FadeOut(duration);
        }

        // 화면 참조는 null 허용 — 전용 메뉴 씬을 쓰는 게임은 mainMenu 없이 구성한다.
        private void HandleStateChanged(int stateValue)
        {
            var state = (GameState)stateValue;
            switch (state)
            {
                case GameState.Ready:
                    if (mainMenu != null) mainMenu.Show();
                    if (pause != null) pause.HideImmediate();
                    if (result != null) result.HideImmediate();
                    if (clear != null) clear.HideImmediate();
                    if (hud != null) hud.HideImmediate();
                    break;
                case GameState.Playing:
                    if (mainMenu != null) mainMenu.Hide();
                    if (pause != null) pause.Hide();
                    if (result != null) result.Hide();
                    if (clear != null) clear.Hide();
                    // pause 중에는 HUD를 숨기지 않으므로, 이미 떠 있으면 Show()로 팝 애니를 다시 돌리지 않는다.
                    if (hud != null && !hud.gameObject.activeSelf) hud.Show();
                    break;
                case GameState.Paused:
                    if (pause != null) pause.Show();
                    break;
                case GameState.GameOver:
                    if (pause != null) pause.Hide();
                    if (hud != null) hud.Hide();
                    if (result != null) result.Show();
                    break;
                case GameState.Cleared:
                    if (pause != null) pause.Hide();
                    if (hud != null) hud.Hide();
                    if (clear != null) clear.Show();
                    break;
            }
        }
    }
}
