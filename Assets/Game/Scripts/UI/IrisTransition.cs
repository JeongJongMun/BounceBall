using System.Collections;
using Core;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    // 마스크 이미지의 흰색(밝은) 영역이 구멍이다. 반대로 쓰려면 invertMask를 켠다.
    public class IrisTransition : Singleton<IrisTransition>
    {
        private static readonly int ScaleId = Shader.PropertyToID("_Scale");
        private static readonly int AspectId = Shader.PropertyToID("_Aspect");
        private static readonly int MaskAspectId = Shader.PropertyToID("_MaskAspect");
        private static readonly int MaskTexId = Shader.PropertyToID("_MaskTex");
        private static readonly int InvertMaskId = Shader.PropertyToID("_InvertMask");

        [SerializeField] private Texture2D maskTexture;
        [Tooltip("켜면 흰색 영역을 가림으로 해석한다 (기본은 흰색 = 구멍)")]
        [SerializeField] private bool invertMask;
        [SerializeField] private float closeDuration = 0.75f;
        [SerializeField] private float openDuration = 0.75f;
        [Tooltip("화면이 완전히 검은 상태로 머무는 시간")]
        [SerializeField] private float blackHoldDuration = 0.45f;
        [Tooltip("화면을 다 드러내려면 이 배율까지 커져야 한다. 마스크 여백에 따라 조절")]
        [SerializeField] private float openScale = 3f;

        private Canvas _canvas;
        private CanvasGroup _group;
        private Material _material;
        private Texture2D _fallbackMask;
        private Tween _tween;
        private float _scale;

        // Systems 프리팹에 없으면 폴백으로 생성한다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null) return;

            var systems = GameObject.Find("Systems");
            var go = new GameObject("IrisTransition");
            if (systems != null)
                go.transform.SetParent(systems.transform, false);
            else
                DontDestroyOnLoad(go);

            go.AddComponent<IrisTransition>();
        }

        protected override void Awake()
        {
            base.Awake();
            if (Instance != this) return;
            BuildOverlay();
        }

        protected override void OnDestroy()
        {
            _tween?.Kill();
            if (_material != null) Destroy(_material);
            if (_fallbackMask != null) Destroy(_fallbackMask);
            base.OnDestroy();
        }

        // 완전 암전을 즉시 표시한다. 스테이지 시작 페이드인 직전에 쓴다.
        public void ShowBlack()
        {
            EnsureReady();
            ApplyMaskSettings();
            _tween?.Kill();
            _canvas.enabled = true;
            SetScale(0f);
            _group.alpha = 1f;
        }

        // 구멍이 좁아지며 화면을 완전히 가린 뒤, 잠깐 암전을 유지한다.
        // 스케일이 클수록 빠르고, 작아질수록 느려진다 (OutQuad).
        public IEnumerator Close()
        {
            EnsureReady();
            ApplyMaskSettings();
            _canvas.enabled = true;

            SetScale(openScale);

            _tween?.Kill();
            _tween = DOTween.To(() => _scale, SetScale, 0f, closeDuration).SetEase(Ease.OutQuad);
            yield return _tween.WaitForCompletion();

            if (blackHoldDuration > 0f)
                yield return new WaitForSeconds(blackHoldDuration);
        }

        // 완전 암전 상태에서 전체 알파 페이드로 화면을 다시 밝힌다.
        public IEnumerator Open()
        {
            EnsureReady();
            ApplyMaskSettings();
            _canvas.enabled = true;

            // 아이리스는 닫힌 채(스케일 0)로 두고, 검정 오버레이만 페이드 아웃한다.
            SetScale(0f);
            _group.alpha = 1f;

            _tween?.Kill();
            _tween = _group.DOFade(0f, openDuration).SetEase(Ease.OutQuad);
            yield return _tween.WaitForCompletion();

            _canvas.enabled = false;
        }

        private void BuildOverlay()
        {
            var canvasGo = new GameObject("IrisCanvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);

            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // UI(100)보다 위, Pause(1100)·SceneLoader 페이드(2000)보다 아래
            _canvas.sortingOrder = 1000;
            _canvas.enabled = false;

            _group = canvasGo.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _group.interactable = false;

            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

            var imageGo = new GameObject("Iris", typeof(RectTransform));
            imageGo.transform.SetParent(canvasGo.transform, false);

            var rect = (RectTransform)imageGo.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            // RawImage는 UV가 항상 0~1이라 구멍이 화면 중앙에 맞는다.
            var raw = imageGo.AddComponent<RawImage>();
            raw.texture = Texture2D.whiteTexture;
            raw.color = Color.black;
            raw.raycastTarget = false;

            var source = Resources.Load<Material>("IrisWipe");
            _material = new Material(source);
            raw.material = _material;

            ApplyMaskSettings();
        }

        private void SetScale(float scale)
        {
            _scale = scale;
            _material.SetFloat(ScaleId, scale);
            // 커질수록 검정 오버레이 알파가 빠진다.
            _group.alpha = 1f - Mathf.Clamp01(scale / openScale);
        }

        private void ApplyMaskSettings()
        {
            _material.SetFloat(AspectId, Aspect());
            _material.SetFloat(InvertMaskId, invertMask ? 1f : 0f);

            var mask = maskTexture != null ? maskTexture : GetFallbackMask();
            _material.SetTexture(MaskTexId, mask);
            _material.SetFloat(MaskAspectId, (float)mask.width / Mathf.Max(1, mask.height));
        }

        // 마스크 미지정 시 중앙 흰색 원 (흰색 = 구멍)
        private Texture2D GetFallbackMask()
        {
            if (_fallbackMask != null) return _fallbackMask;

            const int size = 256;
            _fallbackMask = new Texture2D(size, size, TextureFormat.RGBA32, false);
            _fallbackMask.wrapMode = TextureWrapMode.Clamp;
            _fallbackMask.filterMode = FilterMode.Bilinear;

            float center = (size - 1) * 0.5f;
            float holeRadius = size * 0.35f;
            float soft = size * 0.04f;
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float shape = 1f - Mathf.Clamp01((dist - holeRadius) / soft);
                    byte v = (byte)Mathf.RoundToInt(shape * 255f);
                    pixels[y * size + x] = new Color32(v, v, v, v);
                }
            }

            _fallbackMask.SetPixels32(pixels);
            _fallbackMask.Apply(false, true);
            return _fallbackMask;
        }

        private void EnsureReady()
        {
            if (_material == null) BuildOverlay();
        }

        private static float Aspect() => (float)Screen.width / Mathf.Max(1, Screen.height);
    }
}
