using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace Game
{
    // 스테이지 시작 연출. 맵 전체를 한 번 보여준 뒤 실제 플레이 화면으로 줌인한다.
    // 연출 동안에는 물리를 멈추고(timeScale 0) CameraFollow를 꺼 두므로, 추적과 싸우지 않는다.
    // StageController가 필요할 때 카메라에 붙여 주므로 씬에 직접 배치하지 않는다.
    [RequireComponent(typeof(Camera))]
    public class StageIntroCamera : MonoBehaviour
    {
        private StageController _stage;
        private CameraFollow _follow;
        private CameraSettings _settings;
        private Action _onComplete;
        private Camera _camera;
        private bool _isFrozen;

        // 인트로를 시작했으면 true. false면 호출부가 곧바로 게임을 시작하면 된다.
        public static bool TryPlay(StageController stage, Action onComplete)
        {
            if (stage == null || !stage.UseIntroCamera) return false;

            var cam = Camera.main;
            if (cam == null) return false;

            var settings = stage.CameraSettingsOverride != null
                ? stage.CameraSettingsOverride
                : CameraSettings.Load();

            // 타일이 이미 플레이 화면에 다 들어오면 보여줄 게 없으므로 연출을 건너뛴다.
            // 여기서 빠지면 코루틴도 timeScale 조작도 일어나지 않는다.
            var tileSize = StageTiles.TryGetWorldBounds(out var tileBounds) ? tileBounds.size : Vector3.zero;
            float minZoomRatio = settings != null ? settings.IntroMinZoomRatio : 1.2f;

            if (!ShouldPlayIntro(tileSize.x, tileSize.y, cam.aspect, stage.CameraZoom, minZoomRatio))
                return false;

            if (!cam.TryGetComponent<StageIntroCamera>(out var intro))
                intro = cam.gameObject.AddComponent<StageIntroCamera>();

            intro.Play(stage, cam.GetComponent<CameraFollow>(), onComplete, settings);
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

        private void Awake() => _camera = GetComponent<Camera>();

        private void Play(StageController stage, CameraFollow follow, Action onComplete, CameraSettings settings)
        {
            _stage = stage;
            _follow = follow;
            _onComplete = onComplete;
            _settings = settings;

            StopAllCoroutines();
            StartCoroutine(Routine());
        }

        private IEnumerator Routine()
        {
            if (_camera == null) _camera = GetComponent<Camera>();

            float hold = _settings != null ? _settings.IntroHoldDuration : 1f;
            float duration = _settings != null ? _settings.IntroZoomDuration : 1f;
            var ease = _settings != null ? _settings.IntroEase : Ease.InOutCubic;

            // 도착 지점 = 지금 화면. CameraFollow.Init이 이미 플레이어 기준으로 잡아 둔 상태다.
            float playSize = _stage.CameraZoom;
            var playPosition = transform.position;

            // 연출 동안 추적을 멈추고 플레이어를 정지시킨다
            if (_follow != null) _follow.enabled = false;
            Freeze();

            // ① 맵 전체가 들어오는 화면으로 즉시 이동.
            //    맵이 플레이 화면보다 작으면 줌인이 아니라 줌아웃이 되어 버리므로 플레이 화면을 하한으로 둔다.
            var introBounds = ComputeIntroBounds();
            float fitSize = Mathf.Max(
                ComputeFitSize(introBounds.size.x, introBounds.size.y,
                               _camera.aspect, _stage.IntroPadding, _stage.IntroMaxSize),
                playSize);

            _camera.orthographicSize = fitSize;
            transform.position = ComputeIntroPosition(introBounds, fitSize, playPosition);

            // ② 유지 (timeScale 0이므로 실시간으로 센다)
            if (hold > 0f) yield return new WaitForSecondsRealtime(hold);

            // ③ 플레이 화면으로 전환
            if (duration > 0f)
            {
                var sequence = DOTween.Sequence().SetUpdate(true);
                sequence.Join(DOTween.To(() => _camera.orthographicSize, v => _camera.orthographicSize = v, playSize, duration)
                    .SetEase(ease));
                sequence.Join(transform.DOMove(playPosition, duration).SetEase(ease));

                while (sequence.IsActive() && !sequence.IsComplete()) yield return null;
            }

            // 트윈이 중간에 끊겼어도 정확한 값으로 마무리한다
            _camera.orthographicSize = playSize;
            transform.position = playPosition;

            if (_follow != null) _follow.enabled = true;

            Unfreeze();
            _onComplete?.Invoke();
        }

        // 인트로가 담아야 할 영역 = 스테이지 경계 ∪ 실제 타일 범위.
        // 경계를 아직 계산하지 않았거나 타일보다 좁게 잡힌 스테이지에서도 타일이 잘리지 않는다.
        private Bounds ComputeIntroBounds()
        {
            var bounds = new Bounds(
                new Vector3((_stage.StageMinX + _stage.StageMaxX) * 0.5f,
                            (_stage.StageMinY + _stage.StageMaxY) * 0.5f, 0f),
                new Vector3(_stage.StageMaxX - _stage.StageMinX,
                            _stage.StageMaxY - _stage.StageMinY, 0f));

            if (StageTiles.TryGetWorldBounds(out var tiles))
            {
                bounds.Encapsulate(new Vector3(tiles.min.x, tiles.min.y, 0f));
                bounds.Encapsulate(new Vector3(tiles.max.x, tiles.max.y, 0f));
            }

            return bounds;
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
