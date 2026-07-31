using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game
{
    // 성질 아이템 감지 + E 입력 획득 (기획 §11). 이동/성질 시스템과 분리된 상호작용 전담 컴포넌트.
    [RequireComponent(typeof(Player), typeof(PlayerProperty))]
    public class PlayerInteraction : MonoBehaviour
    {
        [Tooltip("끄면 키보드를 읽지 않는다 (테스트/컷씬용)")]
        [SerializeField] private bool readKeyboard = true;

        private readonly List<PropertyItem> _inRange = new();

        private Player _player;
        private PlayerProperty _property;
        private bool _promptSuppressed;

        public PropertyItem CurrentInteractable { get; private set; }

        public bool ReadKeyboard
        {
            get => readKeyboard;
            set => readKeyboard = value;
        }

        private void Awake()
        {
            _player = GetComponent<Player>();
            _property = GetComponent<PlayerProperty>();
        }

        private void Update()
        {
            // 클리어·낙사 연출 중에는 획득이 막히므로 안내 UI도 남기지 않는다 (기획 §22.2).
            // 범위 목록은 유지했다가 조작이 돌아오면 복구한다 (부활 후 재진입 없이도 다시 뜨도록).
            bool disabled = _player.State == PlayerState.Disabled;
            if (disabled != _promptSuppressed)
            {
                _promptSuppressed = disabled;
                if (disabled) CurrentInteractable?.SetPromptVisible(false);
                else UpdateInteractable();
            }
            if (disabled) return;

            if (!readKeyboard || Keyboard.current == null) return;
            if (Keyboard.current.eKey.wasPressedThisFrame) TryAcquire();
        }

        // 테스트 및 외부 제어용. readKeyboard가 꺼져 있을 때 사용.
        public void TryAcquire()
        {
            if (_player.State == PlayerState.Disabled) return;
            CurrentInteractable?.Acquire(_property);
        }

        // 부활 시 상호작용 대상과 안내 UI를 초기화한다 (기획 §24.3).
        // 텔레포트는 OnTriggerExit2D를 확실히 발생시키지 않아, 안 지우면 멀리 있는 아이템이 최근접으로 남는다.
        public void ClearRange()
        {
            CurrentInteractable?.SetPromptVisible(false);
            CurrentInteractable = null;
            foreach (var item in _inRange) item?.SetPromptVisible(false);
            _inRange.Clear();
        }

        public void EnterRange(PropertyItem item)
        {
            if (item == null || _inRange.Contains(item)) return;
            _inRange.Add(item);
            UpdateInteractable();
        }

        public void ExitRange(PropertyItem item)
        {
            if (_inRange.Remove(item)) UpdateInteractable();
        }

        // 감지 범위 내 아이템 중 가장 가까운 것을 상호작용 대상으로 선택 (기획 §11.4)
        private void UpdateInteractable()
        {
            PropertyItem nearest = null;
            float best = float.MaxValue;
            foreach (var item in _inRange)
            {
                if (item == null || !item.IsActive) continue;
                float sqrDist = (item.transform.position - transform.position).sqrMagnitude;
                if (sqrDist < best)
                {
                    best = sqrDist;
                    nearest = item;
                }
            }

            if (nearest == CurrentInteractable) return;

            CurrentInteractable?.SetPromptVisible(false);
            CurrentInteractable = nearest;
            if (!_promptSuppressed) CurrentInteractable?.SetPromptVisible(true);
        }
    }
}
