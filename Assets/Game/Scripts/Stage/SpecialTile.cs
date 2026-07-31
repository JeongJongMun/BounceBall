using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Game
{
    // 플레이어의 현재 성질에 따라 다르게 반응하는 타일.
    // 구체 효과 목록은 기믹 구현서에서 확정하며, 여기서는 데이터 구조만 정의한다.
    [CreateAssetMenu(menuName = "Game/Special Tile", fileName = "NewSpecialTile")]
    public class SpecialTile : Tile
    {
        [Serializable]
        public class Reaction
        {
            [Tooltip("반응할 성질 태그. 빈 문자열 = 모든 성질에 대한 기본 반응")]
            public string propertyTag = "";
            [Tooltip("효과 식별자 (SpecialTileEffects 상수 참조)")]
            public string effectId = "";
            [Tooltip("효과 파라미터 (배율 등)")]
            public float value = 1f;
        }

        [SerializeField] private string tileId;
        [SerializeField] private List<Reaction> reactions = new();

        public string TileId => tileId;

        public void SetData(string id, List<Reaction> list)
        {
            tileId = id;
            reactions = list;
        }

        // 성질 태그와 일치하는 반응 → 없으면 기본 반응(빈 태그) → 없으면 null
        public Reaction GetReaction(string propertyTag)
        {
            Reaction fallback = null;
            foreach (var reaction in reactions)
            {
                if (reaction.propertyTag == propertyTag) return reaction;
                if (string.IsNullOrEmpty(reaction.propertyTag)) fallback ??= reaction;
            }
            return fallback;
        }
    }

    // 특수 타일 효과 식별자. 기믹 구현서 확정 시 추가된다.
    public static class SpecialTileEffects
    {
        public const string JumpMultiplier = "JumpMultiplier";
    }
}
