using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    // 배경 전체의 기준점. 스테이지마다 지면 높이가 다르므로 지평선을 자동으로 맞춘다.
    // 각 레이어(산·구름·나무)는 이 오브젝트 아래에서 지평선 기준 상대 높이로 배치된다.
    public class ParallaxBackground : MonoBehaviour
    {
        // 프롭 배치를 다시 만들 때 쓰는 설정. 인스펙터의 [배경 다시 생성] 버튼이 읽는다.
        [System.Serializable]
        public class LayerConfig
        {
            [Label("레이어 이름")] public string layerName = "Layer";
            [Label("스프라이트")] public Sprite[] sprites;

            [Label("프롭 개수")] public int count = 6;
            [Label("반복 폭")] public float repeatWidth = 40f;

            [Label("지평선 기준 높이")]
            [Tooltip("지평선에서 이만큼 위에 놓는다. 나무·산은 0이면 지면에 딱 선다")]
            public float baseY = 0f;

            [Label("높이 흔들기")] public float yJitter = 0.3f;

            [Label("기본 크기")]
            [Tooltip("원본 이미지 대비 배율. 산이 구름보다 높아지지 않도록 조절한다")]
            public float baseScale = 1f;

            [Label("가로 이동 비율")] [Range(0f, 1f)] public float horizontalFactor = 0.85f;
            [Label("세로 이동 비율")] [Range(0f, 1f)] public float verticalFactor = 0.9f;
            [Label("자동 흐름 속도")] public float autoScrollSpeed = 0f;

            [Label("정렬 순서")]
            [Tooltip("작을수록 뒤에 그려진다. 지형(0)보다 확실히 작아야 한다")]
            public int sortingOrder = -300;
        }

        [Label("지평선 오프셋")]
        [Tooltip("스테이지 경계 아래쪽(= 화면 최하단)에서 위아래로 더 옮길 거리. 산·나무가 너무 낮거나 높으면 여기서 조정한다")]
        [SerializeField] private float horizonOffset = 0f;

        [Label("배치 시드")]
        [Tooltip("같은 숫자면 항상 같은 배치가 나온다. 마음에 들 때까지 숫자를 바꿔가며 다시 생성한다")]
        [SerializeField] private int seed = 12345;

        [Label("레이어")]
        [SerializeField] private List<LayerConfig> layers = new();

        public int Seed => seed;
        public IReadOnlyList<LayerConfig> Layers => layers;

        private void Awake() => AlignToStage();

        private void AlignToStage()
        {
            var stage = FindAnyObjectByType<StageController>();
            if (stage == null) return;

            float horizon = ComputeHorizon(stage.StageMinY, stage.StageMaxY, stage.CameraZoom, horizonOffset);
            transform.position = new Vector3(transform.position.x, horizon, transform.position.z);
        }

        // 카메라가 보여줄 수 있는 가장 낮은 지점에 지평선을 둔다. 그래야 산·나무 밑동이
        // 화면 최하단이나 그 아래에 놓여 아래쪽에 빈 공간이 생기지 않는다.
        // 맵이 화면보다 낮으면 카메라가 세로 중앙에 고정되어 경계보다 더 아래까지 보이므로,
        // 경계를 그대로 쓰지 않고 CameraFollow와 같은 규칙으로 실제 최하단을 구한다.
        public static float ComputeHorizon(float stageMinY, float stageMaxY, float cameraZoom, float offset)
        {
            float lowestCameraY = CameraFollow.ClampAxis(stageMinY, stageMinY, stageMaxY, cameraZoom);
            return lowestCameraY - cameraZoom + offset;
        }

        // 반복 폭을 count칸으로 나눠 i번째 프롭의 x를 구한다. 균등하게 퍼지되 지터로 규칙성을 지운다.
        // 결과는 항상 [-width/2, width/2) 안에 들어온다.
        public static float PropX(int index, int count, float width, float jitter01)
        {
            if (count <= 0 || width <= 0f) return 0f;

            float slot = width / count;
            float center = -width * 0.5f + slot * (index + 0.5f);
            return center + (jitter01 - 0.5f) * slot;
        }
    }
}
