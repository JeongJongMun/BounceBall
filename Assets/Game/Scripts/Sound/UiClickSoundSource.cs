using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game
{
    // 버튼 하나의 클릭음. 누르는 순간(PointerDown) 재생한다.
    // uGUI의 onClick은 버튼을 '뗄 때' 발생해 클릭을 누르고 있는 시간(보통 60~150ms)만큼
    // 늦게 들리므로, 소리는 onClick에 걸지 않고 여기서 낸다.
    // 부착은 UiClickSound가 대신 해 주므로 손으로 붙일 필요는 없다.
    public class UiClickSoundSource : MonoBehaviour, IPointerDownHandler
    {
        [Tooltip("이 버튼을 누를 때 낼 소리. 실패를 알리는 버튼은 UI_Error로 바꾼다")]
        public SoundId sound = SoundId.UI_Click;

        [Tooltip("켜면 이 버튼은 소리를 내지 않는다")]
        public bool mute;

        private Selectable _selectable;

        private void Awake() => _selectable = GetComponent<Selectable>();

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (mute) return;

            // 비활성 버튼은 소리를 내지 않는다
            if (_selectable == null || !_selectable.interactable) return;

            Sound.Play(sound);
        }
    }
}
