namespace Game
{
    // 사운드 기획 문서의 최종 사운드 목록. 파일명과 같은 이름을 쓴다.
    // Jelly_Move·Ice_Slide는 반복 재생용이며 파일이 준비되면 채운다.
    public enum SoundId
    {
        None = 0,

        // BGM
        Main_BGM,
        Stage_BGM,

        // UI
        UI_Click,
        UI_Error,

        // 플레이어
        Bounce_Normal,
        Bounce_Jelly,
        Bounce_Ice,
        Jelly_Move,
        Ice_Slide,
        Lick,

        // 아이템
        Coin,
        Goal_Item,
        Property_Change,

        // 기믹
        CheckPoint,
        SuperJump,

        // 값이 SoundDatabase에 int로 저장되므로 새 사운드는 항상 뒤에 추가한다.
        // 중간에 끼우면 기존 항목의 클립 연결이 어긋난다.
        Dead // 플레이어 사망
    }
}
