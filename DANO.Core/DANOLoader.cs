using System;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using DANO.API;
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
            CommandManager.Initialize();
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
            sentinel.AddComponent<ConnectionMonitor>();

            Log.LogInfo($"{LoaderInfo.Name} {LoaderInfo.Version} 起動完了。");
        }

        private void ApplyPatches()
        {
            // ── 注意 ──
            // PlayerHealth.RemoveHealth / ChangeKilledState は FishNet [ServerRpc] のため
            // Harmony パッチが適用成功しても実行時に発火しない（v0.2.6 で確認済み）。
            // → ConnectionMonitor のヘルスポーリングで代替済み。パッチ登録は行わない。

            TryPatch(
                AccessTools.Method(typeof(Gun), "Fire"),
                prefix: new HarmonyMethod(typeof(GunFirePatch), "Prefix"),
                name: "Gun.Fire");

            TryPatch(
                AccessTools.Method(typeof(PlayerManager), "SpawnPlayer",
                    new[] { typeof(int), typeof(int), typeof(Vector3), typeof(Quaternion) }),
                postfix: new HarmonyMethod(typeof(PlayerManagerPatch), "Postfix"),
                name: "PlayerManager.SpawnPlayer");

            TryPatch(
                AccessTools.Method(typeof(SceneMotor), nameof(SceneMotor.ServerEndGameScene)),
                prefix: new HarmonyMethod(typeof(SceneMotorPatch), "Prefix"),
                name: "SceneMotor.ServerEndGameScene");

            // ── 注意 ──
            // LobbyChatUILogic.Start は Unity ライフサイクルメソッドのため
            // Harmony パッチが発火しない（v0.2.6 で確認済み）。
            // → ConnectionMonitor が Traverse で直接 inputField をフック済み。

            TryPatch(
                AccessTools.Method(typeof(HeathenEngineering.DEMO.LobbyChatUILogic), "OnSendChatMessage"),
                prefix: new HarmonyMethod(typeof(ChatSendPatch), "Prefix"),
                name: "LobbyChatUILogic.OnSendChatMessage");

            TryPatch(
                AccessTools.Method(typeof(HeathenEngineering.DEMO.LobbyChatUILogic), "HandleChatMessage"),
                prefix: new HarmonyMethod(typeof(ChatReceivePatch), "Prefix"),
                name: "LobbyChatUILogic.HandleChatMessage");

            // ── 注意 ──
            // MatchChat.Update は Unity ライフサイクルメソッドのため発火しない可能性大。
            // → ConnectionMonitor の onSubmit フックがコマンドインターセプトを担当。

            // ゲーム内チャット（ChatBroadcast — FishNet Broadcast 経由）
            TryPatch(
                AccessTools.Method(typeof(ChatBroadcast), "SendMessage"),
                prefix: new HarmonyMethod(typeof(ChatBroadcastSendPatch), "Prefix"),
                name: "ChatBroadcast.SendMessage");

            TryPatch(
                AccessTools.Method(typeof(ChatBroadcast), "OnMessageReceived"),
                prefix: new HarmonyMethod(typeof(ChatBroadcastReceivePatch), "Prefix"),
                name: "ChatBroadcast.OnMessageReceived");

            // アイテムイベント
            TryPatch(
                AccessTools.Method(typeof(ItemBehaviour), nameof(ItemBehaviour.OnGrab)),
                postfix: new HarmonyMethod(typeof(ItemGrabPatch), "Postfix"),
                name: "ItemBehaviour.OnGrab");

            TryPatch(
                AccessTools.Method(typeof(ItemBehaviour), nameof(ItemBehaviour.OnDrop)),
                prefix: new HarmonyMethod(typeof(ItemDropPatch), "Prefix"),
                name: "ItemBehaviour.OnDrop");

            // チーム変更イベント
            TryPatch(
                AccessTools.Method(typeof(ScoreManager), nameof(ScoreManager.SetTeamId)),
                prefix: new HarmonyMethod(typeof(ScoreManagerPatch), "Prefix"),
                postfix: new HarmonyMethod(typeof(ScoreManagerPatch), "Postfix"),
                name: "ScoreManager.SetTeamId");

            // 武器イベント
            TryPatch(
                AccessTools.Method(typeof(Weapon), nameof(Weapon.OnReload)),
                postfix: new HarmonyMethod(typeof(WeaponReloadPatch), "Postfix"),
                name: "Weapon.OnReload");

            TryPatch(
                AccessTools.Method(typeof(MeleeWeapon), nameof(MeleeWeapon.HitServer)),
                prefix: new HarmonyMethod(typeof(MeleeHitPatch), "Prefix"),
                name: "MeleeWeapon.HitServer");

            // ドアイベント
            TryPatch(
                AccessTools.Method(typeof(Door), nameof(Door.OnInteract)),
                prefix: new HarmonyMethod(typeof(DoorInteractPatch), "Prefix"),
                name: "Door.OnInteract");

            // ゲーム状態イベント
            TryPatch(
                AccessTools.Method(typeof(PauseManager), nameof(PauseManager.InvokeBeforeSpawn)),
                postfix: new HarmonyMethod(typeof(SpawnPhasePatch), "Postfix"),
                name: "PauseManager.InvokeBeforeSpawn");

            TryPatch(
                AccessTools.Method(typeof(GameManager), nameof(GameManager.StartGame)),
                postfix: new HarmonyMethod(typeof(GameStartPatch), "Postfix"),
                name: "GameManager.StartGame");

            // FishNet ハッシュ名パッチ（バージョン依存）
            GameManagerPatch.TryApply(_harmony, Log);
            ClientInstancePatch.TryApply(_harmony, Log);
            GrenadePatch.TryApply(_harmony, Log);
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
        public const string Version = "0.2.0";
    }
}
