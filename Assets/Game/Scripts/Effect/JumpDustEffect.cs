using System.Collections;
using UnityEngine;

namespace Game
{
    // 점프 먼지. 파티클이 아니라 발밑에서 한 번 재생되는 프레임 애니메이션이다
    // (Effect_Jump·Effect_Superjump 모두 퍼졌다가 흩어지는 순서로 그려져 있다).
    //
    // 튄 자리에 남아야 하므로 플레이어 자식이 아니라 씬에 따로 띄운다.
    public class JumpDustEffect : MonoBehaviour
    {
        [Header("일반 점프")]
        [Tooltip("Effect_Jump 스프라이트를 번호 순서대로 넣는다")]
        [SerializeField] private Sprite[] frames;
        [Tooltip("한 번 재생에 걸리는 시간(초)")]
        [SerializeField] private float duration = 0.25f;
        [Tooltip("먼지의 가로 폭(월드 단위). 원본이 6유닛대라 이 값으로 줄인다")]
        [SerializeField] private float width = 1.4f;

        [Header("슈퍼 점프")]
        [Tooltip("Effect_Superjump 스프라이트를 번호 순서대로 넣는다")]
        [SerializeField] private Sprite[] superFrames;
        [SerializeField] private float superDuration = 0.35f;
        [Tooltip("원본이 11유닛대라 이 값으로 줄인다")]
        [SerializeField] private float superWidth = 2.4f;

        [Header("배치")]
        [Tooltip("발밑 기준 위치 보정. y를 낮추면 먼지가 더 아래에 깔린다")]
        [SerializeField] private Vector2 positionOffset = new Vector2(0f, -0.1f);
        [Tooltip("플레이어 렌더 순서 대비 오프셋. 음수면 플레이어 뒤에 그려진다")]
        [SerializeField] private int sortingOrderOffset = -1;

        private CircleCollider2D _body;
        private SpriteRenderer _renderer;
        private Coroutine _playing;
        private int _sortingLayerId;
        private int _playerSortingOrder;

        public bool IsPlaying => _playing != null;

        // 에디터 툴링/테스트에서 데이터를 지정할 때 사용.
        public void SetData(Sprite[] dustFrames, float playDuration)
        {
            frames = dustFrames;
            duration = playDuration;
        }

        public void SetSuperData(Sprite[] dustFrames, float playDuration)
        {
            superFrames = dustFrames;
            superDuration = playDuration;
        }

        private void Awake()
        {
            _body = GetComponent<CircleCollider2D>();

            var playerRenderer = GetComponentInChildren<Renderer>();
            if (playerRenderer != null)
            {
                _sortingLayerId = playerRenderer.sortingLayerID;
                _playerSortingOrder = playerRenderer.sortingOrder;
            }
        }

        private void OnDestroy()
        {
            if (_renderer != null) Destroy(_renderer.gameObject);
        }

        // 자동 바운스가 일어난 순간 PlayerBounce가 호출한다.
        public void Play() => Play(frames, duration, width);

        // 슈퍼 점프 발판에서 튕겼을 때 (기믹 문서 §3.2).
        public void PlaySuper() => Play(superFrames, superDuration, superWidth);

        private void Play(Sprite[] playFrames, float playDuration, float playWidth)
        {
            if (playFrames == null || playFrames.Length == 0 || !isActiveAndEnabled) return;

            // 연타로 들어오면 이전 재생을 끊고 새로 시작한다.
            if (_playing != null) StopCoroutine(_playing);
            _playing = StartCoroutine(PlayRoutine(playFrames, playDuration, playWidth));
        }

        private IEnumerator PlayRoutine(Sprite[] playFrames, float playDuration, float playWidth)
        {
            var sr = RendererRef;
            float radius = _body != null ? _body.radius : 0.35f;

            // 튄 자리에 남는다 — 시작할 때 위치를 한 번만 잡는다.
            sr.transform.position = transform.position + Vector3.down * radius + (Vector3)positionOffset;
            sr.gameObject.SetActive(true);

            float elapsed = 0f;
            while (elapsed < playDuration)
            {
                // 수명에 걸쳐 프레임을 순서대로 넘긴다.
                int index = Mathf.Clamp(Mathf.FloorToInt(elapsed / playDuration * playFrames.Length), 0, playFrames.Length - 1);
                ApplyFrame(sr, playFrames[index], playWidth);

                elapsed += Time.deltaTime;
                yield return null;
            }

            sr.gameObject.SetActive(false);
            _playing = null;
        }

        // 원본이 6~11유닛대로 크고 장마다 가로세로가 달라, 지정한 폭에 맞춰 비율을 유지한 채 줄인다.
        private static void ApplyFrame(SpriteRenderer sr, Sprite sprite, float targetWidth)
        {
            if (sprite == null) return;
            sr.sprite = sprite;

            float ppu = sprite.pixelsPerUnit;
            float spriteWidth = ppu > 0f ? sprite.rect.width / ppu : 0f;
            float scale = spriteWidth > 0f ? targetWidth / spriteWidth : 1f;
            sr.transform.localScale = Vector3.one * scale;
        }

        private SpriteRenderer RendererRef
        {
            get
            {
                if (_renderer != null) return _renderer;

                var go = new GameObject("JumpDust");
                _renderer = go.AddComponent<SpriteRenderer>();
                _renderer.sortingLayerID = _sortingLayerId;
                _renderer.sortingOrder = _playerSortingOrder + sortingOrderOffset;
                go.SetActive(false);
                return _renderer;
            }
        }
    }
}
