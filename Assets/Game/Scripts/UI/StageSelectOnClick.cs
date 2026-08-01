using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    // 메뉴로 돌아가는 버튼에 함께 붙인다.
    // 메뉴 씬이 메인 화면 대신 스테이지 선택 화면으로 열리도록 요청만 남긴다
    // (실제 씬 이동은 Core의 PauseScreen/GameManager가 처리한다).
    [RequireComponent(typeof(Button))]
    public class StageSelectOnClick : MonoBehaviour
    {
        private void Start()
        {
            GetComponent<Button>().onClick.AddListener(MenuNavigation.RequestStageSelect);
        }
    }
}
