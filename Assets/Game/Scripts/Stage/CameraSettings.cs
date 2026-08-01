using UnityEngine;

namespace Game
{
    // 게임 전체 공통 카메라 조작감 (기획 §16.6).
    // 스테이지마다 달라지면 어색한 값들만 여기 둔다. 줌·오프셋 같은 프레이밍 값은 StageController에 있다.
    [CreateAssetMenu(menuName = "Game/Camera Settings", fileName = "CameraSettings")]
    public class CameraSettings : ScriptableObject
    {
        public const string ResourcePath = "CameraSettings";

        [Header("데드존 — 화면 절반 대비 비율")]
        [Tooltip("좌우로 이 범위 안에서 움직이는 동안에는 카메라가 따라가지 않는다. 크면 화면이 안정적, 작으면 반응이 빠르다")]
        [Range(0f, 1f)]
        [SerializeField] private float horizontalDeadzone = 0.33f;

        [Tooltip("위아래 기준. 점프할 때 화면이 출렁이면 이 값을 키운다")]
        [Range(0f, 1f)]
        [SerializeField] private float verticalDeadzone = 0.33f;

        [Header("추적 속도 — 목표까지 따라붙는 시간(초)")]
        [Tooltip("작을수록 빠릿하게 따라온다")]
        [SerializeField] private float horizontalSmoothTime = 0.15f;

        [Tooltip("세로를 조금 느리게 하면 점프가 부드러워 보인다")]
        [SerializeField] private float verticalSmoothTime = 0.15f;

        [Header("기타")]
        [Tooltip("부활할 때 카메라를 즉시 옮긴다. 끄면 맵을 가로질러 부드럽게 이동한다")]
        [SerializeField] private bool snapOnRespawn = true;

        [Tooltip("씬 화면에 데드존 사각형을 그린다. 빌드에는 영향이 없다")]
        [SerializeField] private bool showDeadzoneGizmo = true;

        public float HorizontalDeadzone => horizontalDeadzone;
        public float VerticalDeadzone => verticalDeadzone;
        public float HorizontalSmoothTime => horizontalSmoothTime;
        public float VerticalSmoothTime => verticalSmoothTime;
        public bool SnapOnRespawn => snapOnRespawn;
        public bool ShowDeadzoneGizmo => showDeadzoneGizmo;

        public static CameraSettings Load() => Resources.Load<CameraSettings>(ResourcePath);
    }
}
