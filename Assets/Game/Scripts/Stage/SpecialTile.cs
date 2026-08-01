using UnityEngine;
using UnityEngine.Tilemaps;

namespace Game
{
    // 성질을 가진 타일 (기획 §1.3). 플레이어 성질과의 조합으로 반응이 달라진다.
    // 이 컴포넌트가 없는 일반 타일은 TilePropertyType.Default로 간주한다.
    //
    // isDeadly를 켜면 접촉 시 사망하는 발판이 된다 (기믹 문서 §4). 사망 면제 성질은
    // 타일 자신의 성질에서 끌어온다 — 기획의 사망 발판 3종이 정확히 그 조합이다:
    //   기본 사망 발판(Default) → 면제 없음 / 젤리 사망 발판(Jelly) → 젤리만 생존 / 얼음 사망 발판(Ice) → 얼음만 생존
    [CreateAssetMenu(menuName = "Game/Special Tile", fileName = "NewSpecialTile")]
    public class SpecialTile : Tile
    {
        [SerializeField] private TilePropertyType tileProperty = TilePropertyType.Default;

        [Header("사망 발판 (기믹 문서 §4)")]
        [Tooltip("켜면 접촉 시 사망하는 발판이 된다")]
        [SerializeField] private bool isDeadly;
        [Tooltip("사망을 면제받은 성질에서 부착·미끄러짐을 그대로 적용할지 (기믹 문서 §4.3, §4.4)")]
        [SerializeField] private bool applySurfaceEffectWhenSafe = true;
        [Tooltip("아랫면 접촉(타일을 천장으로 받는 경우)에도 사망할지. 가시처럼 위·옆에만 돋은 타일은 끈다")]
        [SerializeField] private bool lethalFromBelow = true;

        // 아랫면 접촉으로 볼 노멀 기준. PlayerBounce의 하단 접촉 판정과 같은 값을 쓴다.
        private const float BelowContactThreshold = 0.5f;

        public TilePropertyType TileProperty => tileProperty;
        public bool IsDeadly => isDeadly;

        public void SetTileProperty(TilePropertyType value) => tileProperty = value;

        public void SetDeadly(bool deadly, bool applySurfaceEffect = true, bool fromBelow = true)
        {
            isDeadly = deadly;
            applySurfaceEffectWhenSafe = applySurfaceEffect;
            lethalFromBelow = fromBelow;
        }

        // 위험 판정을 면제받는 성질인가 (기믹 문서 §5 상호작용표).
        public bool IsSafeFor(PlayerPropertyType property)
        {
            switch (tileProperty)
            {
                case TilePropertyType.Jelly: return property == PlayerPropertyType.Jelly;
                case TilePropertyType.Ice: return property == PlayerPropertyType.Ice;
                default: return false; // 기본 사망 발판은 모든 성질에서 사망
            }
        }

        // 이 성질의 플레이어가 접촉하면 죽는가.
        public bool IsLethalTo(PlayerPropertyType property) => isDeadly && !IsSafeFor(property);

        // 접촉 방향까지 반영한 사망 판정. contactNormal은 타일에서 플레이어를 향하므로,
        // 아래를 향하면 플레이어가 타일 밑면을 천장으로 받은 것이다.
        public bool IsLethalOnContact(PlayerPropertyType property, Vector2 contactNormal)
            => IsLethalTo(property) && (lethalFromBelow || contactNormal.y > -BelowContactThreshold);

        // 부착·미끄러짐 같은 표면 효과를 적용할지. 사망 발판이 아니면 항상 적용한다.
        public bool AppliesSurfaceEffectFor(PlayerPropertyType property)
            => !isDeadly || (applySurfaceEffectWhenSafe && IsSafeFor(property));
    }
}
