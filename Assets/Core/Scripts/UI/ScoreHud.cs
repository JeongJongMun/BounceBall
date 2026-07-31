using Core.Events;
using TMPro;
using UnityEngine;

namespace Core.UI
{
    public class ScoreHud : UIScreen
    {
        [SerializeField] private TMP_Text scoreText;
        [SerializeField] private IntEventChannel onScoreChanged;

        private void OnEnable()
        {
            if (onScoreChanged != null) onScoreChanged.OnRaised += UpdateScore;
            if (GameManager.Instance != null) UpdateScore(GameManager.Instance.Score);
        }

        private void OnDisable()
        {
            if (onScoreChanged != null) onScoreChanged.OnRaised -= UpdateScore;
        }

        private void UpdateScore(int score) => scoreText.text = score.ToString();
    }
}
