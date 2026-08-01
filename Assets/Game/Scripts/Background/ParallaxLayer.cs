using UnityEngine;

namespace Game
{
    // 패럴랙스 배경 한 겹. 카메라 이동량의 일부만 따라 움직여 원근감을 만든다.
    // 자식 프롭들은 반복 폭 밖으로 나가면 반대편으로 되감겨, 맵이 아무리 길어도 배경이 끊기지 않는다.
    public class ParallaxLayer : MonoBehaviour
    {
        [Label("가로 이동 비율")]
        [Tooltip("카메라를 좌우로 얼마나 따라갈지. 1이면 완전히 따라가 화면에 붙은 듯 멀어 보이고, 0이면 지형처럼 그대로 흘러간다")]
        [Range(0f, 1f)]
        [SerializeField] private float horizontalFactor = 0.85f;

        [Label("세로 이동 비율")]
        [Tooltip("위아래 기준. 값이 작을수록 위로 올라갈 때 화면 아래로 빨리 사라진다")]
        [Range(0f, 1f)]
        [SerializeField] private float verticalFactor = 0.9f;

        [Label("반복 폭")]
        [Tooltip("이 폭을 주기로 프롭이 되감긴다. 인트로 줌아웃 화면보다 넓어야 프롭이 튀는 게 보이지 않는다")]
        [SerializeField] private float repeatWidth = 40f;

        [Label("자동 흐름 속도")]
        [Tooltip("초당 이동 거리. 음수면 왼쪽으로 흐른다. 구름에만 쓰고 나머지는 0")]
        [SerializeField] private float autoScrollSpeed = 0f;

        private Transform _camera;
        private Camera _cameraComponent;
        private Vector3 _startPosition;
        private Vector3 _cameraStart;
        private float _referenceSize;
        private float _scrollOffset;
        private bool _initialized;

        public float RepeatWidth => repeatWidth;

        // 기준 카메라 위치를 "지금 카메라가 있는 곳"이 아니라 스테이지 경계에서 계산한 고정 지점으로 잡는다.
        // 첫 프레임 위치를 쓰면 인트로 줌아웃 중에 초기화될 때 기준이 밀려, 인트로 재생 여부에 따라
        // 배경 높이가 달라진다. 고정 기준이면 어느 시점에 초기화돼도 항상 같은 그림이 나온다.
        // 기준: 세로 = 플레이 카메라가 내려갈 수 있는 최저 중심 (씬에 배치한 밑동이 화면 최하단과 만나는 지점),
        //       가로 = 스테이지 중앙.
        private void Initialize()
        {
            var cam = Camera.main;
            if (cam == null) return;

            var stage = FindAnyObjectByType<StageController>();

            _camera = cam.transform;
            _cameraComponent = cam;
            _startPosition = transform.position;
            _cameraStart = ComputeReferenceCamera(stage, _camera.position);
            // 배경 밑동은 이 크기의 화면 최하단에 맞춰 배치돼 있다. 줌 보정의 기준이 된다.
            _referenceSize = stage != null ? stage.CameraZoom : cam.orthographicSize;
            _initialized = true;
        }

        public static Vector3 ComputeReferenceCamera(StageController stage, Vector3 fallback)
        {
            if (stage == null) return fallback; // 스테이지가 없는 씬(메인 메뉴 등)은 기존 동작 유지

            float x = (stage.StageMinX + stage.StageMaxX) * 0.5f;
            float y = CameraFollow.ClampAxis(stage.StageMinY, stage.StageMinY, stage.StageMaxY, stage.CameraZoom);
            return new Vector3(x, y, fallback.z);
        }

        private void LateUpdate()
        {
            if (!_initialized)
            {
                Initialize();
                if (!_initialized) return;
            }

            // 인트로(timeScale 0) 중에도 구름이 흐르도록 실시간을 쓴다
            if (autoScrollSpeed != 0f)
            {
                _scrollOffset += autoScrollSpeed * Time.unscaledDeltaTime;
                // 프롭이 반복 폭 주기로 배치돼 있어 한 주기만큼 되돌려도 그림이 같다 — 값이 무한히 커지는 것을 막는다
                if (repeatWidth > 0f) _scrollOffset = Mathf.Repeat(_scrollOffset, repeatWidth);
            }

            var cameraDelta = _camera.position - _cameraStart;

            // 인트로 줌아웃처럼 화면이 기준보다 커지면 화면 최하단이 내려간 만큼 배경도 따라 내린다.
            // 밑동이 항상 화면 하단에 붙어 있어 아래로 빈 공간이 드러나지 않는다 (줌인 방향은 보정 불필요).
            float zoomDrop = ComputeZoomDrop(
                _cameraComponent != null ? _cameraComponent.orthographicSize : _referenceSize, _referenceSize);

            transform.position = new Vector3(
                _startPosition.x + cameraDelta.x * horizontalFactor + _scrollOffset,
                _startPosition.y + cameraDelta.y * verticalFactor - zoomDrop,
                _startPosition.z);

            WrapProps();
        }

        // 카메라에서 반복 폭의 절반 이상 벗어난 프롭을 반대편으로 옮긴다.
        // 화면 밖에서만 일어나므로 순간이동이 보이지 않는다.
        private void WrapProps()
        {
            if (repeatWidth <= 0f) return;

            float center = _camera.position.x;
            for (int i = 0; i < transform.childCount; i++)
            {
                var prop = transform.GetChild(i);
                var position = prop.position;

                float wrapped = Wrap(position.x, center, repeatWidth);
                if (!Mathf.Approximately(wrapped, position.x))
                    prop.position = new Vector3(wrapped, position.y, position.z);
            }
        }

        // 화면이 기준 크기보다 커진 만큼 화면 최하단이 내려간 거리. 줌인(작아짐)은 0.
        public static float ComputeZoomDrop(float currentSize, float referenceSize)
        {
            return Mathf.Max(0f, currentSize - referenceSize);
        }

        // value를 center ± width/2 범위 안으로 되감는다. 여러 주기 벗어나 있어도 한 번에 처리한다.
        public static float Wrap(float value, float center, float width)
        {
            if (width <= 0f) return value;

            float half = width * 0.5f;
            return center + Mathf.Repeat(value - center + half, width) - half;
        }

        // 에디터 툴이 생성한 프롭 배치를 반영할 때 사용.
        public void SetLayout(float horizontal, float vertical, float width, float scroll)
        {
            horizontalFactor = horizontal;
            verticalFactor = vertical;
            repeatWidth = width;
            autoScrollSpeed = scroll;
        }
    }
}
