using DG.Tweening;
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
        [Label("가로 데드존")]
        [Tooltip("좌우로 이 범위 안에서 움직이는 동안에는 카메라가 따라가지 않는다. 크면 화면이 안정적, 작으면 반응이 빠르다")]
        [Range(0f, 1f)]
        [SerializeField] private float horizontalDeadzone = 0.33f;

        [Label("세로 데드존")]
        [Tooltip("위아래 기준. 점프할 때 화면이 출렁이면 이 값을 키운다")]
        [Range(0f, 1f)]
        [SerializeField] private float verticalDeadzone = 0.33f;

        [Header("추적 속도 — 목표까지 따라붙는 시간(초)")]
        [Label("가로 추적 속도")]
        [Tooltip("작을수록 빠릿하게 따라온다")]
        [SerializeField] private float horizontalSmoothTime = 0.15f;

        [Label("세로 추적 속도")]
        [Tooltip("세로를 조금 느리게 하면 점프가 부드러워 보인다")]
        [SerializeField] private float verticalSmoothTime = 0.15f;

        [Header("스테이지 인트로 — 시작할 때 맵 전체를 한 번 보여주는 연출")]
        [Label("전체 맵 유지 시간")]
        [Tooltip("맵 전체를 보여준 채 멈춰 있는 시간(초)")]
        [SerializeField] private float introHoldDuration = 1f;

        [Label("플레이 화면 전환 시간")]
        [Tooltip("맵 전체에서 실제 플레이 화면으로 줌인하는 데 걸리는 시간(초)")]
        [SerializeField] private float introZoomDuration = 1f;

        [Label("맵 여백 배율")]
        [Tooltip("인트로에서 맵 주변에 얼마나 여백을 둘지. 1이면 맵에 딱 맞고, 클수록 넓게 보인다")]
        [SerializeField] private float introPadding = 1.2f;

        [Label("인트로 최소 확대 비율")]
        [Tooltip("맵이 플레이 화면보다 이 배율 이상 클 때만 인트로를 재생한다. 타일이 이미 다 보이면 연출을 건너뛴다")]
        [SerializeField] private float introMinZoomRatio = 1.1f;

        [Label("전환 곡선")]
        [Tooltip("줌인이 가감속하는 방식. InOutCubic이면 천천히 시작해 천천히 멈춘다")]
        [SerializeField] private Ease introEase = Ease.InOutCubic;

        [Header("기타")]
        [Label("부활 시 즉시 이동")]
        [Tooltip("부활할 때 카메라를 즉시 옮긴다. 끄면 맵을 가로질러 부드럽게 이동한다")]
        [SerializeField] private bool snapOnRespawn = true;

        [Label("데드존 표시")]
        [Tooltip("씬 화면에 데드존 사각형을 그린다. 빌드에는 영향이 없다")]
        [SerializeField] private bool showDeadzoneGizmo = true;

        public float HorizontalDeadzone => horizontalDeadzone;
        public float VerticalDeadzone => verticalDeadzone;
        public float HorizontalSmoothTime => horizontalSmoothTime;
        public float VerticalSmoothTime => verticalSmoothTime;
        public float IntroHoldDuration => introHoldDuration;
        public float IntroZoomDuration => introZoomDuration;
        public float IntroPadding => introPadding;
        public float IntroMinZoomRatio => introMinZoomRatio;
        public Ease IntroEase => introEase;
        public bool SnapOnRespawn => snapOnRespawn;
        public bool ShowDeadzoneGizmo => showDeadzoneGizmo;

        public static CameraSettings Load() => Resources.Load<CameraSettings>(ResourcePath);
    }
}
