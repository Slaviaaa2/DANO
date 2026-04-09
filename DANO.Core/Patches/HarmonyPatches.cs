using System.Collections.Generic;
using DANO.Events;
using HarmonyLib;
using UnityEngine;

namespace DANO.Patches
{
    // ═══════════════════════════════════════════════
    //  アイテム拾得/投棄 — Harmony で直接検出
    // ═══════════════════════════════════════════════

    [HarmonyPatch(typeof(ItemBehaviour), nameof(ItemBehaviour.OnGrab))]
    internal static class ItemOnGrabPatch
    {
        // OnDrop 時には Holder が既に null になっているため、OnGrab で記録しておく
        internal static readonly Dictionary<int, API.Player> LastHolders = new Dictionary<int, API.Player>();

        [HarmonyPostfix]
        public static void Postfix(ItemBehaviour __instance, bool owner)
        {
            if (!owner) return;
            var player = API.Player.Local;
            if (player != null)
                LastHolders[__instance.GetInstanceID()] = player;
            EventBus.Raise(new ItemPickedUpEvent(__instance));
        }
    }

    [HarmonyPatch(typeof(ItemBehaviour), nameof(ItemBehaviour.OnDrop))]
    internal static class ItemOnDropPatch
    {
        [HarmonyPostfix]
        public static void Postfix(ItemBehaviour __instance)
        {
            int id = __instance.GetInstanceID();
            ItemOnGrabPatch.LastHolders.TryGetValue(id, out var lastHolder);
            EventBus.Raise(new ItemDroppedEvent(__instance, lastHolder));
            ItemOnGrabPatch.LastHolders.Remove(id);
        }
    }

    // ═══════════════════════════════════════════════
    //  武器射撃/リロード — Weapon.WeaponUpdate Prefix でフレーム間比較
    //  全 Weapon サブクラス (Gun, Shotgun, Minigun, ChargeGun 等) の
    //  Update() が WeaponUpdate() を呼ぶため、ここで一括検出できる。
    //  ServerRpc(RunLocally=true) の弾数変更は Update 後に反映されるため、
    //  フレーム間比較が必要。
    // ═══════════════════════════════════════════════

    [HarmonyPatch(typeof(Weapon), nameof(Weapon.WeaponUpdate))]
    internal static class WeaponUpdatePatch
    {
        private static readonly Dictionary<int, float> _prevCharged = new Dictionary<int, float>();
        private static readonly Dictionary<int, int> _prevAmmo = new Dictionary<int, int>();
        private static readonly Dictionary<int, bool> _prevReloading = new Dictionary<int, bool>();

        [HarmonyPrefix]
        public static void Prefix(Weapon __instance)
        {
            // layer 7 = 地面に落ちている武器（WeaponUpdate 内でも早期リターンされる）
            if (__instance.gameObject.layer == 7) return;
            int id = __instance.GetInstanceID();

            float currentCharged = __instance.chargedBullets;
            int currentAmmo = __instance.currentAmmo;
            bool isReloading = __instance.isReloading;

            // 射撃検出: 前フレームの保存値と現在値を比較
            bool fired = false;
            if (__instance.reloadWeapon)
            {
                // reload 型: chargedBullets の減少で検出
                if (_prevCharged.TryGetValue(id, out float prevCharged) && currentCharged < prevCharged && !isReloading)
                    fired = true;
            }
            else
            {
                // 非 reload 型: currentAmmo の減少で検出
                if (_prevAmmo.TryGetValue(id, out int prevAmmo) && currentAmmo < prevAmmo)
                    fired = true;
            }

            if (fired)
            {
                var ev = new WeaponFiredEvent(__instance);
                EventBus.Raise(ev);

                if (ev.Cancel)
                {
                    if (__instance.reloadWeapon)
                    {
                        __instance.chargedBullets = _prevCharged[id];
                        currentCharged = _prevCharged[id];
                    }
                    else
                    {
                        __instance.currentAmmo = _prevAmmo[id];
                        currentAmmo = _prevAmmo[id];
                    }
                }
            }

            // リロード検出: isReloading が false → true
            if (_prevReloading.TryGetValue(id, out bool wasReloading) && isReloading && !wasReloading)
            {
                EventBus.Raise(new WeaponReloadEvent(__instance));
            }

            // 現在の値を保存（次フレームの比較用）
            _prevCharged[id] = currentCharged;
            _prevAmmo[id] = currentAmmo;
            _prevReloading[id] = isReloading;
        }
    }

    // ═══════════════════════════════════════════════
    //  ドア開閉 — OnInteract で直接検出
    // ═══════════════════════════════════════════════

    [HarmonyPatch(typeof(Door), nameof(Door.OnInteract))]
    internal static class DoorInteractPatch
    {
        [HarmonyPrefix]
        public static bool Prefix(Door __instance, Transform player)
        {
            bool wasOpen = __instance.sync___get_value_isOpen();
            var ev = new DoorInteractEvent(__instance, wasOpen);
            EventBus.Raise(ev);
            return !ev.Cancel;
        }
    }
}
