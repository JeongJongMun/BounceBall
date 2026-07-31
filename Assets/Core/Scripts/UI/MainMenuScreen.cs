using UnityEngine;
using UnityEngine.UI;

namespace Core.UI
{
    public class MainMenuScreen : UIScreen
    {
        [SerializeField] private Button playButton;
        [SerializeField] private Button quitButton;

        private void Awake()
        {
            playButton.onClick.AddListener(() => GameManager.Instance.StartGame());
            quitButton.onClick.AddListener(Application.Quit);
        }
    }
}
