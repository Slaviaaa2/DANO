using DANO.Events;
using HarmonyLib;

namespace DANO.Patches
{
    /// <summary>ScoreManager.SetTeamId() をフックしてチーム変更イベントを発火する</summary>
    [HarmonyPatch(typeof(ScoreManager), nameof(ScoreManager.SetTeamId))]
    internal static class ScoreManagerPatch
    {
        private static void Prefix(int playerId, int teamId, out int __state)
        {
            // 変更前のチームIDを保存
            __state = -1;
            var scoreManager = ScoreManager.Instance;
            if (scoreManager != null && scoreManager.PlayerIdToTeamId.ContainsKey(playerId))
            {
                __state = scoreManager.PlayerIdToTeamId[playerId];
            }
        }

        private static void Postfix(int playerId, int teamId, int __state)
        {
            // チームが実際に変わった場合のみイベント発火
            if (__state != teamId)
            {
                EventBus.Raise(new TeamChangedEvent(playerId, __state, teamId));
            }
        }
    }
}
