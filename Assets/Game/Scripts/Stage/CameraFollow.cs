using UnityEngine;

namespace Game
{
    // 데드존 기반 카메라 추적 + 스테이지 경계 제한 (기획 §16).
    // 축별 독립 처리: 데드존 안이면 고정, 넘으면 넘은 만큼만 추적.
    // 조작감 값은 CameraSettings(글로벌), 프레이밍 값은 StageController(스테이지별)에서 온다.
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        private Camera _camera;
        private Transform _target;
        private StageController _stage;
        private CameraSettings _settings;
        private Vector2 _velocity;

        private void Awake() => _camera = GetComponent<Camera>();

        public void Init(Transform target, StageController stage)
        {
            if (_camera == null) _camera = GetComponent<Camera>();

            _target = target;
            _stage = stage;

            // 스테이지가 지정한 프리셋이 있으면 그것을, 없으면 글로벌 설정을 쓴다
            _settings = stage != null && stage.CameraSettingsOverride != null
                ? stage.CameraSettingsOverride
                : CameraSettings.Load();

            if (stage != null) _camera.orthographicSize = stage.CameraZoom;

            // 시작·부활 시 즉시 자리를 잡는다 (경계 안에서)
            if (_target == null) return;
            var snapped = ClampToStage(ComputeDesiredPosition(transform.position));
            transform.position = new Vector3(snapped.x, snapped.y, transform.position.z);
            _velocity = Vector2.zero;
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            var desired = ClampToStage(ComputeDesiredPosition(transform.position));

            float x = Mathf.SmoothDamp(transform.position.x, desired.x, ref _velocity.x, HorizontalSmoothTime);
            float y = Mathf.SmoothDamp(transform.position.y, desired.y, ref _velocity.y, VerticalSmoothTime);
            transform.position = new Vector3(x, y, transform.position.z);
        }

        // 축 잠금이면 스테이지 중앙에 고정하고, 아니면 데드존 밖으로 나간 만큼 따라간다.
        private Vector2 ComputeDesiredPosition(Vector2 current)
        {
            float halfHeight = _camera.orthographicSize;
            float halfWidth = halfHeight * _camera.aspect;

            bool lockX = _stage != null && _stage.LockCameraX;
            bool lockY = _stage != null && _stage.LockCameraY;
            float offsetY = _stage != null ? _stage.CameraVerticalOffset : 0f;

            float targetX = lockX
                ? StageCenterX()
                : ComputeAxisTarget(current.x, _target.position.x, halfWidth * HorizontalDeadzone);

            // 오프셋은 플레이어 위치를 올려 잡는 식으로 적용한다 — 값이 크면 캐릭터가 화면 아래에 놓인다
            float targetY = lockY
                ? StageCenterY()
                : ComputeAxisTarget(current.y, _target.position.y + offsetY, halfHeight * VerticalDeadzone);

            return new Vector2(targetX, targetY);
        }

        private float StageCenterX() => _stage != null ? (_stage.StageMinX + _stage.StageMaxX) * 0.5f : transform.position.x;
        private float StageCenterY() => _stage != null ? (_stage.StageMinY + _stage.StageMaxY) * 0.5f : transform.position.y;

        private float HorizontalDeadzone => _settings != null ? _settings.HorizontalDeadzone : 0.33f;
        private float VerticalDeadzone => _settings != null ? _settings.VerticalDeadzone : 0.33f;
        private float HorizontalSmoothTime => _settings != null ? _settings.HorizontalSmoothTime : 0.15f;
        private float VerticalSmoothTime => _settings != null ? _settings.VerticalSmoothTime : 0.15f;

        public bool SnapOnRespawn => _settings == null || _settings.SnapOnRespawn;

        private Vector2 ClampToStage(Vector2 position)
        {
            if (_stage == null) return position;

            float halfHeight = _camera.orthographicSize;
            float halfWidth = halfHeight * _camera.aspect;
            return new Vector2(
                ClampAxis(position.x, _stage.StageMinX, _stage.StageMaxX, halfWidth),
                ClampAxis(position.y, _stage.StageMinY, _stage.StageMaxY, halfHeight));
        }

        // 플레이어가 데드존 안이면 현재 위치 유지, 밖이면 데드존 경계까지만 따라간다.
        public static float ComputeAxisTarget(float camPos, float playerPos, float deadzoneHalf)
        {
            if (playerPos < camPos - deadzoneHalf) return playerPos + deadzoneHalf;
            if (playerPos > camPos + deadzoneHalf) return playerPos - deadzoneHalf;
            return camPos;
        }

        // 카메라 화면 끝점이 스테이지 밖을 비추지 않도록 제한. 스테이지가 화면보다 작으면 중앙 고정 (기획 §16.5).
        public static float ClampAxis(float value, float min, float max, float halfExtent)
        {
            if (max - min < halfExtent * 2f) return (min + max) * 0.5f;
            return Mathf.Clamp(value, min + halfExtent, max - halfExtent);
        }

        // 데드존을 씬 화면에 그려 기획자가 값을 눈으로 확인할 수 있게 한다.
        private void OnDrawGizmos()
        {
            var settings = _settings != null ? _settings : CameraSettings.Load();
            if (settings == null || !settings.ShowDeadzoneGizmo) return;

            var cam = _camera != null ? _camera : GetComponent<Camera>();
            if (cam == null || !cam.orthographic) return;

            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * cam.aspect;

            Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.9f);
            Gizmos.DrawWireCube(transform.position,
                new Vector3(halfWidth * settings.HorizontalDeadzone * 2f,
                            halfHeight * settings.VerticalDeadzone * 2f, 0f));
        }
    }
}
