using DANO.Events;
using HarmonyLib;

namespace DANO.Patches
{
    /// <summary>Gun.Fire() をフックして WeaponFiredEvent を発火する</summary>
    [HarmonyPatch(typeof(Gun), "Fire")]
    internal static class GunFirePatch
    {
        private static bool Prefix(Gun __instance)
        {
            // 発射元のClientInstanceを探す（rootObjectのPickupスクリプト経由）
            var owner = ClientInstance.Instance; // 発射は常にローカルオーナー

            var ev = new WeaponFiredEvent(__instance, owner);
            EventBus.Raise(ev);

            return !ev.Cancel;
        }
    }
}
