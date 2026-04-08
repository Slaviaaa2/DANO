using DANO.Events;
using HarmonyLib;

namespace DANO.Patches
{
    /// <summary>ItemBehaviour.OnGrab() をフックしてアイテム拾得イベントを発火する</summary>
    [HarmonyPatch(typeof(ItemBehaviour), nameof(ItemBehaviour.OnGrab))]
    internal static class ItemGrabPatch
    {
        private static void Postfix(ItemBehaviour __instance, bool owner, bool rightHand)
        {
            EventBus.Raise(new ItemPickedUpEvent(__instance, owner, rightHand));
        }
    }

    /// <summary>ItemBehaviour.OnDrop() をフックしてアイテム投棄イベントを発火する</summary>
    [HarmonyPatch(typeof(ItemBehaviour), nameof(ItemBehaviour.OnDrop))]
    internal static class ItemDropPatch
    {
        private static void Prefix(ItemBehaviour __instance)
        {
            EventBus.Raise(new ItemDroppedEvent(__instance));
        }
    }
}
