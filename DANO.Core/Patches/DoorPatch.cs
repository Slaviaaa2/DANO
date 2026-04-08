using DANO.Events;
using HarmonyLib;
using UnityEngine;

namespace DANO.Patches
{
    /// <summary>Door.OnInteract をフックしてドアイベントを発火する</summary>
    [HarmonyPatch(typeof(Door), nameof(Door.OnInteract))]
    internal static class DoorInteractPatch
    {
        private static bool Prefix(Door __instance, Transform player)
        {
            var ev = new DoorInteractEvent(__instance, player);
            EventBus.Raise(ev);
            return !ev.Cancel;
        }
    }
}
