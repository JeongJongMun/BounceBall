using UnityEngine;

namespace Game
{
    // 사용 즉시 실드를 활성화한다 (상점 소비형 문서 §5). 값은 문서 §5.9.
    [CreateAssetMenu(menuName = "Game/Item Effect/1회성 실드", fileName = "Effect_ShieldOneHit")]
    public class ShieldEffect : ItemEffect
    {
        [Label("실드 외형")]
        [Tooltip("활성 중 캐릭터에 씌워지는 이미지")]
        [SerializeField] private Sprite visualSprite;

        [Label("외형 정렬 순서")]
        [Tooltip("캐릭터보다 앞에 그려지도록 여유 있게 둔다 (캐릭터 스파인 20, 혀 25)")]
        [SerializeField] private int visualSortingOrder = 30;

        [Label("실드 지름")]
        [Tooltip("월드 유닛 기준 실드 크기. 원본 이미지 크기와 무관하게 이 지름으로 맞춘다. 캐릭터(지름 약 1)보다 살짝 크게")]
        [SerializeField] private float visualDiameter = 1.4f;

        [Label("외형 오프셋")]
        [Tooltip("플레이어 중심 기준 외형 위치(월드 유닛). 머리가 튀어나오면 y를 올린다")]
        [SerializeField] private Vector2 visualOffset = new(0f, 0.2f);

        [Label("밀어내기 수평 반발력")]
        [Tooltip("방어 순간 접촉면 반대 방향으로 밀리는 속도 (문서 §5.7)")]
        [SerializeField] private float knockbackHorizontal = 6f;

        [Label("밀어내기 수직 반발력")]
        [Tooltip("방어 순간 위로 튀는 속도 (문서 §5.7)")]
        [SerializeField] private float knockbackVertical = 9f;

        [Label("사망 판정 무시 시간")]
        [Tooltip("방어 직후 같은 발판과의 연속 사망을 막는 시간(초). 0.3~0.7 권장 (문서 §5.7)")]
        [SerializeField] private float invulnerabilityTime = 0.5f;

        public override bool TryApply(Player player)
        {
            var shield = player.GetComponent<PlayerShield>();
            if (shield == null) shield = player.gameObject.AddComponent<PlayerShield>();

            // 이미 활성화돼 있으면 false — 수량이 차감되지 않는다 (문서 §5.2)
            return shield.TryActivate(visualSprite, knockbackHorizontal, knockbackVertical,
                invulnerabilityTime, visualSortingOrder, visualDiameter, visualOffset);
        }
    }
}
