using UnityEngine;

namespace Game
{
    // 플레이어의 성질을 바꾼다 (성질 변화 아이템 3종).
    [CreateAssetMenu(menuName = "Game/Item Effect/성질 변화", fileName = "Effect_Property")]
    public class PropertyChangeEffect : ItemEffect
    {
        [Label("적용할 성질")]
        [SerializeField] private PropertyData grantedProperty;

        public PropertyData GrantedProperty => grantedProperty;

        public override bool TryApply(Player player)
        {
            if (grantedProperty == null) return false;

            var playerProperty = player.GetComponent<PlayerProperty>();
            if (playerProperty == null) return false;

            player.GetComponent<PlayerSpineView>()?.PlayEat();
            playerProperty.Apply(grantedProperty);
            Sound.Play(SoundId.Property_Change);
            return true;
        }
    }
}
