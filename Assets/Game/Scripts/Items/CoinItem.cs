using Core.Events;
using UnityEngine;

namespace Game
{
    // 스테이지에 배치하는 코인 (인벤토리 문서 §6.2). 접촉 즉시 자동 획득한다.
    // 획득 상태는 체크포인트에 저장되며, 저장 이후 먹은 코인은 사망 시 되살아난다 (§6.5).
    [RequireComponent(typeof(Collider2D))]
    public class CoinItem : MonoBehaviour
    {
        [SerializeField] private int amount = 1;
        [SerializeField] private VoidEventChannel onCollected;

        private Collider2D _collider;
        private SpriteRenderer _renderer;
        private StageController _stage;

        // 팔레트 브러시가 Gimmicks 아래에 스폰하므로 인스펙터 배선이 불가능하다 — 런타임에 지연 해석한다.
        private StageController StageRef => _stage != null ? _stage : (_stage = FindAnyObjectByType<StageController>());
        private Collider2D ColliderRef => _collider != null ? _collider : (_collider = GetComponent<Collider2D>());
        private SpriteRenderer RendererRef => _renderer != null ? _renderer : (_renderer = GetComponent<SpriteRenderer>());

        public int Amount => amount;
        public bool IsCollected { get; private set; }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (IsCollected) return;
            if (other.GetComponent<Player>() == null) return;
            Collect();
        }

        public void Collect()
        {
            if (IsCollected) return;
            IsCollected = true;

            // SetActive가 아니라 컴포넌트만 끈다 (GoalItem과 동일 — 참조를 살려둔다)
            if (ColliderRef != null) ColliderRef.enabled = false;
            if (RendererRef != null) RendererRef.enabled = false;

            CurrencyWallet.Add(amount);
            Sound.Play(SoundId.Coin);
            onCollected?.Raise();

            var stage = StageRef;
            if (stage != null) stage.NotifyCoinCollected(amount);
        }

        // 체크포인트 저장 이후 획득한 코인을 되살린다 (문서 §6.5)
        public void Restore()
        {
            if (!IsCollected) return;
            IsCollected = false;

            if (ColliderRef != null) ColliderRef.enabled = true;
            if (RendererRef != null) RendererRef.enabled = true;
        }

        public void SetAmount(int value) => amount = value;
    }
}
