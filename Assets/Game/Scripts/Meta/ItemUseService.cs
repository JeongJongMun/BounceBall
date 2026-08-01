using Core;
using UnityEngine;

namespace Game
{
    // 소비형 아이템 사용의 단일 진입점 (인벤토리 문서 §5.4).
    // 인벤토리 더블 클릭과 퀵슬롯 숫자키가 모두 여기를 호출한다.
    // 성공·실패 안내(토스트·사운드)도 여기서 한 번만 처리해 호출부마다 어긋나지 않게 한다.
    public static class ItemUseService
    {
        // 사용에 실패하면 수량을 차감하지 않는다 (문서 §5.4).
        // 결과 안내까지 포함하므로 호출부는 반환값을 무시해도 된다.
        public static ItemUseResult TryUse(string itemId)
        {
            var result = Use(itemId);
            Report(result, itemId);
            return result;
        }

        private static ItemUseResult Use(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return ItemUseResult.Failed;

            // 1. 인게임 상태인가 (스테이지 선택 화면에서는 사용할 수 없다)
            var manager = GameManager.Instance;
            if (manager == null || manager.State != GameState.Playing) return ItemUseResult.NotInGame;

            // 2. 소비형이고 인게임에서 쓸 수 있는 아이템인가
            var database = ItemDatabase.Load();
            var item = database != null ? database.Find(itemId) : null;
            if (item == null || !item.IsUsableInGame) return ItemUseResult.NotUsable;

            // 3. 보유 수량이 1개 이상인가.
            //    수량이 0이면 인벤토리에서 항목이 사라지고 퀵슬롯도 자동으로 비워지므로
            //    정상 경로로는 여기에 닿지 않는다. 안내 없이 막기만 한다.
            if (!Inventory.Has(itemId)) return ItemUseResult.Failed;

            // 4. 플레이어가 사용할 수 있는 상태인가
            var player = Object.FindAnyObjectByType<Player>();
            if (player == null || player.State == PlayerState.Disabled) return ItemUseResult.PlayerBusy;

            // 5. 클리어 처리 중이 아닌가
            var stage = Object.FindAnyObjectByType<StageController>();
            if (stage != null && stage.IsStageCleared) return ItemUseResult.StageCleared;

            // 6. 효과 적용 → 성공했을 때만 수량 차감
            if (!ApplyEffect(item, player)) return ItemUseResult.Failed;
            if (!Inventory.TryConsume(itemId)) return ItemUseResult.Failed;

            return ItemUseResult.Success;
        }

        // 성공하면 사용 안내를, 실패하면 사유에 맞는 안내와 알림음을 낸다.
        private static void Report(ItemUseResult result, string itemId)
        {
            if (result == ItemUseResult.Success)
            {
                var database = ItemDatabase.Load();
                var item = database != null ? database.Find(itemId) : null;
                if (item != null) ToastManager.Show($"{KoreanParticle.WithObject(item.ItemName)} 사용하였습니다.");
                return;
            }

            Sound.Play(SoundId.UI_Error); // 사용 불가 알림음 (사운드 기획: UI_Error)

            var message = MessageFor(result);
            if (!string.IsNullOrEmpty(message)) ToastManager.Show(message);
        }

        // 안내할 문구. 잠깐 지나가는 상태(사망 연출·클리어 처리)는 빈 문자열로 두어 조용히 넘어간다.
        public static string MessageFor(ItemUseResult result)
        {
            switch (result)
            {
                case ItemUseResult.NotInGame: return "인게임에서만 사용할 수 있습니다.";
                case ItemUseResult.NotUsable: return "이 아이템은 사용할 수 없습니다.";
                default: return string.Empty;
            }
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
