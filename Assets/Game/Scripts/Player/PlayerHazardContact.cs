using UnityEngine;

namespace Game
{
    // 사망 발판 접촉 판정 (기믹 문서 §4, §6). 착지뿐 아니라 벽·천장 접촉에서도 발생한다.
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

            for (int i = 0; i < collision.contactCount; i++)
            {
                var contact = collision.GetContact(i);
                var tile = StageTiles.GetSpecialTileAt(contact.point, contact.normal);
                if (tile == null || !tile.IsLethalOnContact(PlayerRef.PropertyType, contact.normal)) continue;

                // 실드가 있으면 사망을 1회 무효화한다 (상점 소비형 문서 §5.6).
                // 원래 안전한 조합은 위의 IsLethalOnContact에서 걸러지므로 실드가 소모되지 않는다 (§5.4).
                var shield = GetComponent<PlayerShield>();
                if (shield != null && shield.TryAbsorbLethalHit(contact.normal)) return;

                StageRef?.RespawnPlayer();
                return;
            }
        }
    }
}
