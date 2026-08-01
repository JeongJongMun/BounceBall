namespace Game
{
    // 한국어 조사 선택. 앞말의 받침 유무에 따라 을/를, 이/가 등을 고른다.
    public static class KoreanParticle
    {
        private const char HangulStart = '가';
        private const char HangulEnd = '힣';
        private const int JongseongCount = 28; // 종성 개수(없음 포함)

        // 마지막 글자에 받침이 있는가. 한글 음절이 아니면 false로 본다.
        public static bool HasFinalConsonant(string word)
        {
            if (string.IsNullOrEmpty(word)) return false;

            char last = word[word.Length - 1];
            if (last < HangulStart || last > HangulEnd) return false;

            return (last - HangulStart) % JongseongCount != 0;
        }

        // 목적격 조사: 받침 있으면 "을", 없으면 "를"
        public static string Object(string word) => HasFinalConsonant(word) ? "을" : "를";

        // 주격 조사: 받침 있으면 "이", 없으면 "가"
        public static string Subject(string word) => HasFinalConsonant(word) ? "이" : "가";

        public static string WithObject(string word) => word + Object(word);
    }
}
