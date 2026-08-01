using UnityEngine;

namespace Game
{
    // 소비형 아이템을 사용했을 때 일어나는 일.
    // 아이템마다 효과가 다르므로 ItemUseService가 종류를 알 필요 없도록 데이터로 분리한다.
    // 새 아이템을 추가할 때 이 클래스를 상속한 에셋만 만들면 된다.
    public abstract class ItemEffect : ScriptableObject
    {
        // 효과를 적용했으면 true. false를 돌려주면 아이템 수량이 차감되지 않는다 (문서 §2.2).
        public abstract bool TryApply(Player player);
    }
}
