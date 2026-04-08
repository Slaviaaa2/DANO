using System.Collections.Generic;
using System.Linq;

namespace DANO.API
{
    /// <summary>サーバー/ロビー情報へのアクセスAPI</summary>
    public static class ServerAPI
    {
        /// <summary>最大プレイヤー数</summary>
        public static int MaxPlayers =>
            SteamLobby.Instance?.maxPlayers ?? 0;

        /// <summary>現在のプレイヤー数</summary>
        public static int PlayerCount =>
            ClientInstance.playerInstances.Count;

        /// <summary>ロビー名</summary>
        public static string LobbyName =>
            SteamLobby.Instance?.lobbyName ?? "";

        /// <summary>ロビーIDを取得</summary>
        public static ulong LobbyId =>
            SteamLobby.Instance?.CurrentLobbyID ?? 0;

        /// <summary>ロビーに参加中かどうか</summary>
        public static bool InLobby =>
            SteamLobby.Instance?.inSteamLobby ?? false;

        /// <summary>チームモードかどうか</summary>
        public static bool IsTeamMode =>
            GameManager.Instance?.playingTeams ?? false;

        /// <summary>ゲームが開始されたかどうか</summary>
        public static bool GameStarted =>
            PauseManager.Instance?.gameStarted ?? false;

        /// <summary>ポーズ中かどうか</summary>
        public static bool IsPaused =>
            PauseManager.Instance?.pause ?? false;

        /// <summary>ラウンド間かどうか</summary>
        public static bool BetweenRounds =>
            PauseManager.BetweenRounds;

        /// <summary>勝利画面かどうか</summary>
        public static bool InVictoryMenu =>
            PauseManager.Instance?.inVictoryMenu ?? false;

        /// <summary>メインメニューかどうか</summary>
        public static bool InMainMenu =>
            PauseManager.Instance?.inMainMenu ?? false;
    }
}
