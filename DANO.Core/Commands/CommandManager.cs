using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace DANO.API
{
    /// <summary>チャットコマンドの登録・実行を管理する</summary>
    public static class CommandManager
    {
        private static readonly Dictionary<string, CommandInfo> _commands
            = new Dictionary<string, CommandInfo>(StringComparer.OrdinalIgnoreCase);

        /// <summary>登録済みコマンド一覧</summary>
        public static IReadOnlyDictionary<string, CommandInfo> Commands => _commands;

        /// <summary>フレームワーク初期化時に呼ばれる</summary>
        internal static void Initialize()
        {
            Register("help", HelpCommand, "登録済みコマンド一覧を表示");
            Register("diag", DANO.ConnectionMonitor.RunDiagnostics, "シーン内コンポーネント診断");
            Register("patchtest", PatchTestCommand, "パッチ発火テスト（自分に1ダメージ）");
        }

        /// <summary>コマンドを登録する</summary>
        public static void Register(string name, Action<CommandContext> handler, string description = "", bool hostOnly = false)
        {
            _commands[name.ToLowerInvariant()] = new CommandInfo(name.ToLowerInvariant(), handler, description, hostOnly);
        }

        /// <summary>コマンドを登録解除する</summary>
        public static void Unregister(string name)
        {
            _commands.Remove(name.ToLowerInvariant());
        }

        /// <summary>
        /// 入力テキストがコマンドなら実行する。
        /// </summary>
        /// <returns>コマンドとして処理された場合 true</returns>
        internal static bool TryExecute(string input)
        {
            if (string.IsNullOrEmpty(input) || !input.StartsWith("/"))
                return false;

            var trimmed = input.Substring(1).Trim();
            if (string.IsNullOrEmpty(trimmed))
                return false;

            var parts = trimmed.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var commandName = parts[0].ToLowerInvariant();

            if (!_commands.TryGetValue(commandName, out var info))
                return false;

            var args = parts.Length > 1 ? parts.Skip(1).ToArray() : Array.Empty<string>();
            var rawArgs = trimmed.Length > commandName.Length
                ? trimmed.Substring(commandName.Length).TrimStart()
                : "";

            var sender = Player.Local;
            var ctx = new CommandContext(commandName, args, rawArgs, sender);

            // ホスト限定コマンドの権限チェック
            if (info.HostOnly && sender != null && !sender.IsHost)
            {
                ctx.Reply("<color=#FF4444>このコマンドはホストのみ実行できます。</color>");
                return true;
            }

            try
            {
                info.Handler(ctx);
            }
            catch (Exception ex)
            {
                DANOLoader.Log.LogError($"[CommandManager] コマンド '{commandName}' の実行中にエラー: {ex}");
                ctx.Reply($"<color=#FF4444>コマンドエラー: {ex.Message}</color>");
            }

            return true;
        }

        private static void PatchTestCommand(CommandContext ctx)
        {
            var lines = new List<string> { "<color=#00FFFF>=== パッチ発火テスト ===</color>" };
            DANOLoader.Log.LogInfo("[PatchTest] 開始");

            // ClientInstance チェック
            var ci = ClientInstance.Instance;
            DANOLoader.Log.LogInfo($"[PatchTest] ClientInstance.Instance = {(ci != null ? ci.PlayerName : "NULL")}");

            // PlayerHealth 検索（FindObjectOfType で確実に見つける）
            var playerHealth = UnityEngine.Object.FindObjectOfType<PlayerHealth>();
            DANOLoader.Log.LogInfo($"[PatchTest] PlayerHealth = {(playerHealth != null ? $"found (health={playerHealth.health})" : "NULL")}");

            if (playerHealth != null)
            {
                var hpBefore = playerHealth.health;
                DANOLoader.Log.LogInfo($"[PatchTest] RemoveHealth(1) 呼び出し (HP={hpBefore})");

                try
                {
                    playerHealth.RemoveHealth(1f);
                }
                catch (System.Exception ex)
                {
                    DANOLoader.Log.LogError($"[PatchTest] RemoveHealth 例外: {ex}");
                    lines.Add($"  RemoveHealth: <color=#FF4444>例外: {ex.Message}</color>");
                }

                var hpAfter = playerHealth.health;
                lines.Add($"  RemoveHealth: HP {hpBefore} → {hpAfter}");
                DANOLoader.Log.LogInfo($"[PatchTest] RemoveHealth 完了 (HP={hpAfter})");
            }
            else
            {
                lines.Add("  PlayerHealth: <color=#FF4444>NOT FOUND</color>");
            }

            // EventBus サブスクライバー数
            lines.Add($"  EventBus 登録数: {DANO.Events.EventBus.GetSubscriberCount()}");

            // 手動テスト案内
            lines.Add("  Gun.Fire / OnGrab: <color=#888888>手動テスト</color>");

            lines.Add("<color=#00FFFF>=== テスト完了 ===</color>");
            ctx.Reply(string.Join("\n", lines));
        }

        private static void HelpCommand(CommandContext ctx)
        {
            if (_commands.Count == 0)
            {
                ctx.Reply("登録済みコマンドはありません。");
                return;
            }

            var lines = new List<string> { "<color=#00FFFF>=== DANO コマンド一覧 ===</color>" };
            foreach (var cmd in _commands.Values)
            {
                var desc = string.IsNullOrEmpty(cmd.Description) ? "" : $" — {cmd.Description}";
                var hostTag = cmd.HostOnly ? " <color=#FF8800>[HOST]</color>" : "";
                lines.Add($"<color=#FFFFFF>/{cmd.Name}</color>{desc}{hostTag}");
            }
            ctx.Reply(string.Join("\n", lines));
        }
    }

    /// <summary>登録済みコマンドの情報</summary>
    public class CommandInfo
    {
        public string Name { get; }
        public Action<CommandContext> Handler { get; }
        public string Description { get; }
        /// <summary>true の場合、ホストのみ実行可能</summary>
        public bool HostOnly { get; }

        internal CommandInfo(string name, Action<CommandContext> handler, string description, bool hostOnly = false)
        {
            Name = name;
            Handler = handler;
            Description = description;
            HostOnly = hostOnly;
        }
    }
}
