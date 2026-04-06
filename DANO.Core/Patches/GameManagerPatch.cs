using System;
using BepInEx.Logging;
using DANO.Events;
using HarmonyLib;

namespace DANO.Patches
{
    /// <summary>
    /// FishNet が生成するハッシュ付きメソッド名はバージョン間で変わる可能性がある。
    /// PatchAll に含めず、存在確認してから手動適用する。
    /// </summary>
    internal static class GameManagerPatch
    {
        // FishNet が ObserversRpc に付けるメソッド名（publicized DLL から確認）
        private const string RoundSpawnMethod  = "RpcLogic___ObserversRoundSpawn_2166136261";
        // FishNet が Cmd に付けるメソッド名
        private const string EndRoundMethod    = "RpcLogic___CmdEndRound_3316948804";

        internal static void TryApply(Harmony harmony, ManualLogSource log)
        {
            TryPatch(harmony, log,
                typeof(GameManager), RoundSpawnMethod,
                postfix: new HarmonyMethod(typeof(GameManagerPatch), nameof(RoundSpawnPostfix)));

            TryPatch(harmony, log,
                typeof(RoundManager), EndRoundMethod,
                prefix: new HarmonyMethod(typeof(GameManagerPatch), nameof(EndRoundPrefix)));
        }

        private static void TryPatch(
            Harmony harmony, ManualLogSource log,
            Type targetType, string methodName,
            HarmonyMethod? prefix = null,
            HarmonyMethod? postfix = null)
        {
            var method = AccessTools.Method(targetType, methodName);
            if (method == null)
            {
                log.LogWarning($"[GameManagerPatch] メソッドが見つかりません: {targetType.Name}.{methodName}");
                return;
            }

            try
            {
                harmony.Patch(method, prefix: prefix, postfix: postfix);
                log.LogDebug($"[GameManagerPatch] パッチ適用: {targetType.Name}.{methodName}");
            }
            catch (Exception ex)
            {
                log.LogWarning($"[GameManagerPatch] パッチ失敗: {targetType.Name}.{methodName} — {ex.Message}");
            }
        }

        private static void RoundSpawnPostfix()
        {
            var takeIndex = ScoreManager.Instance?.sync___get_value_TakeIndex() ?? 0;
            EventBus.Raise(new RoundStartedEvent(takeIndex));
        }

        private static void EndRoundPrefix(int winningTeamId)
        {
            EventBus.Raise(new RoundEndedEvent(winningTeamId, isDraw: false));
        }
    }
}
