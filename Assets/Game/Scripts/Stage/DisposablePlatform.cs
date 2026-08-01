using System.Collections;
using UnityEngine;

namespace Game
{
    // 일회성 발판 (기믹 문서 §3.1). 상단에 착지하면 사라지고, 설정된 시간이 지나면 돌아온다.
    // 성질과 무관하게 모든 플레이어에게 동일하게 작동한다.
    [RequireComponent(typeof(Collider2D))]
    public class DisposablePlatform : MonoBehaviour
    {
        public enum PlatformState { Active, Disabled, RespawnWaiting }

        [Tooltip("밟은 뒤 사라지기까지의 지연 시간")]
        [SerializeField] private float disableDelay = 0.3f;
        [Tooltip("재생성까지 걸리는 시간. -1이면 스테이지가 초기화되기 전까지 돌아오지 않는다")]
        [SerializeField] private float respawnTime = 3f;
        [Tooltip("낙사 부활·스테이지 재시작·재진입 시 초기 활성 상태로 복구할지")]
        [SerializeField] private bool resetOnStageRestart = true;

        private Collider2D _collider;
        private SpriteRenderer _renderer;

        // Awake 타이밍에 의존하지 않도록 지연 해석한다 (EditMode 테스트에서는 Awake가 돌지 않는다).
        private Collider2D ColliderRef => _collider != null ? _collider : (_collider = GetComponent<Collider2D>());
        private SpriteRenderer RendererRef => _renderer != null ? _renderer : (_renderer = GetComponent<SpriteRenderer>());

        public PlatformState State { get; private set; } = PlatformState.Active;
        public bool ResetOnStageRestart => resetOnStageRestart;

        // 에디터 툴링/테스트에서 수치를 지정할 때 사용.
        public void SetData(float delay, float respawn, bool resetOnRestart = true)
        {
            disableDelay = delay;
            respawnTime = respawn;
            resetOnStageRestart = resetOnRestart;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (State != PlatformState.Active) return;
            if (collision.collider.GetComponent<Player>() == null) return;
            if (!IsTopContact(collision)) return; // 벽·하단 접촉에서는 작동하지 않는다 (기믹 문서 §3.1)

            Trigger();
        }

        // 접촉 지점이 발판 윗면에 있는지. 노멀 방향은 어느 콜라이더 기준인지 헷갈리므로 위치로 본다.
        private bool IsTopContact(Collision2D collision)
        {
            float top = ColliderRef.bounds.max.y;
            for (int i = 0; i < collision.contactCount; i++)
            {
                if (collision.GetContact(i).point.y >= top - 0.1f) return true;
            }
            return false;
        }

        // 테스트 및 외부 제어용. 착지 판정을 거치지 않고 곧바로 작동시킨다.
        public void Trigger()
        {
            if (State != PlatformState.Active) return;
            StartCoroutine(DisableRoutine());
        }

        private IEnumerator DisableRoutine()
        {
            if (disableDelay > 0f) yield return new WaitForSeconds(disableDelay);

            SetPresent(false);

            // RespawnTime이 음수면 재생성하지 않는다 (기믹 문서 §3.1 재생성 시간 규칙)
            if (respawnTime < 0f)
            {
                State = PlatformState.Disabled;
                yield break;
            }

            State = PlatformState.RespawnWaiting;
            yield return new WaitForSeconds(respawnTime);

            SetPresent(true);
            State = PlatformState.Active;
        }

        // 초기 활성 상태로 복구한다 (기믹 문서 §3.1).
        // 스테이지 재시작·재진입뿐 아니라 낙사 부활에서도 호출한다 — StageController가 부른다.
        public void ResetPlatform()
        {
            if (!resetOnStageRestart) return;

            StopAllCoroutines();
            SetPresent(true);
            State = PlatformState.Active;
        }

        // SetActive가 아니라 컴포넌트만 끈다 (아이템들과 동일 — 코루틴/참조를 살려둔다)
        private void SetPresent(bool present)
        {
            if (ColliderRef != null) ColliderRef.enabled = present;
            if (RendererRef != null) RendererRef.enabled = present;
        }
    }
}
