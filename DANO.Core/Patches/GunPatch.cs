using DANO.Events;
using HarmonyLib;

namespace DANO.Patches
{
    /// <summary>Gun.Fire() をフックして WeaponFiredEvent を発火する</summary>
    [HarmonyPatch(typeof(Gun), "Fire")]
    internal static class GunFirePatch
    {
        private static bool _logged;

        private static bool Prefix(Gun __instance)
        {
            if (!_logged)
            {
                _logged = true;
                DANOLoader.Log.LogInfo("[GunFirePatch] Prefix 初回発火確認！");
            }

            var owner = ClientInstance.Instance;
            var ev = new WeaponFiredEvent(__instance, owner);
            EventBus.Raise(ev);

            return !ev.Cancel;
        }
    }
}
