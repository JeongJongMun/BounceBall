using UnityEngine;

namespace Game
{
    // 사망 발판 접촉 판정 (기믹 문서 §4, §6). 착지뿐 아니라 벽·천장 접촉에서도 발생한다.
    // 사망 발판은 두 종류다 — 타일맵에 칠한 사망 타일(SpecialTile)과,
    // 프리팹 기믹의 사망 콜라이더(HazardSurface). 판정 규칙(면제·등면 안전)은 같다.
    // 실제 사망 처리(입력 제한 → 체크포인트 복구 → 재개)는 StageController가 낙사와 공유한다.
    [RequireComponent(typeof(Player))]
    public class PlayerHazardContact : MonoBehaviour
    {
        private Player _player;
        private StageController _stage;

        private Player PlayerRef => _player != null ? _player : (_player = GetComponent<Player>());

        // 플레이어가 런타임에 스폰되므로 인스펙터 배선이 불가능하다 — 지연 해석한다.
        private StageController StageRef => _stage != null ? _stage : (_stage = FindAnyObjectByType<StageController>());

        private void OnCollisionEnter2D(Collision2D collision) => CheckHazard(collision);

        // 발판 위에 얹힌 채로 성질이 바뀌는 경우까지 잡는다 (기믹 문서 §6).
        private void OnCollisionStay2D(Collision2D collision) => CheckHazard(collision);

        private void CheckHazard(Collision2D collision)
        {
            // 이미 사망 연출 중이면 중복 판정하지 않는다 (기믹 문서 §4.2)
            if (PlayerRef.State == PlayerState.Disabled) return;

            // 프리팹 기믹의 사망 콜라이더 — 닿은 콜라이더 자체가 표식을 갖는다.
            // 같은 프리팹의 몸통(일반 콜라이더)은 표식이 없어 별개의 충돌로 들어온다.
            var hazard = collision.collider.GetComponent<HazardSurface>();

            for (int i = 0; i < collision.contactCount; i++)
            {
                var contact = collision.GetContact(i);

                if (hazard != null && hazard.IsLethalOnContact(PlayerRef.PropertyType, contact.normal))
                {
                    Kill(contact.normal);
                    return;
                }

                // 타일맵 사망 타일 — 가시 방향(셀 회전 반영)까지 받아 등면 접촉만 안전 처리한다
                if (!StageTiles.TryGetSpecialTileAt(contact.point, contact.normal, out var tile, out var spikeDirection)) continue;
                if (!tile.IsLethalOnContact(PlayerRef.PropertyType, contact.normal, spikeDirection)) continue;

                Kill(contact.normal);
                return;
            }
        }

        private void Kill(Vector2 contactNormal)
        {
            // 실드가 있으면 사망을 1회 무효화한다 (상점 소비형 문서 §5.6).
            // 원래 안전한 조합은 호출 전에 걸러지므로 실드가 소모되지 않는다 (§5.4).
            var shield = GetComponent<PlayerShield>();
            if (shield != null && shield.TryAbsorbLethalHit(contactNormal)) return;

            StageRef?.RespawnPlayer();
        }
    }
}
