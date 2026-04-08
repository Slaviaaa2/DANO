using System;
using BepInEx.Logging;
using DANO.Events;
using HarmonyLib;
using UnityEngine;

namespace DANO.Patches
{
    /// <summary>
    /// PhysicsGrenade の爆発をフックしてグレネードイベントを発火する。
    /// FishNet ハッシュ名のため TryApply パターン。
    ///
    /// 注意: FishNet RPC メソッドは Harmony パッチが発火しない可能性が高い。
    /// </summary>
    internal static class GrenadePatch
    {
        private const string HandleExplosionMethod = "RpcLogic___HandleExplosion_4276783012";
        private static bool _logged;

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
                log.LogInfo($"[GrenadePatch] パッチ適用（FishNet RPC — 発火しない可能性あり）: PhysicsGrenade.{HandleExplosionMethod}");
            }
            catch (Exception ex)
            {
                log.LogWarning($"[GrenadePatch] パッチ失敗: {ex.Message}");
            }
        }

        private static void HandleExplosionPrefix(PhysicsGrenade __instance, Vector3 position)
        {
            if (!_logged)
            {
                _logged = true;
                DANOLoader.Log.LogInfo("[GrenadePatch] HandleExplosionPrefix 初回発火確認！（FishNet RPC パッチが動作した）");
            }

            EventBus.Raise(new GrenadeExplodedEvent(
                position,
                __instance.explosionRadius,
                __instance.fragGrenade,
                __instance.stunGrenade));
        }
    }
}
