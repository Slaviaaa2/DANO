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
    ///
    /// 注意: FishNet RPC メソッドは Harmony パッチが発火しない可能性が高い。
    /// ConnectionMonitor のポーリングが接続/切断検出のフォールバックとして機能する。
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
                log.LogInfo($"[ClientInstancePatch] パッチ適用（FishNet RPC — 発火しない可能性あり）: ClientInstance.{AddNewPlayerMethod}");
            }
            catch (Exception ex)
            {
                log.LogWarning($"[ClientInstancePatch] パッチ失敗: {ex.Message}");
            }
        }

        private static bool _logged;

        private static void AddNewPlayerPostfix(NetworkConnection owner, NetworkObject newPlayer, int id, ulong steamID)
        {
            if (!_logged)
            {
                _logged = true;
                DANOLoader.Log.LogInfo("[ClientInstancePatch] AddNewPlayerPostfix 初回発火確認！（FishNet RPC パッチが動作した）");
            }

            var client = newPlayer?.GetComponent<ClientInstance>();
            var playerName = client?.PlayerName ?? "";

            EventBus.Raise(new PlayerConnectedEvent(id, playerName, steamID));
        }
    }
}
