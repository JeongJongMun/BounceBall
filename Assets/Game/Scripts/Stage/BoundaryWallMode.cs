namespace Game
{
    // 투명 벽 위치를 정하는 방식
    public enum BoundaryWallMode
    {
        // 카메라 경계에서 자동으로 계산한다
        FromBounds,
        // 좌우 X 좌표를 직접 입력한다
        Explicit
    }
}
