using System;
using UnityEngine;

namespace Game
{
    // 코인 보유량 (인벤토리 문서 §6). 스테이지가 아니라 플레이어 공용 데이터라 영구 저장한다 (§6.4).
    public static class CurrencyWallet
    {
        private const string CoinKey = "game.currency.coin";

        public static event Action<int> OnCoinChanged;

        public static int Coin
        {
            get => PlayerPrefs.GetInt(CoinKey, 0);
            private set
            {
                PlayerPrefs.SetInt(CoinKey, Mathf.Max(0, value));
                PlayerPrefs.Save();
                OnCoinChanged?.Invoke(Coin);
            }
        }

        public static void Add(int amount)
        {
            if (amount <= 0) return;
            Coin += amount;
        }

        // 잔액이 부족하면 차감하지 않고 false (문서 §6.3, §7.6)
        public static bool TrySpend(int amount)
        {
            if (amount <= 0 || Coin < amount) return false;
            Coin -= amount;
            return true;
        }

        // 체크포인트 복구용 — 저장 시점 잔액으로 되돌린다 (문서 §6.5)
        public static void RestoreTo(int amount) => Coin = amount;

        public static void ResetAll()
        {
            PlayerPrefs.DeleteKey(CoinKey);
            PlayerPrefs.Save();
            OnCoinChanged?.Invoke(Coin);
        }
    }
}
