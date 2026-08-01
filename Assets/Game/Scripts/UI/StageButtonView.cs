using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    // 스테이지 버튼의 상태 (UI 기획서 §2.4)
    public enum StageButtonState { Cleared, Playable, Locked }

    // 스테이지 선택 버튼 하나의 표시를 담당한다.
    // 번호와 상태를 각각의 텍스트에 넣고, 상태별 문구와 색상은 인스펙터에서 조정한다.
    public class StageButtonView : MonoBehaviour
    {
        [SerializeField] private TMP_Text numberText;
        [SerializeField] private TMP_Text statusText;
        [Tooltip("잠김 상태에만 표시할 자물쇠 아이콘 (비워둬도 동작한다)")]
        [SerializeField] private GameObject lockIcon;

        [Header("클리어")]
        [SerializeField] private string clearedText = "클리어";
        [SerializeField] private Color clearedColor = new(0.30f, 0.69f, 0.31f);

        [Header("미클리어")]
        [SerializeField] private string unclearedText = "미클리어";
        [SerializeField] private Color unclearedColor = new(0.62f, 0.62f, 0.62f);

        [Header("잠김")]
        [SerializeField] private string lockedText = "잠김";
        [SerializeField] private Color lockedColor = new(0.45f, 0.45f, 0.45f);

        public void SetDisplay(int stageNumber, StageButtonState state)
        {
            if (numberText != null) numberText.text = stageNumber.ToString();

            if (statusText != null)
            {
                statusText.text = state switch
                {
                    StageButtonState.Cleared => clearedText,
                    StageButtonState.Playable => unclearedText,
                    _ => lockedText
                };
                statusText.color = state switch
                {
                    StageButtonState.Cleared => clearedColor,
                    StageButtonState.Playable => unclearedColor,
                    _ => lockedColor
                };
            }

            if (lockIcon != null) lockIcon.SetActive(state == StageButtonState.Locked);

            // 잠긴 버튼은 눌리긴 해야 안내 팝업을 띄울 수 있으므로 interactable을 끄지 않는다.
            // 대신 Hover 색 변화를 없애 "선택 불가"임을 알린다 (UI 기획서 §2.4).
            if (TryGetComponent<Button>(out var button))
            {
                var colors = button.colors;
                colors.highlightedColor = state == StageButtonState.Locked
                    ? colors.normalColor
                    : Color.white;
                colors.selectedColor = colors.highlightedColor;
                button.colors = colors;
            }
        }

        // 에디터 툴링에서 배선할 때 사용.
        public void SetReferences(TMP_Text number, TMP_Text status)
        {
            numberText = number;
            statusText = status;
        }
    }
}
