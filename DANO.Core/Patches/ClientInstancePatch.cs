using System;
using BepInEx.Logging;
using DANO.Events;
using FishNet.Connection;
using FishNet.Object;
using HarmonyLib;

namespace DANO.Patches
{
    /// <summary>
    /// ClientInstance の AddNewPlayer RPC をフックしてプレイヤー接続イベントを発火する。
    /// FishNet が生成するハッシュ付きメソッド名はバージョン間で変わる可能性がある。
    /// </summary>
    internal static class ClientInstancePatch
    {
        private const string AddNewPlayerMethod = "RpcLogic___AddNewPlayer_2166193949";

        internal static void TryApply(Harmony harmony, ManualLogSource log)
        {
            var method = AccessTools.Method(typeof(ClientInstance), AddNewPlayerMethod);
            if (method == null)
            {
                log.LogWarning($"[ClientInstancePatch] メソッドが見つかりません: ClientInstance.{AddNewPlayerMethod}");
                return;
            }

            try
            {
                harmony.Patch(method,
                    postfix: new HarmonyMethod(typeof(ClientInstancePatch), nameof(AddNewPlayerPostfix)));
                log.LogDebug($"[ClientInstancePatch] パッチ適用: ClientInstance.{AddNewPlayerMethod}");
            }
            catch (Exception ex)
            {
                log.LogWarning($"[ClientInstancePatch] パッチ失敗: {ex.Message}");
            }
        }

        private static void AddNewPlayerPostfix(NetworkConnection owner, NetworkObject newPlayer, int id, ulong steamID)
        {
            var client = newPlayer?.GetComponent<ClientInstance>();
            var playerName = client?.PlayerName ?? "";

            EventBus.Raise(new PlayerConnectedEvent(id, playerName, steamID));
        }
    }
}
