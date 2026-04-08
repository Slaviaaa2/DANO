namespace DANO.Events
{
    /// <summary>プレイヤーがサーバーに接続したときのイベント</summary>
    public class PlayerConnectedEvent
    {
        public int PlayerId { get; }
        public string PlayerName { get; }
        public ulong SteamId { get; }

        internal PlayerConnectedEvent(int playerId, string playerName, ulong steamId)
        {
            PlayerId = playerId;
            PlayerName = playerName;
            SteamId = steamId;
        }
    }

    /// <summary>プレイヤーがサーバーから切断したときのイベント</summary>
    public class PlayerDisconnectedEvent
    {
        public int PlayerId { get; }
        public string PlayerName { get; }

        internal PlayerDisconnectedEvent(int playerId, string playerName)
        {
            PlayerId = playerId;
            PlayerName = playerName;
        }
    }
}
