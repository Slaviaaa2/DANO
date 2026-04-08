using System.Reflection;
using DANO.Events;
using HarmonyLib;
using UnityEngine;

namespace DANO.Patches
{
    /// <summary>
    /// PlayerHealth のダメージ/死亡パ��チ。
    /// FishNet [ServerRpc] メソッドは Harmony パッチをバイパスするため、
    /// 生成された RpcLogic___ メソッドを直接パッチする。
    /// </summary>
    internal static class PlayerHealthPatches
    {
        internal static void TryApply(Harmony harmony, BepInEx.Logging.ManualLogSource log)
        {
            // RemoveHealth → RpcLogic___RemoveHealth_431000436
            var removeHealthLogic = AccessTools.Method(typeof(PlayerHealth), "RpcLogic___RemoveHealth_431000436");
            if (removeHealthLogic != null)
            {
                harmony.Patch(removeHealthLogic,
                    prefix: new HarmonyMethod(typeof(PlayerHealthPatches), nameof(RemoveHealthPrefix)));
                log.LogInfo("[DANOLoader] パッチ適用成功: PlayerHealth.RpcLogic___RemoveHealth (FishNet)");
            }
            else
            {
                log.LogWarning("[DANOLoader] PlayerHealth.RpcLogic___RemoveHealth が見つかりません");
            }

            // ChangeKilledState → RpcLogic___ChangeKilledState_1140765316
            var changeKilledLogic = AccessTools.Method(typeof(PlayerHealth), "RpcLogic___ChangeKilledState_1140765316");
            if (changeKilledLogic != null)
            {
                harmony.Patch(changeKilledLogic,
                    prefix: new HarmonyMethod(typeof(PlayerHealthPatches), nameof(ChangeKilledStatePrefix)));
                log.LogInfo("[DANOLoader] パッチ適用成功: PlayerHealth.RpcLogic___ChangeKilledState (FishNet)");
            }
            else
            {
                log.LogWarning("[DANOLoader] PlayerHealth.RpcLogic___ChangeKilledState が見つかりません");
            }
        }

        private static bool RemoveHealthPrefix(PlayerHealth __instance, ref float damage)
        {
            DANOLoader.Log.LogInfo($"[RemoveHealthPatch] Prefix 発火！ damage={damage}");
            var ev = new PlayerDamagedEvent(__instance, damage, __instance.killer);
            EventBus.Raise(ev);

            if (ev.Cancel) return false;
            damage = ev.Damage;
            return true;
        }

        private static void ChangeKilledStatePrefix(PlayerHealth __instance, bool tempBool)
        {
            if (!tempBool) return;
            if (__instance.isKilled) return;

            DANOLoader.Log.LogInfo("[ChangeKilledStatePatch] Prefix 発火！");
            var ev = new PlayerDiedEvent(__instance, __instance.killer);
            EventBus.Raise(ev);
        }
    }
}
