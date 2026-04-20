using System.Collections.Generic;
using System.Linq;
using FishNet.Object;

namespace DANO.API
{
    /// <summary>
    /// ゲーム・ラウンド制御。
    /// サーバー（ホスト）側でのみ効果がある操作が多い。
    /// </summary>
    public static class Game
    {
        // ─── ラウンド制御 ───

        /// <summary>ラウンドを強制終了し、勝利チームを指定する</summary>
        public static void ForceEndRound(int winningTeamId)
        {
            RoundManager.Instance?.CmdEndRound(winningTeamId);
        }

        /// <summary>次のテイク（ラウンド）に進む</summary>
        public static void ProgressToNextTake()
        {
            GameManager.Instance?.ProgressToNextTake();
        }

        /// <summary>ゲームをリセットする（スコア・ラウンド状態クリア）</summary>
        public static void ResetGame()
        {
            GameManager.Instance?.ResetGame();
        }

        /// <summary>チームをシャッフルする</summary>
        public static void ScrambleTeams()
        {
            if (GameManager.Instance == null || ScoreManager.Instance == null) return;

            var setup = new Dictionary<int, List<int>>();
            foreach (var ci in ClientInstance.playerInstances.Values)
            {
                if (ci == null) continue;
                var teamId = ScoreManager.Instance.GetTeamId(ci.PlayerId);
                if (!setup.ContainsKey(teamId))
                    setup[teamId] = new List<int>();
                setup[teamId].Add(ci.PlayerId);
            }

            GameManager.Instance.ScrambleTeams(setup);
        }

        // ─── プレイヤー管理 ───

        /// <summary>プレイヤーをサーバーからキックする</summary>
        public static void KickPlayer(Player player, string message = "")
        {
            if (GameManager.Instance == null || player?.Base == null) return;
            var conn = player.Base.GetComponent<NetworkObject>()?.Owner;
            if (conn != null)
                GameManager.Instance.CmdKickPlayer(conn, message);
        }

        /// <summary>プレイヤーIDでキックする</summary>
        public static void KickPlayer(int playerId, string message = "")
        {
            var player = Player.Get(playerId);
            if (player != null)
                KickPlayer(player, message);
        }

        // ─── ゲーム状態 ───

        /// <summary>生存中のチームIDリスト</summary>
        public static int[] GetAliveTeams()
        {
            if (GameManager.Instance == null) return System.Array.Empty<int>();

            var teams = new System.Collections.Generic.HashSet<int>();
            foreach (var pid in GameManager.Instance.alivePlayers)
            {
                if (ScoreManager.Instance != null)
                    teams.Add(ScoreManager.Instance.GetTeamId(pid));
            }
            return teams.ToArray();
        }

        /// <summary>接続中のチームIDリスト</summary>
        public static int[] GetConnectedTeams() =>
            GameManager.Instance?.GetConnectedTeams() ?? System.Array.Empty<int>();

        /// <summary>生存中のプレイヤーIDリスト</summary>
        public static IEnumerable<int> AlivePlayerIds =>
            GameManager.Instance?.alivePlayers ?? Enumerable.Empty<int>();

        /// <summary>生存中のプレイヤーリスト</summary>
        public static IEnumerable<Player> AlivePlayers =>
            AlivePlayerIds.Select(id => Player.Get(id)).Where(p => p != null)!;

        /// <summary>生存プレイヤー数</summary>
        public static int AliveCount =>
            GameManager.Instance?.alivePlayers.Count ?? 0;
    }
}
