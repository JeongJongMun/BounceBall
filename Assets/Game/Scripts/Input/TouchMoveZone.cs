using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game
{
    // 화면 좌/우 절반을 덮는 투명 터치 영역.
    //
    // 자동 바운스라 조작이 좌우뿐이어서 버튼을 그리지 않고 화면을 반으로 나눠 쓴다.
    // 화면을 가리지 않고, 손가락 위치를 정확히 맞출 필요도 없다.
    //
    // EventSystem을 거치므로 일시정지·퀵슬롯 같은 위쪽 UI를 누를 때는
    // 여기로 내려오지 않는다 — 버튼을 누르려다 캐릭터가 움직이는 일이 없다.
    //
    // 마우스는 무시한다. 데스크톱 브라우저에서도 Touchscreen 장치가 잡히기 때문에
    // 장치 유무로 이 영역을 껐다 켜면 데스크톱에서 화면을 클릭했을 때 캐릭터가 움직인다.
    // 포인터 종류로 거르면 항상 켜 두어도 안전하다.
    [RequireComponent(typeof(Graphic))]
    public class TouchMoveZone : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Label("방향 (-1 왼쪽 / +1 오른쪽)")]
        [SerializeField] private float direction = -1f;

        // 같은 영역을 여러 손가락이 눌렀을 때, 하나를 떼도 나머지가 유지되도록
        // 포인터 id를 세어 둔다.
        private readonly HashSet<int> _pointers = new();

        private void OnEnable()
        {
            var graphic = GetComponent<Graphic>();
            if (graphic != null) graphic.raycastTarget = true;
        }

        private void OnDisable() => ReleaseAll();

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!IsTouch(eventData)) return;
            if (!_pointers.Add(eventData.pointerId)) return;
            if (_pointers.Count == 1) GameInput.AddTouchDirection(direction);

            InputMode.ReportTouch();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!_pointers.Remove(eventData.pointerId)) return;
            if (_pointers.Count == 0) GameInput.AddTouchDirection(-direction);
        }

        // 마우스 포인터는 음수 id를 쓴다 (좌 -1 / 우 -2 / 가운데 -3). 터치는 0 이상이다.
        private static bool IsTouch(PointerEventData eventData) => eventData.pointerId >= 0;

        private void ReleaseAll()
        {
            if (_pointers.Count > 0) GameInput.AddTouchDirection(-direction);
            _pointers.Clear();
        }
    }
}
