using UnityEngine;

namespace Game
{
    // 인스펙터에 표시할 이름을 지정한다. 변수명과 저장 데이터는 그대로 두고 라벨만 바꾼다.
    // 예: [Label("카메라 크기")] [SerializeField] private float cameraZoom;
    public class LabelAttribute : PropertyAttribute
    {
        public string Text { get; }

        public LabelAttribute(string text) => Text = text;
    }
}
