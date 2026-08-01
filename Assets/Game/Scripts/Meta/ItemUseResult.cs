namespace Game
{
    // 아이템 사용 시도의 결과. 실패 사유를 구분해 플레이어에게 이유를 알려 준다.
    // (예전에는 bool 하나였어서 왜 안 되는지 알 수 없었다)
    public enum ItemUseResult
    {
        Success,

        // 안내가 필요한 실패 — 플레이어가 고칠 수 있는 상황이다
        NotInGame,   // 스테이지 선택 화면 등 인게임이 아님 (UI 기획서 §2.9, §5.3)
        NotUsable,   // 인게임에서 쓸 수 없는 아이템

        // 조용히 무시하는 실패 — 잠깐 지나가는 상태라 안내하면 오히려 성가시다
        PlayerBusy,    // 사망·부활 연출 중
        StageCleared,  // 클리어 처리 중
        Failed         // 그 밖의 이유 (수량 없음, 효과 적용 실패 등)
    }
}
