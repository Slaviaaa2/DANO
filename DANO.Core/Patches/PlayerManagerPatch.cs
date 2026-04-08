using DANO.Events;
using HarmonyLib;
using UnityEngine;

namespace DANO.Patches
{
    /// <summary>
    /// PlayerManager.SpawnPlayer(int, int, Vector3, Quaternion) にパッチを適用し、
    /// PlayerSpawnedEvent を発火させる。
    /// </summary>
    [HarmonyPatch(typeof(PlayerManager), "SpawnPlayer",
        typeof(int), typeof(int), typeof(Vector3), typeof(Quaternion))]
    internal static class PlayerManagerPatch
    {
        private static bool _logged;

        private static void Postfix(PlayerManager __instance)
        {
            if (!_logged)
            {
                _logged = true;
                DANOLoader.Log.LogInfo("[PlayerManagerPatch] Postfix 初回発火確認！");
            }

            var client = __instance.ClientScript;
            if (client == null) return;

            EventBus.Raise(new PlayerSpawnedEvent(client));
        }
    }
}
