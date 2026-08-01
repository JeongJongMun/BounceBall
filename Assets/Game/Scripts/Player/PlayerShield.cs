using UnityEngine;

namespace Game
{
    // 1회성 실드의 실제 동작 (상점 소비형 문서 §5).
    // 방어 가능한 사망 발판 접촉을 1회 무효화하고 사라진다.
    // 낙사·비방어 사망에서는 방어 없이 제거만 된다 (§5.5) — 부활 경로가 Deactivate를 부른다.
    // 효과 에셋(ShieldEffect)이 필요할 때 붙여 주므로 프리팹에 직접 배치하지 않는다.
    [RequireComponent(typeof(Player))]
    public class PlayerShield : MonoBehaviour
    {
        private Player _player;
        private GameObject _visual;
        private float _knockbackHorizontal;
        private float _knockbackVertical;
        private float _invulnerabilityTime;
        private float _invulnerableUntil = float.NegativeInfinity;

        public bool IsActive { get; private set; }

        private Player PlayerRef => _player != null ? _player : (_player = GetComponent<Player>());

        // 문서 §5.3. 이미 켜져 있으면 실패 — 수량이 차감되지 않는다 (§5.2).
        public bool TryActivate(Sprite visualSprite, float knockbackHorizontal, float knockbackVertical,
            float invulnerabilityTime, int visualSortingOrder, float visualDiameter, Vector2 visualOffset)
        {
            if (IsActive) return false;
            if (PlayerRef.State == PlayerState.Disabled) return false;

            _knockbackHorizontal = knockbackHorizontal;
            _knockbackVertical = knockbackVertical;
            _invulnerabilityTime = invulnerabilityTime;

            IsActive = true;
            ShowVisual(visualSprite, visualSortingOrder, visualDiameter, visualOffset);
            return true;
        }

        // 사망 발판 접촉을 흡수했으면 true — 호출부(PlayerHazardContact)는 사망 처리를 건너뛴다 (문서 §5.6).
        // 파괴 직후의 짧은 무시 시간(§5.7) 동안의 재접촉도 여기서 흡수한다.
        public bool TryAbsorbLethalHit(Vector2 contactNormal)
        {
            if (Time.time < _invulnerableUntil) return true; // 같은 발판과의 연속 판정 방지

            if (!IsActive) return false;

            Deactivate();
            _invulnerableUntil = Time.time + _invulnerabilityTime;

            // 밀어내기: 수직 초기화 후 위 + 접촉면 반대 방향 (문서 §5.7)
            PlayerRef.Body.linearVelocity =
                ComputeKnockback(contactNormal, _knockbackHorizontal, _knockbackVertical);
            return true;
        }

        // 접촉 법선은 발판에서 플레이어 쪽을 향하므로, 그 수평 성분 방향으로 밀면 발판에서 멀어진다.
        public static Vector2 ComputeKnockback(Vector2 contactNormal, float horizontal, float vertical)
        {
            float x = Mathf.Approximately(contactNormal.x, 0f) ? 0f : Mathf.Sign(contactNormal.x) * horizontal;
            return new Vector2(x, vertical);
        }

        // 방어 없이 제거만 한다 — 낙사·비방어 사망·클리어 (문서 §5.5, §5.8).
        // 반발력·무시 시간을 적용하지 않는다.
        public void Deactivate()
        {
            if (!IsActive) return;
            IsActive = false;
            if (_visual != null) _visual.SetActive(false);
        }

        private void ShowVisual(Sprite sprite, int sortingOrder, float diameter, Vector2 offset)
        {
            if (_visual == null)
            {
                _visual = new GameObject("ShieldVisual");
                _visual.transform.SetParent(transform, false);
                var renderer = _visual.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite;
                renderer.sortingOrder = sortingOrder;
            }

            // 원본 이미지 해상도와 무관하게 지정한 지름이 되도록 축소·확대한다
            _visual.transform.localScale = Vector3.one * ComputeVisualScale(sprite, diameter);
            _visual.transform.localPosition = offset;
            _visual.SetActive(sprite != null);
        }

        // 스프라이트의 큰 쪽 변을 기준으로 목표 지름에 맞는 배율을 구한다.
        public static float ComputeVisualScale(Sprite sprite, float diameter)
        {
            if (sprite == null || diameter <= 0f) return 1f;

            float size = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
            return size > 0.0001f ? diameter / size : 1f;
        }
    }
}
