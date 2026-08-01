namespace Game
{
    // 메인 메뉴 씬을 어떤 화면으로 열지 알려 준다.
    // 스테이지에서 돌아올 때는 메인 화면이 아니라 스테이지 선택 화면을 바로 보여준다.
    public static class MenuNavigation
    {
        public static bool OpenStageSelectOnLoad { get; private set; }

        public static void RequestStageSelect() => OpenStageSelectOnLoad = true;

        // 메뉴 씬이 요청을 소비한다. 이후 메인 화면으로 돌아가면 다시 기본값이 된다.
        public static bool ConsumeStageSelectRequest()
        {
            if (!OpenStageSelectOnLoad) return false;
            OpenStageSelectOnLoad = false;
            return true;
        }
    }
}
