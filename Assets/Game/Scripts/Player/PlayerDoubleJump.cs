using UnityEngine;

namespace Game
{
    // 더블 점프 아이템의 실제 동작 (상점 소비형 문서 §3).
    // 지상·공중·부착·미끄러짐 어디서든 수직 속도를 끊고 위로 쏘아 올린다.
    // 효과 에셋(DoubleJumpEffect)이 필요할 때 붙여 주므로 프리팹에 직접 배치하지 않는다.
    [RequireComponent(typeof(Player))]
    public class PlayerDoubleJump : MonoBehaviour
    {
        private Player _player;
        private float _lastUseTime = float.NegativeInfinity;

        private Player PlayerRef => _player != null ? _player : (_player = GetComponent<Player>());

        // 문서 §3.3 순서: 상태 확인 → 방해 상태 해제 → 수직 속도 초기화 → 점프력 적용.
        public bool TryJump(float force, float useInterval, bool preserveHorizontalVelocity)
        {
            // 한 번의 입력으로 여러 개가 소모되지 않도록 최소 간격을 둔다 (문서 §3.8)
            if (!CanUse(_lastUseTime, Time.time, useInterval)) return false;

            var player = PlayerRef;
            if (player.State == PlayerState.Disabled) return false;

            // 젤리 부착 중이면 해제하고 점프한다 (문서 §3.5).
            // 재부착 방지 시간은 Release()가 스스로 적용한다.
            GetComponent<PlayerJellyAttach>()?.Release();

            // 얼음 미끄러짐은 유지한 채 수평 관성만 살린다 (문서 §3.6)

            var body = player.Body;
            float horizontal = preserveHorizontalVelocity ? body.linearVelocity.x : 0f;
            body.linearVelocity = new Vector2(horizontal, force);

            _lastUseTime = Time.time;
            GetComponent<PlayerSpineView>()?.PlayJump();
            return true;
        }

        // 중복 입력 방지 판정. 순수 함수로 분리해 테스트한다.
        public static bool CanUse(float lastUseTime, float now, float useInterval)
        {
            return now - lastUseTime >= useInterval;
        }
    }
}
