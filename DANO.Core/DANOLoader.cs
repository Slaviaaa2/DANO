using System.IO;
using BepInEx;
using BepInEx.Logging;
using DANO.API;
using DANO.Events;
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

        private void Awake()
        {
            Instance = this;
            Log = base.Logger;

            var danoDir     = Path.GetDirectoryName(Info.Location)!;
            var bepInExRoot = Path.GetDirectoryName(Path.GetDirectoryName(danoDir))!;
            var gameRoot    = Path.GetDirectoryName(bepInExRoot)!;

            EventBus.Initialize(Log);
            CommandManager.Initialize();
            DANOCanvas.GetOrCreate();
            HintController.GetOrCreate();

            // Harmony PatchAll() 方式テスト（Genehmigt / Straftat_CMR と同パターン）
            try
            {
                var harmony = new Harmony(LoaderInfo.GUID);
                harmony.PatchAll();
                var count = 0;
                foreach (var _ in harmony.GetPatchedMethods()) count++;
                Log.LogInfo($"[Harmony] PatchAll() 完了 — パッチ済みメソッド数: {count}");
                foreach (var method in harmony.GetPatchedMethods())
                    Log.LogInfo($"[Harmony]   patched: {method.DeclaringType?.Name}.{method.Name}");

            }
            catch (System.Exception ex)
            {
                Log.LogError($"[Harmony] PatchAll() 失敗: {ex}");
            }

            PluginLoader.ScanAndPrepare(gameRoot, Log);

            // BepInEx の管理 GameObject はゲームに破棄される可能性がある。
            // 自前の隠し GameObject を作成し、そこで初期化を監視する。
            var sentinel = new GameObject("[DANO]");
            sentinel.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(sentinel);
            sentinel.AddComponent<DANOSentinel>();
            sentinel.AddComponent<ConnectionMonitor>();

            Log.LogInfo($"{LoaderInfo.Name} {LoaderInfo.Version} 起動完了。");
        }

        private void OnDestroy()
        {
            PluginLoader.DisableAll();
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
                Destroy(this);
            }
        }
    }

    internal static class LoaderInfo
    {
        public const string GUID    = "dev.slaviaaa.dano";
        public const string Name    = "DANO";
        public const string Version = "0.4.0";
    }
}
