using UnityEngine;

namespace Game
{
    // 성질 데이터 (기획 §2, §11).
    // 점프력은 성질별 값을 쓰지 않는다 (§4) — 물리 결과는 PropertyInteractionTable이 결정하므로
    // 여기엔 성질 식별자와 외형만 둔다.
    [CreateAssetMenu(menuName = "Game/Property Data", fileName = "NewPropertyData")]
    public class PropertyData : ScriptableObject
    {
        [SerializeField] private PlayerPropertyType propertyType = PlayerPropertyType.Default;
        [SerializeField] private string displayName;

        [Header("외형 (기획 §11)")]
        [SerializeField] private Color characterColor = Color.white;

        public PlayerPropertyType PropertyType => propertyType;
        public string DisplayName => displayName;
        public Color CharacterColor => characterColor;

        public void SetData(PlayerPropertyType type, string name, Color color)
        {
            propertyType = type;
            displayName = name;
            characterColor = color;
        }
    }
}
