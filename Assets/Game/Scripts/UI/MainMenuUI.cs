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
        [SerializeField] private Canvas settingsCanvas;

        private void Awake()
        {
            if (startButton)
            {
                startButton.onClick.AddListener(() =>
                {
                    stageCanvas.gameObject.SetActive(true);
                    settingsCanvas.gameObject.SetActive(false);
                    gameObject.SetActive(false);
                });
            }

            if (settingsButton)
            {
                settingsButton.onClick.AddListener(() =>
                {
                    stageCanvas.gameObject.SetActive(false);
                    settingsCanvas.gameObject.SetActive(true);
                    gameObject.SetActive(false);
                });
            }
            
            if (quitButton != null)
            {
                quitButton.onClick.AddListener(QuitGame);
            }
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
