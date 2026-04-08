using System;
using BepInEx.Logging;
using DANO.Events;
using HarmonyLib;
using UnityEngine;

namespace DANO.Patches
{
    /// <summary>
    /// PhysicsGrenade ���爆発をフックしてグレネードイベントを発火する。
    /// FishNet ハッシュ名のため TryApply パターン。
    /// </summary>
    internal static class GrenadePatch
    {
        private const string HandleExplosionMethod = "RpcLogic___HandleExplosion_4276783012";

        internal static void TryApply(Harmony harmony, ManualLogSource log)
        {
            var method = AccessTools.Method(typeof(PhysicsGrenade), HandleExplosionMethod);
            if (method == null)
            {
                log.LogWarning($"[GrenadePatch] メソッドが見つかりません: PhysicsGrenade.{HandleExplosionMethod}");
                return;
            }

            try
            {
                harmony.Patch(method,
                    prefix: new HarmonyMethod(typeof(GrenadePatch), nameof(HandleExplosionPrefix)));
                log.LogDebug($"[GrenadePatch] パッチ適用: PhysicsGrenade.{HandleExplosionMethod}");
            }
            catch (Exception ex)
            {
                log.LogWarning($"[GrenadePatch] パッチ失敗: {ex.Message}");
            }
        }

        private static void HandleExplosionPrefix(PhysicsGrenade __instance, Vector3 position)
        {
            EventBus.Raise(new GrenadeExplodedEvent(
                position,
                __instance.explosionRadius,
                __instance.fragGrenade,
                __instance.stunGrenade));
        }
    }
}
