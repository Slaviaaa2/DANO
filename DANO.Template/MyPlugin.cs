using DANO.API;
using DANO.Events;
using DANO.Plugin;
using UnityEngine;

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
            Logger.Info("MyPlugin が有効になりました！");

            // ── イベント購読 ──
            EventBus.Subscribe<PlayerDiedEvent>(OnPlayerDied);
            EventBus.Subscribe<PlayerDamagedEvent>(OnPlayerDamaged);
            EventBus.Subscribe<RoundStartedEvent>(OnRoundStarted);
            EventBus.Subscribe<ItemPickedUpEvent>(OnItemPickedUp);
            EventBus.Subscribe<WeaponReloadEvent>(OnWeaponReload);
            EventBus.Subscribe<GrenadeExplodedEvent>(OnGrenadeExploded);
            EventBus.Subscribe<PlayerConnectedEvent>(OnPlayerConnected);
            EventBus.Subscribe<DoorInteractEvent>(OnDoorInteract);
            EventBus.Subscribe<GameStartedEvent>(OnGameStarted);

            // ── コマンド登録 ──
            CommandManager.Register("heal", OnHealCommand, "体力を全回復する");
            CommandManager.Register("info", OnInfoCommand, "サーバー情報を表示する");
            CommandManager.Register("weapon", OnWeaponCommand, "持っている武器の情報を表示する");
            CommandManager.Register("tp", OnTeleportCommand, "指定座標にテレポート");
            CommandManager.Register("freeze", OnFreezeCommand, "対象プレイヤーをフリーズ", hostOnly: true);
            CommandManager.Register("scramble", OnScrambleCommand, "チームをシャッフル", hostOnly: true);
            CommandManager.Register("doors", OnDoorsCommand, "全ドアの状態を表示");
            CommandManager.Register("find", OnFindCommand, "名前でプレイヤーを検索");

            HudAPI.ShowHint($"<color=#00FFFF>{Config.WelcomeMessage}</color>", duration: 4f);
        }

        public override void OnDisabled()
        {
            EventBus.Unsubscribe<PlayerDiedEvent>(OnPlayerDied);
            EventBus.Unsubscribe<PlayerDamagedEvent>(OnPlayerDamaged);
            EventBus.Unsubscribe<RoundStartedEvent>(OnRoundStarted);
            EventBus.Unsubscribe<ItemPickedUpEvent>(OnItemPickedUp);
            EventBus.Unsubscribe<WeaponReloadEvent>(OnWeaponReload);
            EventBus.Unsubscribe<GrenadeExplodedEvent>(OnGrenadeExploded);
            EventBus.Unsubscribe<PlayerConnectedEvent>(OnPlayerConnected);
            EventBus.Unsubscribe<DoorInteractEvent>(OnDoorInteract);
            EventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);

            CommandManager.Unregister("heal");
            CommandManager.Unregister("info");
            CommandManager.Unregister("weapon");
            CommandManager.Unregister("tp");
            CommandManager.Unregister("freeze");
            CommandManager.Unregister("scramble");
            CommandManager.Unregister("doors");
            CommandManager.Unregister("find");
        }

        // ────────── イベントハンドラ ──────────

        private void OnPlayerDied(PlayerDiedEvent ev)
        {
            if (!Config.AnnounceDeaths) return;

            var msg = $"{ev.Player?.Name ?? "?"} が死亡しました。";
            if (ev.Attacker != null)
                msg += $" (キル: {ev.Attacker.Name})";

            HudAPI.LocalMessage(msg);
        }

        private void OnPlayerDamaged(PlayerDamagedEvent ev)
        {
            Logger.Debug($"{ev.Player?.Name} が {ev.Damage} ダメージを受けた (残HP: {ev.Player?.Health})");
        }

        private void OnRoundStarted(RoundStartedEvent ev)
        {
            HudAPI.ShowHint($"Round {ev.TakeIndex + 1} スタート！", color: Color.yellow);
        }

        private void OnItemPickedUp(ItemPickedUpEvent ev)
        {
            Logger.Info($"{ev.Player?.Name} が {ev.Item.Name} を拾った (弾数: {ev.Item.Ammo})");
        }

        private void OnWeaponReload(WeaponReloadEvent ev)
        {
            var weapon = ev.Item?.Weapon;
            if (weapon == null) return;
            Logger.Debug($"{ev.Player?.Name} が {weapon.Name} をリロード (弾数: {weapon.Ammo})");
        }

        private void OnGrenadeExploded(GrenadeExplodedEvent ev)
        {
            var type = ev.IsFragGrenade ? "フラグ" : ev.IsStunGrenade ? "スタン" : "不明";
            Logger.Info($"{type}グレネードが爆発 (半径: {ev.Radius:F1}m, 位置: {ev.Position})");
        }

        private void OnPlayerConnected(PlayerConnectedEvent ev)
        {
            HudAPI.ShowHint($"<color=#00FF00>{ev.PlayerName} が参加しました</color>", duration: 3f);
        }

        private void OnDoorInteract(DoorInteractEvent ev)
        {
            var action = ev.WasOpen ? "閉じた" : "開けた";
            Logger.Debug($"ドア {ev.Door.Name} を{action}");
        }

        private void OnGameStarted(GameStartedEvent ev)
        {
            Logger.Info("ゲームが開始されました！");

            // DanoTimer の使用例: 5秒後にメッセージ表示
            DanoTimer.After(5f, () =>
            {
                HudAPI.ShowHint("<color=#FFFF00>ゲーム開始から5秒経過！</color>", duration: 2f);
            });
        }

        // ────────── コマンドハンドラ ──────────

        private void OnHealCommand(CommandContext ctx)
        {
            // Player.HealFull() で全回復
            var player = ctx.Sender;
            if (player == null) return;

            player.HealFull();
            ctx.Reply($"<color=#00FF00>{player.Name} の体力を全回復しました！</color>");
        }

        private void OnInfoCommand(CommandContext ctx)
        {
            // ServerAPI / RoundAPI / MapAPI の使用例
            var map = MapAPI.CurrentMap ?? "不明";
            var players = ServerAPI.PlayerCount;
            var max = ServerAPI.MaxPlayers;
            var take = RoundAPI.CurrentTake + 1;

            ctx.Reply($"マップ: {map} | プレイヤー: {players}/{max} | ラウンド: {take}");
        }

        private void OnWeaponCommand(CommandContext ctx)
        {
            // DanoWeapon ラッパーの使用例
            var player = ctx.Sender;
            if (player == null) return;

            var item = player.GetHeldItem();
            if (item == null)
            {
                ctx.Reply("武器を持っていません。");
                return;
            }

            var weapon = item.Weapon;
            if (weapon == null)
            {
                ctx.Reply($"持っているアイテム: {item.Name} (武器ではありません)");
                return;
            }

            var info = $"武器: {weapon.Name} | ダメージ: {weapon.Damage:F0}";
            if (weapon.IsGun)
                info += $" | 弾数: {weapon.Ammo} | レート: {weapon.FireRate:F1}";
            if (weapon.IsMelee)
                info += $" | ノックバック: {weapon.MeleeKnockback:F1}";

            ctx.Reply(info);
        }

        private void OnTeleportCommand(CommandContext ctx)
        {
            // Player.Teleport() の使用例: /tp x y z
            var player = ctx.Sender;
            if (player == null || ctx.Args.Length < 3) return;

            if (float.TryParse(ctx.Args[0], out var x) &&
                float.TryParse(ctx.Args[1], out var y) &&
                float.TryParse(ctx.Args[2], out var z))
            {
                player.Teleport(new Vector3(x, y, z));
                ctx.Reply($"({x}, {y}, {z}) にテレポートしました。");
            }
        }

        private void OnFreezeCommand(CommandContext ctx)
        {
            // Player.Freeze/Unfreeze の使用例: /freeze <playerId>
            if (ctx.Args.Length < 1 || !int.TryParse(ctx.Args[0], out var targetId)) return;

            var target = PlayerAPI.Get(targetId);
            if (target == null) { ctx.Reply("プレイヤーが見つかりません。"); return; }

            if (target.CanMove)
            {
                target.Freeze();
                ctx.Reply($"{target.Name} をフリーズしました。");
            }
            else
            {
                target.Unfreeze();
                ctx.Reply($"{target.Name} のフリーズを解除しました。");
            }
        }

        private void OnScrambleCommand(CommandContext ctx)
        {
            // GameAPI.ScrambleTeams() の使用例
            GameAPI.ScrambleTeams();
            ctx.Reply("<color=#FFFF00>チームをシャッフルしました！</color>");
        }

        private void OnDoorsCommand(CommandContext ctx)
        {
            // DanoDoor.List の使用例
            var doors = DanoDoor.List;
            foreach (var door in doors)
                ctx.Reply($"  {door.Name}: {(door.IsOpen ? "開" : "閉")}");
        }

        private void OnFindCommand(CommandContext ctx)
        {
            // PlayerAPI.GetByName() の使用例: /find <名前>
            if (ctx.Args.Length < 1) { ctx.Reply("使い方: /find <名前>"); return; }

            var target = PlayerAPI.GetByName(ctx.Args[0]);
            if (target == null) { ctx.Reply("見つかりませんでした。"); return; }

            ctx.Reply($"ID: {target.Id} | 名前: {target.Name} | HP: {target.Health}/{target.MaxHealth} | チーム: {target.TeamId}");
        }

        // ────────── 設定クラス ──────────

        public class MyConfig : PluginConfig
        {
            public string WelcomeMessage { get; set; } = "MyPlugin が起動しました！";
            public bool AnnounceDeaths   { get; set; } = true;
        }
    }
}
