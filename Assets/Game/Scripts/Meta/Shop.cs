using UnityEngine;

namespace Game
{
    // 상점 구매 처리 (인벤토리 문서 §7.6). UI와 분리해 규칙만 담는다.
    public static class Shop
    {
        public static int TotalPrice(ItemData item, int quantity)
        {
            if (item == null) return 0;
            return item.Price * Mathf.Max(0, quantity);
        }

        public static bool CanAfford(ItemData item, int quantity)
        {
            return quantity > 0 && CurrencyWallet.Coin >= TotalPrice(item, quantity);
        }

        // 코인이 부족하면 차감도, 인벤토리 추가도 하지 않는다 (문서 §7.6)
        public static bool TryPurchase(ItemData item, int quantity)
        {
            if (item == null || quantity <= 0) return false;
            if (!CurrencyWallet.TrySpend(TotalPrice(item, quantity))) return false;

            Inventory.Add(item.ItemId, quantity);
            return true;
        }
    }
}
