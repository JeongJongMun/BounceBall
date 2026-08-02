using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace Game
{
    // 스테이지 시작 연출. 맵 전체를 한 번 보여준 뒤 실제 플레이 화면으로 줌인한다.
    // 연출 동안에는 물리를 멈추고(timeScale 0) CameraFollow를 꺼 두므로, 추적과 싸우지 않는다.
    // StageController가 필요할 때 카메라에 붙여 주므로 씬에 직접 배치하지 않는다.
    //
    // 호출 순서 (암전 → 전체 맵 선배치 → 페이드인 → hold/줌인):
    //   TryPrepare → IrisTransition.Open → PlayPrepared
    // Prepare 없이 Open부터 하면 페이드인 동안 플레이어 뷰가 노출된다.
    [RequireComponent(typeof(Camera))]
    public class StageIntroCamera : MonoBehaviour
    {
        private StageController _stage;
        private CameraFollow _follow;
        private CameraSettings _settings;
        private Action _onComplete;
        private Camera _camera;
        private bool _isFrozen;
        private bool _prepared;
        private float _playSize;
        private Vector3 _playPosition;

        // 암전 중에 전체 맵으로 카메라를 옮긴다. 성공하면 PlayPrepared를 Open 이후에 호출해야 한다.
        public static bool TryPrepare(StageController stage)
        {
            if (!TryResolve(stage, out var cam, out var settings, out var follow)) return false;

            if (!cam.TryGetComponent<StageIntroCamera>(out var intro))
                intro = cam.gameObject.AddComponent<StageIntroCamera>();

            intro.Prepare(stage, follow, settings);
            return true;
        }

        // Prepare로 잡아 둔 전체 맵 화면에서 hold → 플레이 줌인을 시작한다.
        public static bool PlayPrepared(Action onComplete)
        {
            var cam = Camera.main;
            if (cam == null) return false;
            if (!cam.TryGetComponent<StageIntroCamera>(out var intro) || !intro._prepared) return false;

            intro.StartPrepared(onComplete);
            return true;
        }

        // 인트로를 시작했으면 true. false면 호출부가 곧바로 게임을 시작하면 된다.
        // Open 이후에 호출하면 페이드인 동안 플레이어 뷰가 보이므로, 시작 연출에는 TryPrepare를 쓴다.
        public static bool TryPlay(StageController stage, Action onComplete)
        {
            if (!TryPrepare(stage)) return false;
            return PlayPrepared(onComplete);
        }

        private static bool TryResolve(StageController stage, out Camera cam,
            out CameraSettings settings, out CameraFollow follow)
        {
            cam = null;
            settings = null;
            follow = null;

            if (stage == null || !stage.UseIntroCamera) return false;

            cam = Camera.main;
            if (cam == null) return false;

            settings = stage.CameraSettingsOverride != null
                ? stage.CameraSettingsOverride
                : CameraSettings.Load();

            // 맵이 이미 플레이 화면에 다 들어오면 보여줄 게 없으므로 연출을 건너뛴다.
            // 여기서 빠지면 코루틴도 timeScale 조작도 일어나지 않는다.
            var contentSize = StageContentBounds.TryGet(out var content) ? content.size : Vector3.zero;
            float minZoomRatio = settings != null ? settings.IntroMinZoomRatio : 1.2f;

            if (!ShouldPlayIntro(contentSize.x, contentSize.y, cam.aspect, stage.CameraZoom, minZoomRatio))
                return false;

            follow = cam.GetComponent<CameraFollow>();
            return true;
        }

        // 타일 전체가 플레이 화면보다 minZoomRatio 배 이상 클 때만 인트로가 의미 있다.
        // 여백 배율 없이(=1) 계산해 "딱 들어오는가"를 본다.
        public static bool ShouldPlayIntro(float tileWidth, float tileHeight,
            float aspect, float playSize, float minZoomRatio)
        {
            if (tileWidth <= 0f || tileHeight <= 0f) return false; // 타일이 없는 씬
            if (playSize <= 0f) return false;

            float needed = ComputeFitSize(tileWidth, tileHeight, aspect, 1f, 0f);
            return needed > playSize * Mathf.Max(minZoomRatio, 1f);
        }

        private float IntroPadding => _settings != null ? _settings.IntroPadding : 1.2f;

        private void Awake() => _camera = GetComponent<Camera>();

        private void Prepare(StageController stage, CameraFollow follow, CameraSettings settings)
        {
            if (_camera == null) _camera = GetComponent<Camera>();

            _stage = stage;
            _follow = follow;
            _settings = settings;
            _onComplete = null;

            StopAllCoroutines();
            transform.DOKill();
            Unfreeze();

            // 도착 지점 = 지금 화면. CameraFollow.Init이 이미 플레이어 기준으로 잡아 둔 상태다.
            _playSize = _stage.CameraZoom;
            _playPosition = transform.position;

            // 페이드인 전에 추적을 끄고 전체 맵으로 옮긴다. freeze는 Open 이후에 건다.
            if (_follow != null) _follow.enabled = false;

            var introBounds = ComputeIntroBounds();
            float fitSize = Mathf.Max(
                ComputeFitSize(introBounds.size.x, introBounds.size.y,
                               _camera.aspect, IntroPadding, _stage.IntroMaxSize),
                _playSize);

            _camera.orthographicSize = fitSize;
            transform.position = ComputeIntroPosition(introBounds, fitSize, _playPosition);

            _prepared = true;
        }

        private void StartPrepared(Action onComplete)
        {
            if (!_prepared) return;

            _onComplete = onComplete;
            _prepared = false;
            StopAllCoroutines();
            StartCoroutine(Routine());
        }

        private IEnumerator Routine()
        {
            if (_camera == null) _camera = GetComponent<Camera>();

            float hold = _settings != null ? _settings.IntroHoldDuration : 1f;
            float duration = _settings != null ? _settings.IntroZoomDuration : 1f;
            var ease = _settings != null ? _settings.IntroEase : Ease.InOutCubic;

            // 연출 동안 플레이어를 정지시킨다 (카메라는 Prepare에서 이미 옮겨 둔 상태)
            if (_follow != null) _follow.enabled = false;
            Freeze();

            // ① 유지 (timeScale 0이므로 실시간으로 센다)
            if (hold > 0f) yield return new WaitForSecondsRealtime(hold);

            // ② 플레이 화면으로 전환
            if (duration > 0f)
            {
                var sequence = DOTween.Sequence().SetUpdate(true);
                sequence.Join(DOTween.To(() => _camera.orthographicSize, v => _camera.orthographicSize = v, _playSize, duration)
                    .SetEase(ease));
                sequence.Join(transform.DOMove(_playPosition, duration).SetEase(ease));

                while (sequence.IsActive() && !sequence.IsComplete()) yield return null;
            }

            // 트윈이 중간에 끊겼어도 정확한 값으로 마무리한다
            _camera.orthographicSize = _playSize;
            transform.position = _playPosition;

            if (_follow != null) _follow.enabled = true;

            Unfreeze();
            _onComplete?.Invoke();
        }

        private Bounds ComputeIntroBounds()
        {
            var stageBounds = MakeBounds(_stage.StageMinX, _stage.StageMaxX, _stage.StageMinY, _stage.StageMaxY);
            bool hasContent = StageContentBounds.TryGet(out var content);

            return ComputeIntroBounds(stageBounds, hasContent, content);
        }

        // 인트로가 담아야 할 영역 = 스테이지 경계 ∩ 실제 배치물 범위.
        // 경계는 배치물 끝에서 여백만큼 더 넓으므로, 교집합을 쓰면 빈 공간 없이 맵이 화면을 채운다.
        // 플레이 영역 밖으로 뻗은 배경 타일맵이 있어도 경계가 잘라 준다.
        public static Bounds ComputeIntroBounds(Bounds stageBounds, bool hasContent, Bounds content)
        {
            if (!hasContent) return stageBounds;

            float minX = Mathf.Max(stageBounds.min.x, content.min.x);
            float maxX = Mathf.Min(stageBounds.max.x, content.max.x);
            float minY = Mathf.Max(stageBounds.min.y, content.min.y);
            float maxY = Mathf.Min(stageBounds.max.y, content.max.y);

            // 좌표가 어긋나 겹치는 곳이 없으면 경계를 쓴다 — 빈 영역을 비추지 않도록
            if (maxX <= minX || maxY <= minY) return stageBounds;

            return MakeBounds(minX, maxX, minY, maxY);
        }

        private static Bounds MakeBounds(float minX, float maxX, float minY, float maxY)
        {
            return new Bounds(
                new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, 0f),
                new Vector3(maxX - minX, maxY - minY, 0f));
        }

        // 맵 중앙을 비춘다. 축소 한계에 걸려 맵이 다 들어오지 않으면 시작 지점 쪽을 우선한다.
        private Vector3 ComputeIntroPosition(Bounds bounds, float size, Vector3 playPosition)
        {
            bool showsWholeMap = _stage.IntroMaxSize <= 0f || size < _stage.IntroMaxSize + Mathf.Epsilon;

            float x = showsWholeMap ? bounds.center.x : playPosition.x;
            float y = showsWholeMap ? bounds.center.y : playPosition.y;

            // 담아야 할 영역 밖을 비추지 않도록 제한 (CameraFollow와 동일한 규칙)
            float halfHeight = size;
            float halfWidth = size * _camera.aspect;

            return new Vector3(
                CameraFollow.ClampAxis(x, bounds.min.x, bounds.max.x, halfWidth),
                CameraFollow.ClampAxis(y, bounds.min.y, bounds.max.y, halfHeight),
                playPosition.z);
        }

        private void Freeze()
        {
            if (_isFrozen) return;
            _isFrozen = true;
            Time.timeScale = 0f;
        }

        // 연출이 끝나거나 중간에 씬이 바뀌어도 게임이 멈춘 채 남지 않도록 되돌린다.
        private void Unfreeze()
        {
            if (!_isFrozen) return;
            _isFrozen = false;
            Time.timeScale = 1f;
        }

        private void OnDisable()
        {
            transform.DOKill();
            _prepared = false;
            Unfreeze();
        }

        // 스테이지 전체가 화면에 들어오는 orthographicSize.
        // 가로가 긴 맵은 가로가, 세로가 긴 맵은 세로가 기준이 된다.
        // maxSize가 0보다 크면 그 이상 축소하지 않는다 (타일이 너무 작아지는 것 방지).
        public static float ComputeFitSize(float width, float height, float aspect, float padding, float maxSize)
        {
            float byHeight = height * 0.5f;
            float byWidth = width * 0.5f / Mathf.Max(aspect, 0.0001f);

            float size = Mathf.Max(byHeight, byWidth) * Mathf.Max(padding, 0.01f);
            return maxSize > 0f ? Mathf.Min(size, maxSize) : size;
        }
    }
}
