using System;
using UnityEngine;

namespace Game
{
    // 이펙트 파편 하나. 튄 방향으로 날아가다 중력을 받아 떨어지며 서서히 사라진다.
    // 생성과 재사용은 PlayerPropertyEffect가 풀로 관리한다 — 직접 Instantiate하지 않는다.
    [RequireComponent(typeof(SpriteRenderer))]
    public class EffectParticle : MonoBehaviour
    {
        // 파편 하나의 거동. 스포너가 매번 무작위로 굴려서 넘긴다.
        public struct Motion
        {
            public Vector2 Velocity;
            public float Gravity;
            public float Lifetime;
            public float AngularSpeed;
            // 수명의 뒤쪽 몇 할을 페이드에 쓸지 (0~1). 0이면 끝까지 불투명하다가 사라진다.
            public float FadeRatio;
        }

        private SpriteRenderer _renderer;
        private Motion _motion;
        private Vector2 _velocity;
        private float _age;
        private Action<EffectParticle> _onFinished;

        private SpriteRenderer RendererRef => _renderer != null ? _renderer : (_renderer = GetComponent<SpriteRenderer>());

        public void Launch(Sprite sprite, Vector3 position, float scale, int sortingOrder, Motion motion, Action<EffectParticle> onFinished)
        {
            _motion = motion;
            _velocity = motion.Velocity;
            _age = 0f;
            _onFinished = onFinished;

            var t = transform;
            t.position = position;
            t.localScale = Vector3.one * scale;
            t.rotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f));

            var sr = RendererRef;
            sr.sprite = sprite;
            sr.color = Color.white;
            // 종류마다 플레이어 앞뒤가 달라지므로 재사용할 때마다 다시 지정한다.
            sr.sortingOrder = sortingOrder;
        }

        private void Update()
        {
            _age += Time.deltaTime;
            if (_motion.Lifetime <= 0f || _age >= _motion.Lifetime)
            {
                Finish();
                return;
            }

            _velocity.y -= _motion.Gravity * Time.deltaTime;

            var t = transform;
            t.position += (Vector3)(_velocity * Time.deltaTime);
            t.Rotate(0f, 0f, _motion.AngularSpeed * Time.deltaTime);

            // 수명 뒤쪽 구간에서만 투명해진다 — 처음부터 흐리면 파편이 잘 안 보인다.
            float fadeSpan = _motion.Lifetime * Mathf.Clamp01(_motion.FadeRatio);
            float alpha = fadeSpan <= 0f ? 1f : Mathf.Clamp01((_motion.Lifetime - _age) / fadeSpan);
            var sr = RendererRef;
            var c = sr.color;
            sr.color = new Color(c.r, c.g, c.b, alpha);
        }

        private void Finish()
        {
            var callback = _onFinished;
            _onFinished = null;
            if (callback != null) callback(this);
        }
    }
}
