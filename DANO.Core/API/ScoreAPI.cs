namespace DANO.API
{
    /// <summary>スコア情報へのアクセスAPI</summary>
    public static class ScoreAPI
    {
        /// <summary>チームのマッチポイント（ラウンド勝利数）を返す</summary>
        public static int GetMatchScore(int teamId) =>
            ScoreManager.Instance?.GetPoints(teamId) ?? 0;

        /// <summary>プレイヤーのラウンドスコアを返す</summary>
        public static int GetRoundScore(int playerId) =>
            ScoreManager.Instance?.GetRoundScore(playerId) ?? 0;

        /// <summary>現在のテイク（ラウンド内のリスポーン回数）を返す</summary>
        public static int GetCurrentTake() =>
            ScoreManager.Instance?.sync___get_value_TakeIndex() ?? 0;

        /// <summary>ラウンド勝利に必要なスコアを返す</summary>
        public static int GetRoundScoreToWin() =>
            ScoreManager.Instance?.RoundScoreRequiredToWin ?? 0;

        /// <summary>指定チームがラウンドに勝利したかどうか</summary>
        public static bool HasTeamWonRound(int teamId)
        {
            var sm = ScoreManager.Instance;
            if (sm == null) return false;

            return sm.CheckForRoundWin(out int winningTeamId) && winningTeamId == teamId;
        }

        // ─── スコア操作（サーバー側） ───

        /// <summary>チームにマッチポイントを加算する</summary>
        public static void AddMatchPoints(int teamId, int points = 1) =>
            ScoreManager.Instance?.AddPoints(teamId, points);

        /// <summary>プレイヤーにラウンドスコアを加算する</summary>
        public static void AddRoundScore(int playerId, int points = 1) =>
            ScoreManager.Instance?.AddRoundScore(playerId, points);

        /// <summary>ラウンドスコアをリセットする</summary>
        public static void ResetRound() =>
            ScoreManager.Instance?.ResetRound();

        /// <summary>テイクインデックス（ラウンド番号）を設定する</summary>
        public static void SetRoundIndex(int index) =>
            ScoreManager.Instance?.SetRoundIndex(index);

        /// <summary>全チーム割り当てをリセットする</summary>
        public static void ResetTeams() =>
            ScoreManager.Instance?.ResetTeams();
    }
}
