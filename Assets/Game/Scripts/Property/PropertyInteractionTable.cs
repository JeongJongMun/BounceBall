namespace Game
{
    // 성질 × 타일 상호작용 표 (기획 §3.1).
    //
    //            기본타일       젤리타일       얼음타일
    //  기본      NormalJump    LowJump       LowJump
    //  젤리      HighJump      Attach        NormalJump
    //  얼음      LowJump       NormalJump    Slide
    //
    // 점프력은 성질별 값이 아니라 공용 3값(일반/감소/증가)을 쓴다 (기획 §4).
    public static class PropertyInteractionTable
    {
        public static PropertyInteractionType Resolve(PlayerPropertyType player, TilePropertyType tile)
        {
            switch (player)
            {
                case PlayerPropertyType.Jelly:
                    switch (tile)
                    {
                        case TilePropertyType.Jelly: return PropertyInteractionType.Attach;
                        case TilePropertyType.Ice: return PropertyInteractionType.NormalJump;
                        default: return PropertyInteractionType.HighJump;
                    }

                case PlayerPropertyType.Ice:
                    switch (tile)
                    {
                        case TilePropertyType.Ice: return PropertyInteractionType.Slide;
                        case TilePropertyType.Jelly: return PropertyInteractionType.NormalJump;
                        default: return PropertyInteractionType.LowJump;
                    }

                default: // 기본 성질
                    return tile == TilePropertyType.Default
                        ? PropertyInteractionType.NormalJump
                        : PropertyInteractionType.LowJump;
            }
        }
    }
}
