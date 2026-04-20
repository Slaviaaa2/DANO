using DANO.API;
using DANO.Events;
using DANO.Plugin;
using TMPro;
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
            EventBus.Subscribe<WeaponReloadedEvent>(OnWeaponReloaded);
            EventBus.Subscribe<GrenadeExplodedEvent>(OnGrenadeExploded);
            EventBus.Subscribe<PlayerConnectedEvent>(OnPlayerConnected);
            EventBus.Subscribe<WeaponFiredEvent>(OnWeaponFired);
            EventBus.Subscribe<ItemDroppedEvent>(OnItemDropped);
            EventBus.Subscribe<DoorInteractedEvent>(OnDoorInteracted);
            EventBus.Subscribe<GameStartedEvent>(OnGameStarted);
            EventBus.Subscribe<PlayerDamagingEvent>(OnPlayerDamaging);

            // ── コマンド登録 ──
            CommandManager.Register("heal",     OnHealCommand,     "体力を全回復する");
            CommandManager.Register("info",     OnInfoCommand,     "サーバー情報を表示する");
            CommandManager.Register("weapon",   OnWeaponCommand,   "持っている武器の情報を表示する");
            CommandManager.Register("tp",       OnTeleportCommand, "指定座標にテレポート");
            CommandManager.Register("freeze",   OnFreezeCommand,   "対象プレイヤーをフリーズ", hostOnly: true);
            CommandManager.Register("scramble", OnScrambleCommand, "チームをシャッフル",         hostOnly: true);
            CommandManager.Register("doors",    OnDoorsCommand,    "全ドアの状態を表示");
            CommandManager.Register("find",     OnFindCommand,     "名前でプレイヤーを検索");
            CommandManager.Register("guntest",  OnGunTestCommand,  "持っている銃の内部状態をダンプ");
            CommandManager.Register("fonts",    OnFontsCommand,    "ロード済み TMP フォントを全てログに出力");

            Hud.ShowHint($"<color=#00FFFF>{Config.WelcomeMessage}</color>", duration: 4f);
        }

        public override void OnDisabled()
        {
            EventBus.Unsubscribe<PlayerDiedEvent>(OnPlayerDied);
            EventBus.Unsubscribe<PlayerDamagedEvent>(OnPlayerDamaged);
            EventBus.Unsubscribe<RoundStartedEvent>(OnRoundStarted);
            EventBus.Unsubscribe<ItemPickedUpEvent>(OnItemPickedUp);
            EventBus.Unsubscribe<WeaponReloadedEvent>(OnWeaponReloaded);
            EventBus.Unsubscribe<GrenadeExplodedEvent>(OnGrenadeExploded);
            EventBus.Unsubscribe<PlayerConnectedEvent>(OnPlayerConnected);
            EventBus.Unsubscribe<WeaponFiredEvent>(OnWeaponFired);
            EventBus.Unsubscribe<ItemDroppedEvent>(OnItemDropped);
            EventBus.Unsubscribe<DoorInteractedEvent>(OnDoorInteracted);
            EventBus.Unsubscribe<GameStartedEvent>(OnGameStarted);
            EventBus.Unsubscribe<PlayerDamagingEvent>(OnPlayerDamaging);

            CommandManager.Unregister("heal");
            CommandManager.Unregister("info");
            CommandManager.Unregister("weapon");
            CommandManager.Unregister("tp");
            CommandManager.Unregister("freeze");
            CommandManager.Unregister("scramble");
            CommandManager.Unregister("doors");
            CommandManager.Unregister("find");
            CommandManager.Unregister("guntest");
            CommandManager.Unregister("fonts");
        }

        // ────────── -ing イベントハンドラ（Cancel可） ──────────

        private void OnPlayerDamaging(PlayerDamagingEvent ev)
        {
            // 例: 5ダメージ以下を無効化
            // if (ev.Damage <= 5f) ev.Cancel = true;
            Logger.Debug($"[ing] {ev.Player?.Name} が {ev.Damage} ダメージを受けようとしている");
        }

        // ────────── -ed イベントハンドラ（通知のみ） ──────────

        private void OnPlayerDied(PlayerDiedEvent ev)
        {
            if (!Config.AnnounceDeaths) return;

            var msg = $"{ev.Player?.Name ?? "?"} が死亡しました。";
            if (ev.Attacker != null)
                msg += $" (キル: {ev.Attacker.Name})";

            Hud.LocalMessage(msg);
        }

        private void OnPlayerDamaged(PlayerDamagedEvent ev)
        {
            Logger.Debug($"{ev.Player?.Name} が {ev.Damage} ダメージを受けた (残HP: {ev.Player?.Health})");
        }

        private void OnRoundStarted(RoundStartedEvent ev)
        {
            Hud.ShowHint($"Round {ev.TakeIndex + 1} スタート！", color: Color.yellow);
        }

        private void OnItemPickedUp(ItemPickedUpEvent ev)
        {
            Logger.Info($"{ev.Player?.Name} が {ev.Item.Name} を拾った (弾数: {ev.Item.Ammo})");
        }

        private void OnWeaponFired(WeaponFiredEvent ev)
        {
            Logger.Info($"{ev.Player?.Name} が {ev.Item?.Name} を発射！");
        }

        private void OnItemDropped(ItemDroppedEvent ev)
        {
            Logger.Info($"{ev.Player?.Name} が {ev.Item.Name} を捨てた");
        }

        private void OnWeaponReloaded(WeaponReloadedEvent ev)
        {
            var weapon = ev.Item?.Weapon;
            if (weapon == null) return;
            Logger.Info($"{ev.Player?.Name} が {weapon.Name} のリロード完了 (弾数: {weapon.Ammo})");
        }

        private void OnGrenadeExploded(GrenadeExplodedEvent ev)
        {
            var type = ev.IsFragGrenade ? "フラグ" : ev.IsStunGrenade ? "スタン" : "不明";
            Logger.Info($"{type}グレネードが爆発 (半径: {ev.Radius:F1}m, 位置: {ev.Position})");
        }

        private void OnPlayerConnected(PlayerConnectedEvent ev)
        {
            Hud.ShowHint($"<color=#00FF00>{ev.PlayerName} が参加しました</color>", duration: 3f);
        }

        private void OnDoorInteracted(DoorInteractedEvent ev)
        {
            var action = ev.WasOpen ? "閉じた" : "開けた";
            Logger.Info($"ドア {ev.Door.Name} を{action}");
        }

        private void OnGameStarted(GameStartedEvent ev)
        {
            Logger.Info("ゲームが開始されました！");

            Timer.After(5f, () =>
            {
                Hud.ShowHint("<color=#FFFF00>ゲーム開始から5秒経過！</color>", duration: 2f);
            });
        }

        // ────────── コマンドハンドラ ──────────

        private void OnHealCommand(CommandContext ctx)
        {
            var player = ctx.Sender;
            if (player == null) return;

            player.HealFull();
            ctx.Reply($"<color=#00FF00>{player.Name} の体力を全回復しました！</color>");
        }

        private void OnInfoCommand(CommandContext ctx)
        {
            var map     = DANO.API.Map.CurrentMap ?? "不明";
            var players = Server.PlayerCount;
            var max     = Server.MaxPlayers;
            var take    = Round.CurrentTake + 1;

            ctx.Reply($"マップ: {map} | プレイヤー: {players}/{max} | ラウンド: {take}");
        }

        private void OnWeaponCommand(CommandContext ctx)
        {
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

            // 具象型へのキャスト例
            if (weapon is ShotgunWeapon sg)
                info += $" | ペレット: {sg.PelletCount}";

            ctx.Reply(info);
        }

        private void OnTeleportCommand(CommandContext ctx)
        {
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
            if (ctx.Args.Length < 1 || !int.TryParse(ctx.Args[0], out var targetId)) return;

            var target = DANO.API.Player.Get(targetId);
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
            Game.ScrambleTeams();
            ctx.Reply("<color=#FFFF00>チームをシャッフルしました！</color>");
        }

        private void OnDoorsCommand(CommandContext ctx)
        {
            foreach (var door in DANO.API.Door.List)
                ctx.Reply($"  {door.Name}: {(door.IsOpen ? "開" : "閉")}");
        }

        private void OnFindCommand(CommandContext ctx)
        {
            if (ctx.Args.Length < 1) { ctx.Reply("使い方: /find <名前>"); return; }

            var target = DANO.API.Player.GetByName(ctx.Args[0]);
            if (target == null) { ctx.Reply("見つかりませんでした。"); return; }

            ctx.Reply($"ID: {target.Id} | 名前: {target.Name} | HP: {target.Health}/{target.MaxHealth} | チーム: {target.TeamId}");
        }

        private void OnGunTestCommand(CommandContext ctx)
        {
            var player = ctx.Sender;
            if (player == null) return;

            var item = player.GetHeldItem();
            if (item == null) { ctx.Reply("武器を持っていません。"); return; }

            var w = item.Weapon;
            if (w == null) { ctx.Reply($"持っているアイテム: {item.Name} (Weapon なし)"); return; }

            ctx.Reply($"=== {w.Name} ===");
            ctx.Reply($"IsGun={w.IsGun}, CanReload={w.CanReload}");
            ctx.Reply($"Ammo={w.Ammo}, Reserve={w.ReserveAmmo}");
            ctx.Reply($"Damage={w.Damage:F1}, FireRate={w.FireRate:F2}");

            int prevAmmo = w.Ammo;
            ctx.Reply($"[監視] 3秒間弾数を監視します。撃ってください！");
            Timer.Every(0.1f, () =>
            {
                int curAmmo = w.Ammo;
                if (curAmmo != prevAmmo)
                {
                    Logger.Info($"[guntest] 変化! Ammo:{prevAmmo}→{curAmmo}");
                    prevAmmo = curAmmo;
                }
            }, repeatCount: 30);
        }

        private void OnFontsCommand(CommandContext ctx)
        {
            var fonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
            Logger.Info($"=== ロード済み TMP フォント ({fonts.Length}件) ===");
            foreach (var f in fonts)
            {
                bool hasHira  = f.HasCharacter('あ');
                bool hasKanji = f.HasCharacter('漢');
                bool hasKata  = f.HasCharacter('ア');
                Logger.Info(
                    $"  [{f.name}]" +
                    $"  ひらがな:{(hasHira  ? "○" : "×")}" +
                    $"  カタカナ:{(hasKata  ? "○" : "×")}" +
                    $"  漢字:{(hasKanji ? "○" : "×")}" +
                    $"  Atlas:{f.atlasWidth}x{f.atlasHeight}");
            }
            ctx.Reply($"TMP フォント {fonts.Length} 件をログに出力しました。");
        }

        // ────────── 設定クラス ──────────

        public class MyConfig : PluginConfig
        {
            public string WelcomeMessage { get; set; } = "MyPlugin が起動しました！";
            public bool AnnounceDeaths   { get; set; } = true;
        }
    }
}
