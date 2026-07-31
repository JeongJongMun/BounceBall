using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Core.UI
{
    public class ResultScreen : UIScreen
    {
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private TMP_Text highScoreText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button menuButton;

        private void Awake()
        {
            restartButton.onClick.AddListener(() => GameManager.Instance.RestartGame());
            menuButton.onClick.AddListener(() =>
            {
                GameManager.Instance.BackToMenu();
                SceneLoader.Instance.Reload();
            });
        }

        protected override void OnShow()
        {
            scoreText.text = $"Score  {GameManager.Instance.Score}";
            highScoreText.text = $"Best  {GameManager.Instance.HighScore}";
        }
    }
}
