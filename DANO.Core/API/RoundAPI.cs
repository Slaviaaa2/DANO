using System.Collections.Generic;
using System.Linq;

namespace DANO.API
{
    /// <summary>ラウンド状態へのアクセスAPI</summary>
    public static class RoundAPI
    {
        /// <summary>現在のテイク（ラウンド内リスポーン回数）</summary>
        public static int CurrentTake =>
            ScoreManager.Instance?.sync___get_value_TakeIndex() ?? 0;

        /// <summary>ラウンド勝利に必要なスコア</summary>
        public static int ScoreToWin =>
            ScoreManager.Instance?.RoundScoreRequiredToWin ?? 0;

        /// <summary>生存プレイヤーID一覧</summary>
        public static IReadOnlyCollection<int> AlivePlayers =>
            (IReadOnlyCollection<int>?)GameManager.Instance?.alivePlayers
            ?? (IReadOnlyCollection<int>)new List<int>();

        /// <summary>生存プレイヤー数</summary>
        public static int AliveCount =>
            GameManager.Instance?.alivePlayers.Count ?? 0;

        /// <summary>チームのマッチポイント（ラウンド勝利数）</summary>
        public static int GetMatchScore(int teamId) =>
            ScoreManager.Instance?.GetPoints(teamId) ?? 0;

        /// <summary>プレイヤーのラウンドスコア</summary>
        public static int GetRoundScore(int playerId) =>
            ScoreManager.Instance?.GetRoundScore(playerId) ?? 0;

        /// <summary>ラウンド勝者が決まったかどうか</summary>
        public static bool HasWinner(out int winningTeamId)
        {
            winningTeamId = -1;
            return ScoreManager.Instance?.CheckForRoundWin(out winningTeamId) ?? false;
        }

        /// <summary>先取モード（firstToXWins）かどうか</summary>
        public static bool IsFirstToXWins =>
            SceneMotor.Instance?.firstToXWins ?? false;

        /// <summary>必要ラウンド勝利数（先取モード時）</summary>
        public static int RoundsToWin
        {
            get
            {
                var sm = SceneMotor.Instance;
                if (sm == null) return 0;
                return sm.sync___get_value_roundAmount();
            }
        }
    }
}
