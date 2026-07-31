namespace Game
{
    // 플레이어 성질 (기획 §10.1)
    public enum PlayerPropertyType { Default, Jelly, Ice }

    // 타일 성질 (기획 §10.2). SpecialTile이 없는 타일은 Default로 간주한다.
    public enum TilePropertyType { Default, Jelly, Ice }

    // 성질 × 타일 조합의 결과 (기획 §10.3, §8)
    public enum PropertyInteractionType { NormalJump, LowJump, HighJump, Attach, Slide }
}
