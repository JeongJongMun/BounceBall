using Core.Events;
using Core.UI;
using TMPro;
using UnityEngine;

namespace Game.UI
{
    // 목표 아이템 획득 수량 HUD (기획 §21.4). 점수 대신 "2 / 5" 형태로 표시한다.
    public class GoalHud : UIScreen
    {
        [SerializeField] private TMP_Text goalText;
        [SerializeField] private StringEventChannel onGoalProgressChanged;

        private void OnEnable()
        {
            if (onGoalProgressChanged != null) onGoalProgressChanged.OnRaised += UpdateText;

            // Hide()가 GameObject를 비활성화해 구독이 끊기므로, 다시 표시될 때 현재 값으로 맞춘다.
            var stage = FindAnyObjectByType<StageController>();
            if (stage != null) UpdateText(stage.GoalProgressText);
        }

        private void OnDisable()
        {
            if (onGoalProgressChanged != null) onGoalProgressChanged.OnRaised -= UpdateText;
        }

        private void UpdateText(string progress)
        {
            if (goalText != null) goalText.text = progress;
        }

        // 에디터 툴링에서 배선할 때 사용.
        public void SetReferences(TMP_Text text, StringEventChannel channel)
        {
            goalText = text;
            onGoalProgressChanged = channel;
        }
    }
}
