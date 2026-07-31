using UnityEngine;

namespace Game
{
    // 데드존 기반 카메라 추적 + 스테이지 경계 제한 (기획 §16).
    // 축별 독립 처리: 데드존(화면 1/3~2/3) 안이면 고정, 넘으면 넘은 만큼만 추적.
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private float smoothTime = 0.15f;

        private Camera _camera;
        private Transform _target;
        private StageController _stage;
        private Vector2 _velocity;

        private void Awake() => _camera = GetComponent<Camera>();

        public void Init(Transform target, StageController stage)
        {
            _target = target;
            _stage = stage;

            // 시작 시 타깃 위치로 즉시 이동 (경계 안에서)
            if (_target != null)
            {
                var snapped = ClampToStage(new Vector2(_target.position.x, _target.position.y));
                transform.position = new Vector3(snapped.x, snapped.y, transform.position.z);
            }
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            float halfHeight = _camera.orthographicSize;
            float halfWidth = halfHeight * _camera.aspect;

            // 데드존 = 화면 1/3 ~ 2/3 → 중심에서 ±(반폭/3)
            float targetX = ComputeAxisTarget(transform.position.x, _target.position.x, halfWidth / 3f);
            float targetY = ComputeAxisTarget(transform.position.y, _target.position.y, halfHeight / 3f);

            var clamped = ClampToStage(new Vector2(targetX, targetY));

            float x = Mathf.SmoothDamp(transform.position.x, clamped.x, ref _velocity.x, smoothTime);
            float y = Mathf.SmoothDamp(transform.position.y, clamped.y, ref _velocity.y, smoothTime);
            transform.position = new Vector3(x, y, transform.position.z);
        }

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
    }
}
