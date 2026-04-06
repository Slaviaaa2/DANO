using DANO.Events;
using HarmonyLib;
using UnityEngine;

namespace DANO.Patches
{
    /// <summary>PlayerHealth.RemoveHealth をフックしてダメージイベントを発火する</summary>
    [HarmonyPatch(typeof(PlayerHealth), nameof(PlayerHealth.RemoveHealth))]
    internal static class RemoveHealthPatch
    {
        // Prefixでキャンセル・値の変更が可能
        private static bool Prefix(PlayerHealth __instance, ref float damage)
        {
            var ev = new PlayerDamagedEvent(__instance, damage, __instance.killer);
            EventBus.Raise(ev);

            if (ev.Cancel) return false;

            damage = ev.Damage;
            return true;
        }
    }

    /// <summary>PlayerHealth.ChangeKilledState をフックして死亡イベントを発火する</summary>
    [HarmonyPatch(typeof(PlayerHealth), nameof(PlayerHealth.ChangeKilledState))]
    internal static class ChangeKilledStatePatch
    {
        private static void Prefix(PlayerHealth __instance, bool tempBool)
        {
            // trueになるとき＝死亡
            if (!tempBool) return;

            // isKilledがすでにtrueなら二重発火しない
            if (__instance.isKilled) return;

            // PlayerId は PlayerManager の ClientInstance から取る
            var clientId = __instance.GetComponent<FishNet.Object.NetworkObject>()?.OwnerId ?? -1;
            var ev = new PlayerDiedEvent(__instance, __instance.killer, clientId);
            EventBus.Raise(ev);
        }
    }
}
