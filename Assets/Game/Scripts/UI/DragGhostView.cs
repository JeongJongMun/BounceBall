using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    // 드래그하는 동안 커서를 따라다니는 반투명 아이콘.
    // 무엇을 끌고 있는지 보이지 않으면 등록이 어려워서 둔다.
    public class DragGhostView : MonoBehaviour
    {
        public static DragGhostView Instance { get; private set; }

        [SerializeField] private RectTransform rect;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text label;

        private void Awake()
        {
            Instance = this;
            if (rect == null) rect = (RectTransform)transform;
            gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Show(ItemData item, Vector2 screenPosition)
        {
            if (item == null) return;

            if (iconImage != null)
            {
                iconImage.sprite = item.Thumbnail;
                iconImage.enabled = iconImage.sprite != null;
            }
            if (label != null) label.text = item.ItemName;

            gameObject.SetActive(true);
            transform.SetAsLastSibling(); // 항상 맨 위에
            Move(screenPosition);
        }

        // Screen Space Overlay 캔버스라 화면 좌표를 그대로 쓸 수 있다.
        public void Move(Vector2 screenPosition) => rect.position = screenPosition;

        public void Hide() => gameObject.SetActive(false);

        // 에디터 툴링에서 배선할 때 사용.
        public void SetReferences(RectTransform ghostRect, Image icon, TMP_Text text)
        {
            rect = ghostRect;
            iconImage = icon;
            label = text;
        }
    }
}
