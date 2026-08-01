using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

namespace Game
{
    // 잠긴 스테이지를 눌렀을 때 표시하는 안내 팝업 (UI 기획서 §2.6).
    // 확인 버튼 하나만 제공하며, Esc로도 닫는다 (§10 Esc 우선순위: 확인 팝업이 최상위).
    public class LockedStagePopup : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Button confirmButton;

        [TextArea]
        [SerializeField] private string message = "이전 스테이지를 클리어해야\n플레이할 수 있습니다.";

        public bool IsOpen => root != null && root.activeSelf;

        private void Awake()
        {
            if (confirmButton != null) confirmButton.onClick.AddListener(Hide);
            if (messageText != null) messageText.text = message;
            Hide();
        }

        private void Update()
        {
            if (!IsOpen || Keyboard.current == null) return;
            if (Keyboard.current.escapeKey.wasPressedThisFrame) Hide();
        }

        public void Show()
        {
            if (messageText != null) messageText.text = message;
            // UI_Error는 잠긴 버튼을 누르는 순간 버튼 쪽에서 낸다 (StageUI).
            // 팝업이 뜨는 시점은 버튼을 뗀 뒤라 여기서 내면 늦게 들린다.
            if (root != null) root.SetActive(true);
        }

        public void Hide()
        {
            if (root != null) root.SetActive(false);
        }

        // 에디터 툴링에서 배선할 때 사용.
        public void SetReferences(GameObject popupRoot, TMP_Text text, Button confirm)
        {
            root = popupRoot;
            messageText = text;
            confirmButton = confirm;
        }
    }
}
