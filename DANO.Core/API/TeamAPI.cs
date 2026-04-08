using System.Collections.Generic;

namespace DANO.API
{
    /// <summary>チーム情報へのアクセスAPI</summary>
    public static class TeamAPI
    {
        /// <summary>プレイヤーのチームIDを返す</summary>
        public static int GetTeamId(int playerId) =>
            ScoreManager.Instance?.GetTeamId(playerId) ?? -1;

        /// <summary>指定チームの全メンバーIDを返す</summary>
        public static List<int> GetTeamMembers(int teamId)
        {
            var members = new List<int>();
            var sm = ScoreManager.Instance;
            if (sm == null) return members;

            foreach (var kvp in sm.PlayerIdToTeamId)
            {
                if (kvp.Value == teamId)
                    members.Add(kvp.Key);
            }
            return members;
        }

        /// <summary>現在使用中のチームIDを返す</summary>
        public static List<int> GetActiveTeams()
        {
            var teams = new List<int>();
            var sm = ScoreManager.Instance;
            if (sm == null) return teams;

            foreach (var kvp in sm.PlayerIdToTeamId)
            {
                if (!teams.Contains(kvp.Value))
                    teams.Add(kvp.Value);
            }
            return teams;
        }

        /// <summary>2人のプレイヤーが同じチームかどうか</summary>
        public static bool AreAllies(int playerIdA, int playerIdB) =>
            GetTeamId(playerIdA) == GetTeamId(playerIdB) && GetTeamId(playerIdA) != -1;

        /// <summary>チームモードが有効かどうか</summary>
        public static bool IsTeamMode() =>
            GameManager.Instance != null && GameManager.Instance.playingTeams;
    }
}
