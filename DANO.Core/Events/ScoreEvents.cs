namespace DANO.Events
{
    /// <summary>チームのマッチポイントが変更されたときのイベント</summary>
    public class MatchScoreChangedEvent
    {
        /// <summary>変更されたチームID</summary>
        public int TeamId { get; }
        /// <summary>変更前のスコア</summary>
        public int OldScore { get; }
        /// <summary>変更後のスコア</summary>
        public int NewScore { get; }

        internal MatchScoreChangedEvent(int teamId, int oldScore, int newScore)
        {
            TeamId = teamId;
            OldScore = oldScore;
            NewScore = newScore;
        }
    }

    /// <summary>チームのラウンドスコアが変更されたときのイベント</summary>
    public class RoundScoreChangedEvent
    {
        /// <summary>変更されたチームID</summary>
        public int TeamId { get; }
        /// <summary>変更前のスコア</summary>
        public int OldScore { get; }
        /// <summary>変更後のスコア</summary>
        public int NewScore { get; }

        internal RoundScoreChangedEvent(int teamId, int oldScore, int newScore)
        {
            TeamId = teamId;
            OldScore = oldScore;
            NewScore = newScore;
        }
    }
}
