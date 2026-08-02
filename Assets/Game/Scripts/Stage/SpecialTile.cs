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
        [Tooltip("켜면 등면(가시 반대쪽)을 포함한 전방향에서 사망한다. 끄면 가시 반대면만 안전하다. " +
            "가시 방향은 팔레트 회전([ ] 키) 배치를 그대로 따른다")]
        [SerializeField] private bool lethalFromBelow = true;

        // 등면 접촉으로 볼 내적 기준. 가시 빗면(대각 법선)은 사망 쪽에 남도록 절반으로 둔다.
        private const float BackContactThreshold = 0.5f;

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

        // 회전 없는 배치용 축약형 — 가시가 원본 그대로 위를 향한다고 본다.
        public bool IsLethalOnContact(PlayerPropertyType property, Vector2 contactNormal)
            => IsLethalOnContact(property, contactNormal, Vector2.up);

        // 접촉 방향까지 반영한 사망 판정. spikeDirection은 셀 회전이 반영된 가시 방향이다
        // (원본 그림 기준 위쪽 — StageTiles.TryGetSpecialTileAt가 계산해 준다).
        // 가시 반대면(등면)에 닿았을 때만 안전하다: 천장 가시는 아래서 받으면 죽고 위를 밟으면 산다.
        public bool IsLethalOnContact(PlayerPropertyType property, Vector2 contactNormal, Vector2 spikeDirection)
            => IsLethalTo(property)
               && (lethalFromBelow || !IsBackContact(contactNormal, spikeDirection));

        // 접촉 법선은 타일에서 플레이어를 향하므로, 가시 방향과 반대면 등면에 닿은 것이다.
        public static bool IsBackContact(Vector2 contactNormal, Vector2 spikeDirection)
            => Vector2.Dot(contactNormal, spikeDirection) <= -BackContactThreshold;

        // 부착·미끄러짐 같은 표면 효과를 적용할지. 사망 발판이 아니면 항상 적용한다.
        public bool AppliesSurfaceEffectFor(PlayerPropertyType property)
            => !isDeadly || (applySurfaceEffectWhenSafe && IsSafeFor(property));
    }
}
