using Core;
using UnityEngine;

namespace Game
{
    // 소비형 아이템 사용의 단일 진입점 (인벤토리 문서 §5.4).
    // 인벤토리 더블 클릭과 퀵슬롯 숫자키가 모두 여기를 호출한다.
    public static class ItemUseService
    {
        // 사용에 실패하면 수량을 차감하지 않는다 (문서 §5.4).
        public static bool TryUse(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return false;

            // 1. 인게임 상태인가 (스테이지 선택 화면에서는 사용할 수 없다)
            var manager = GameManager.Instance;
            if (manager == null || manager.State != GameState.Playing) return false;

            // 2. 소비형이고 인게임에서 쓸 수 있는 아이템인가
            var database = ItemDatabase.Load();
            var item = database != null ? database.Find(itemId) : null;
            if (item == null || !item.IsUsableInGame) return false;

            // 3. 보유 수량이 1개 이상인가
            if (!Inventory.Has(itemId)) return false;

            // 4. 플레이어가 사용할 수 있는 상태인가
            var player = Object.FindAnyObjectByType<Player>();
            if (player == null || player.State == PlayerState.Disabled) return false;

            // 5. 클리어 처리 중이 아닌가
            var stage = Object.FindAnyObjectByType<StageController>();
            if (stage != null && stage.IsStageCleared) return false;

            // 6. 효과 적용 → 성공했을 때만 수량 차감
            if (!ApplyEffect(item, player)) return false;
            if (!Inventory.TryConsume(itemId)) return false;

            ToastManager.Show($"{KoreanParticle.WithObject(item.ItemName)} 사용하였습니다.");
            return true;
        }

        private static bool ApplyEffect(ItemData item, Player player)
        {
            if (item.Category != ItemCategory.PropertyConsumable) return false;
            if (item.GrantedProperty == null) return false;

            var playerProperty = player.GetComponent<PlayerProperty>();
            if (playerProperty == null) return false;

            player.GetComponent<PlayerSpineView>()?.PlayEat();
            playerProperty.Apply(item.GrantedProperty);
            Sound.Play(SoundId.Property_Change);
            return true;
        }
    }
}
