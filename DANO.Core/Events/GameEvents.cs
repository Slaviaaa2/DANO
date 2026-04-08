namespace DANO.Events
{
    /// <summary>ラウンドが始まったときのイベントデータ</summary>
    public class RoundStartedEvent
    {
        public int TakeIndex { get; }

        internal RoundStartedEvent(int takeIndex)
        {
            TakeIndex = takeIndex;
        }
    }

    /// <summary>ラウンドが終わったときのイベントデータ</summary>
    public class RoundEndedEvent
    {
        public int WinningTeamId { get; }
        /// <summary>引き分けの場合はtrue</summary>
        public bool IsDraw { get; }

        internal RoundEndedEvent(int winningTeamId, bool isDraw)
        {
            WinningTeamId = winningTeamId;
            IsDraw = isDraw;
        }
    }

    /// <summary>マッチ（ラウンド全体）が終わったときのイベントデータ</summary>
    public class MatchEndedEvent
    {
        public int WinningTeamId { get; }

        internal MatchEndedEvent(int winningTeamId)
        {
            WinningTeamId = winningTeamId;
        }
    }

    /// <summary>スポーンフェーズ（ラウンド間のリスポーン待ち）が始まったときのイベント</summary>
    public class SpawnPhaseStartedEvent
    {
        internal SpawnPhaseStartedEvent() { }
    }

    /// <summary>ゲームが開始されたときのイベント（ロビーからゲームへ遷移）</summary>
    public class GameStartedEvent
    {
        internal GameStartedEvent() { }
    }
}
