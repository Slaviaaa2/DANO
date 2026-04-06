using System;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using DANO.Events;
using DANO.Patches;
using DANO.Plugin;
using DANO.UI;
using HarmonyLib;
using UnityEngine;

namespace DANO
{
    [BepInPlugin(LoaderInfo.GUID, LoaderInfo.Name, LoaderInfo.Version)]
    internal class DANOLoader : BaseUnityPlugin
    {
        internal static DANOLoader Instance  { get; private set; } = null!;
        internal static ManualLogSource Log  { get; private set; } = null!;

        private Harmony _harmony = null!;

        private void Awake()
        {
            Instance = this;
            Log = base.Logger;

            var danoDir     = Path.GetDirectoryName(Info.Location)!;
            var bepInExRoot = Path.GetDirectoryName(Path.GetDirectoryName(danoDir))!;
            var gameRoot    = Path.GetDirectoryName(bepInExRoot)!;

            EventBus.Initialize(Log);
            DANOCanvas.GetOrCreate();
            HintController.GetOrCreate();

            _harmony = new Harmony(LoaderInfo.GUID);
            ApplyPatches();

            PluginLoader.ScanAndPrepare(gameRoot, Log);

            // BepInEx の管理 GameObject はゲームに破棄される可能性がある。
            // 自前の隠し GameObject を作成し、そこで初期化を監視する。
            var sentinel = new GameObject("[DANO]");
            sentinel.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(sentinel);
            sentinel.AddComponent<DANOSentinel>();

            Log.LogInfo($"{LoaderInfo.Name} {LoaderInfo.Version} 起動完了。");
        }

        private void ApplyPatches()
        {
            // イベントパッチ（非ライフサイクルメソッド — Harmony が確実に効く）
            TryPatch(
                AccessTools.Method(typeof(PlayerHealth), nameof(PlayerHealth.RemoveHealth)),
                prefix: new HarmonyMethod(typeof(RemoveHealthPatch), "Prefix"),
                name: "PlayerHealth.RemoveHealth");

            TryPatch(
                AccessTools.Method(typeof(PlayerHealth), nameof(PlayerHealth.ChangeKilledState)),
                prefix: new HarmonyMethod(typeof(ChangeKilledStatePatch), "Prefix"),
                name: "PlayerHealth.ChangeKilledState");

            TryPatch(
                AccessTools.Method(typeof(Gun), "Fire"),
                prefix: new HarmonyMethod(typeof(GunFirePatch), "Prefix"),
                name: "Gun.Fire");

            GameManagerPatch.TryApply(_harmony, Log);
        }

        private void TryPatch(
            System.Reflection.MethodInfo? target,
            HarmonyMethod? prefix = null,
            HarmonyMethod? postfix = null,
            string name = "")
        {
            if (target == null)
            {
                Log.LogWarning($"[DANOLoader] メソッドが見つかりません: {name}");
                return;
            }
            try
            {
                _harmony.Patch(target, prefix: prefix, postfix: postfix);
                Log.LogInfo($"[DANOLoader] パッチ適用成功: {name}");
            }
            catch (Exception ex)
            {
                Log.LogError($"[DANOLoader] パッチ失敗 ({name}): {ex}");
            }
        }

        private void OnDestroy()
        {
            PluginLoader.DisableAll();
            _harmony.UnpatchSelf();
        }
    }

    /// <summary>
    /// BepInEx 管理オブジェクトとは別の、自前の不滅 GameObject に付くコンポーネント。
    /// Update() で SteamLobby.Instance を監視し、検出したらプラグインを有効化する。
    /// </summary>
    internal class DANOSentinel : MonoBehaviour
    {
        private bool _logged;

        private void Update()
        {
            if (!_logged)
            {
                _logged = true;
                DANOLoader.Log.LogInfo("[DANOSentinel] Update 初回発火確認");
            }

            if (SteamLobby.Instance != null)
            {
                DANOLoader.Log.LogInfo("[DANOSentinel] SteamLobby.Instance 検出 → TryEnableAll");
                PluginLoader.TryEnableAll();
                Destroy(this); // 役目を終えたのでコンポーネントを削除
            }
        }
    }

    // SteamLobbyStartPatch は不要になったので削除

    internal static class LoaderInfo
    {
        public const string GUID    = "dev.slaviaaa.dano";
        public const string Name    = "DANO";
        public const string Version = "0.1.0";
    }
}
