using DANO.Events;
using HarmonyLib;

namespace DANO.Patches
{
    /// <summary>Weapon.OnReload() をフックしてリロードイベントを発火する</summary>
    [HarmonyPatch(typeof(Weapon), nameof(Weapon.OnReload))]
    internal static class WeaponReloadPatch
    {
        private static void Postfix(Weapon __instance)
        {
            EventBus.Raise(new WeaponReloadEvent(__instance));
        }
    }

    /// <summary>MeleeWeapon.HitServer() をフックして近接ヒットイベントを発火する</summary>
    [HarmonyPatch(typeof(MeleeWeapon), nameof(MeleeWeapon.HitServer))]
    internal static class MeleeHitPatch
    {
        private static void Prefix(MeleeWeapon __instance, PlayerHealth enemyHealth)
        {
            EventBus.Raise(new MeleeHitEvent(__instance, enemyHealth));
        }
    }
}
