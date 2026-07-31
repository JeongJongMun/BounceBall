using System.Collections;
using Core.Events;
using UnityEngine;

namespace Game
{
    // 성질 아이템 (기획 §11). 감지 범위 진입 시 E 키 프롬프트 표시, E 입력으로 성질 획득 후 재생성.
    [RequireComponent(typeof(Collider2D))]
    public class PropertyItem : MonoBehaviour
    {
        [SerializeField] private PropertyData propertyData;
        [SerializeField] private float respawnDelay = 5f;
        [Tooltip("감지 범위 내에 있을 때 표시할 'E' 안내 오브젝트")]
        [SerializeField] private GameObject promptRoot;
        [SerializeField] private VoidEventChannel onAcquired;

        private Collider2D _collider;
        private SpriteRenderer _renderer;
        private PlayerInteraction _currentInteractor;

        public bool IsActive { get; private set; } = true;
        public PropertyData PropertyData => propertyData;

        // 에디터 툴링/테스트에서 지급할 성질과 프롬프트 오브젝트를 지정할 때 사용.
        public void SetData(PropertyData data, GameObject prompt, float respawn = 5f)
        {
            propertyData = data;
            promptRoot = prompt;
            respawnDelay = respawn;
        }

        private void Awake()
        {
            _collider = GetComponent<Collider2D>();
            _renderer = GetComponent<SpriteRenderer>();
            SetPromptVisible(false);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var interaction = other.GetComponent<PlayerInteraction>();
            if (interaction == null) return;
            _currentInteractor = interaction;
            interaction.EnterRange(this);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            var interaction = other.GetComponent<PlayerInteraction>();
            if (interaction == null) return;
            interaction.ExitRange(this);
            if (_currentInteractor == interaction) _currentInteractor = null;
        }

        public void SetPromptVisible(bool visible)
        {
            if (promptRoot != null) promptRoot.SetActive(visible);
        }

        // E 입력으로 성질 획득 처리 (기획 §11.6)
        public void Acquire(PlayerProperty playerProperty)
        {
            if (!IsActive || propertyData == null) return;

            playerProperty.Apply(propertyData);
            onAcquired?.Raise();

            SetPromptVisible(false);
            _currentInteractor?.ExitRange(this);
            _currentInteractor = null;

            Deactivate();
            StartCoroutine(RespawnRoutine());
        }

        private void Deactivate()
        {
            IsActive = false;
            _collider.enabled = false;
            if (_renderer != null) _renderer.enabled = false;
        }

        // 설정된 시간 이후 원래 위치에서 재활성화 (기획 §11.7)
        private IEnumerator RespawnRoutine()
        {
            yield return new WaitForSeconds(respawnDelay);
            IsActive = true;
            _collider.enabled = true;
            if (_renderer != null) _renderer.enabled = true;
        }
    }
}
