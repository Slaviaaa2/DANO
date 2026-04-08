using System.Collections.Generic;
using DANO.Events;
using HarmonyLib;

namespace DANO.Patches
{
    /// <summary>
    /// SceneMotor.ServerEndGameScene() にパッチを適用し、
    /// マッチ終了時に MatchEndedEvent を発火させる。
    /// 注意: サーバー（ホスト）側でのみ発火する。
    /// </summary>
    [HarmonyPatch(typeof(SceneMotor), nameof(SceneMotor.ServerEndGameScene))]
    internal static class SceneMotorPatch
    {
        private static bool _logged;

        private static void Prefix()
        {
            if (!_logged)
            {
                _logged = true;
                DANOLoader.Log.LogInfo("[SceneMotorPatch] Prefix 初回発火確認！");
            }
            int winningTeamId = -1;

            var scoreManager = ScoreManager.Instance;
            if (scoreManager != null)
            {
                // Points ディクショナリから最高スコアのチームを算出
                int highestScore = -1;
                foreach (KeyValuePair<int, int> kvp in scoreManager.Points)
                {
                    if (kvp.Value > highestScore)
                    {
                        highestScore = kvp.Value;
                        winningTeamId = kvp.Key;
                    }
                }
            }

            EventBus.Raise(new MatchEndedEvent(winningTeamId));
        }
    }
}
