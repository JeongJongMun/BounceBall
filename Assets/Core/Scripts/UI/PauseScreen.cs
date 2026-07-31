using UnityEngine;
using UnityEngine.UI;

namespace Core.UI
{
    public class PauseScreen : UIScreen
    {
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button menuButton;

        private void Awake()
        {
            resumeButton.onClick.AddListener(() => GameManager.Instance.Resume());
            menuButton.onClick.AddListener(() => GameManager.Instance.BackToMenu());
        }
    }
}
