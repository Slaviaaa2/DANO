using DANO.API;
using DANO.Events;
using DANO.Plugin;
using UnityEngine;

// STRAFTAT の matchmaking pool に影響しないよう vanilla 互換として登録
[assembly: ComputerysModdingUtilities.StraftatMod(isVanillaCompatible: true)]

namespace MyPlugin
{
    [DANOPlugin(
        id:          "my-plugin",
        version:     "1.0.0",
        author:      "YourName",
        description: "DANO プラグインのテンプレート")]
    public class MyPlugin : Plugin<MyPlugin.MyConfig>
    {
        public override void OnEnabled()
        {
            Log.LogInfo("MyPlugin が有効になりました！");

            // イベント購読
            EventBus.Subscribe<PlayerDiedEvent>(OnPlayerDied);
            EventBus.Subscribe<RoundStartedEvent>(OnRoundStarted);

            // 起動メッセージを画面に表示
            HudAPI.ShowHint($"<color=#00FFFF>{Config.WelcomeMessage}</color>", duration: 4f);
        }

        public override void OnDisabled()
        {
            EventBus.Unsubscribe<PlayerDiedEvent>(OnPlayerDied);
            EventBus.Unsubscribe<RoundStartedEvent>(OnRoundStarted);
        }

        private void OnPlayerDied(PlayerDiedEvent ev)
        {
            if (!Config.AnnounceDeaths) return;
            HudAPI.LocalMessage($"Player {ev.PlayerId} が死亡しました。");
        }

        private void OnRoundStarted(RoundStartedEvent ev)
        {
            HudAPI.ShowHint($"Round {ev.TakeIndex + 1} スタート！", color: Color.yellow);
        }

        // ────────── 設定クラス ──────────
        public class MyConfig : PluginConfig
        {
            public string WelcomeMessage { get; set; } = "MyPlugin が起動しました！";
            public bool AnnounceDeaths   { get; set; } = true;
        }
    }
}
