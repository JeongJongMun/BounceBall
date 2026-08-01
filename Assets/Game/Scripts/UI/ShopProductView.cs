using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game
{
    // 상점 상품 목록의 한 줄. 클릭하면 우측 상세/구매 영역에 표시한다.
    public class ShopProductView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text priceText;
        [SerializeField] private Image selectionOutline;

        public ItemData Item { get; private set; }

        public event Action<ShopProductView> Clicked;

        public void Bind(ItemData item)
        {
            Item = item;

            if (iconImage != null)
            {
                iconImage.sprite = item != null ? item.Thumbnail : null;
                iconImage.enabled = iconImage.sprite != null;
            }
            if (nameText != null) nameText.text = item != null ? item.ItemName : "";
            if (priceText != null) priceText.text = item != null ? item.Price.ToString() : "";
            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            if (selectionOutline != null) selectionOutline.enabled = selected;
        }

        public void OnPointerClick(PointerEventData eventData) => Clicked?.Invoke(this);

        // 에디터 툴링에서 배선할 때 사용.
        public void SetReferences(Image icon, TMP_Text itemName, TMP_Text price, Image outline)
        {
            iconImage = icon;
            nameText = itemName;
            priceText = price;
            selectionOutline = outline;
        }
    }
}
