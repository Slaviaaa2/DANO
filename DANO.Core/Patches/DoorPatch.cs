using DANO.Events;
using HarmonyLib;
using UnityEngine;

namespace DANO.Patches
{
    /// <summary>Door.OnInteract をフックしてドアイベントを発火する</summary>
    [HarmonyPatch(typeof(Door), nameof(Door.OnInteract))]
    internal static class DoorInteractPatch
    {
        private static bool _logged;

        private static bool Prefix(Door __instance, Transform player)
        {
            if (!_logged)
            {
                _logged = true;
                DANOLoader.Log.LogInfo("[DoorInteractPatch] Prefix 初回発火確認！");
            }
            var ev = new DoorInteractEvent(__instance, player);
            EventBus.Raise(ev);
            return !ev.Cancel;
        }
    }
}
