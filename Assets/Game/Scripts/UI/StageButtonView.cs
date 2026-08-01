using TMPro;
using UnityEngine;

namespace Game
{
    // 스테이지 선택 버튼 하나의 표시를 담당한다.
    // 번호와 클리어 상태를 각각의 텍스트에 넣고, 상태 문구와 색상은 인스펙터에서 조정한다.
    public class StageButtonView : MonoBehaviour
    {
        [SerializeField] private TMP_Text numberText;
        [SerializeField] private TMP_Text statusText;

        [Header("클리어")]
        [SerializeField] private string clearedText = "클리어";
        [SerializeField] private Color clearedColor = new(0.30f, 0.69f, 0.31f);

        [Header("미클리어")]
        [SerializeField] private string unclearedText = "미클리어";
        [SerializeField] private Color unclearedColor = new(0.62f, 0.62f, 0.62f);

        public void SetDisplay(int stageNumber, bool cleared)
        {
            if (numberText != null) numberText.text = stageNumber.ToString();

            if (statusText != null)
            {
                statusText.text = cleared ? clearedText : unclearedText;
                statusText.color = cleared ? clearedColor : unclearedColor;
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
