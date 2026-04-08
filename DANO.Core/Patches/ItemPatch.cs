using DANO.Events;
using HarmonyLib;

namespace DANO.Patches
{
    /// <summary>ItemBehaviour.OnGrab() をフックしてアイテム拾得イベントを発火する</summary>
    [HarmonyPatch(typeof(ItemBehaviour), nameof(ItemBehaviour.OnGrab))]
    internal static class ItemGrabPatch
    {
        private static bool _logged;

        private static void Postfix(ItemBehaviour __instance, bool owner, bool rightHand)
        {
            if (!_logged)
            {
                _logged = true;
                DANOLoader.Log.LogInfo("[ItemGrabPatch] Postfix 初回発火確認！");
            }
            EventBus.Raise(new ItemPickedUpEvent(__instance, owner, rightHand));
        }
    }

    /// <summary>ItemBehaviour.OnDrop() をフックしてアイテム投棄イベントを発火する</summary>
    [HarmonyPatch(typeof(ItemBehaviour), nameof(ItemBehaviour.OnDrop))]
    internal static class ItemDropPatch
    {
        private static bool _logged;

        private static void Prefix(ItemBehaviour __instance)
        {
            if (!_logged)
            {
                _logged = true;
                DANOLoader.Log.LogInfo("[ItemDropPatch] Prefix 初回発火確認！");
            }
            EventBus.Raise(new ItemDroppedEvent(__instance));
        }
    }
}
