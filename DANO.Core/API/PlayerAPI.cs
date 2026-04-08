using System.Collections.Generic;
using System.Linq;

namespace DANO.API
{
    /// <summary>プレイヤー情報へのアクセスAPI</summary>
    public static class PlayerAPI
    {
        /// <summary>接続中の全プレイヤー</summary>
        public static IEnumerable<API.Player> All => API.Player.List;

        /// <summary>ローカルプレイヤー</summary>
        public static API.Player? Local => API.Player.Local;

        /// <summary>IDからPlayerを取得</summary>
        public static API.Player? Get(int playerId) => API.Player.Get(playerId);

        /// <summary>名前からプレイヤーを検索（部分一致、大文字小文字無視）</summary>
        public static API.Player? GetByName(string name) =>
            API.Player.List.FirstOrDefault(p =>
                p.Name.IndexOf(name, System.StringComparison.OrdinalIgnoreCase) >= 0);

        /// <summary>SteamID からプレイヤーを取得</summary>
        public static API.Player? GetBySteamId(ulong steamId) =>
            API.Player.List.FirstOrDefault(p => p.SteamId == steamId);

        /// <summary>指定プレイヤーが生きているか</summary>
        public static bool IsAlive(int playerId) =>
            API.Player.Get(playerId)?.IsAlive ?? false;

        /// <summary>指定プレイヤーのチームIDを返す</summary>
        public static int GetTeamId(int playerId) =>
            API.Player.Get(playerId)?.TeamId ?? -1;

        /// <summary>接続中のプレイヤー数</summary>
        public static int Count => API.Player.List.Count();

        /// <summary>生存中のプレイヤー一覧</summary>
        public static IEnumerable<API.Player> Alive =>
            API.Player.List.Where(p => p.IsAlive);

        /// <summary>指定チームのプレイヤー一覧</summary>
        public static IEnumerable<API.Player> GetTeamMembers(int teamId) =>
            API.Player.List.Where(p => p.TeamId == teamId);

        /// <summary>ホスト（サーバー）プレイヤーかどうか</summary>
        public static bool IsHost(API.Player player) =>
            player.Base.IsHost;
    }
}
