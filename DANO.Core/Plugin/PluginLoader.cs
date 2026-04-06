using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx.Logging;

namespace DANO.Plugin
{
    /// <summary>
    /// plugins/ ディレクトリを走査して DANO プラグインを自動ロードする。
    /// <para>
    /// 処理は2フェーズ:<br/>
    /// 1. <see cref="ScanAndPrepare"/> — BepInEx Awake 時。DLL 検索・インスタンス化・config 読み込み。<br/>
    /// 2. <see cref="EnableAll"/>     — 最初のゲームシーンがロードされた後。OnEnabled() を呼ぶ。
    /// </para>
    /// </summary>
    internal static class PluginLoader
    {
        // 準備済みだがまだ有効化していないプラグイン
        private readonly struct PendingPlugin(IPlugin plugin, ManualLogSource log, string configPath)
        {
            public readonly IPlugin Plugin      = plugin;
            public readonly ManualLogSource Log = log;
            public readonly string ConfigPath   = configPath;
        }

        private static readonly List<PendingPlugin> _pending = new();
        private static readonly List<IPlugin>       _enabled = new();
        private static ManualLogSource _loaderLog = null!;
        private static bool _enableAllCalled;

        /// <summary>STRAFTAT/DANO/mods/</summary>
        internal static string PluginDirectory { get; private set; } = "";
        /// <summary>STRAFTAT/DANO/configs/</summary>
        internal static string ConfigDirectory  { get; private set; } = "";

        internal static IReadOnlyList<IPlugin> Enabled => _enabled;

        // ────────────────────────────────────────────────────────────
        // フェーズ 1 — BepInEx Awake 時に呼ぶ
        // ────────────────────────────────────────────────────────────

        /// <param name="gameRoot">ゲームルートフォルダ (STRAFTAT/)</param>
        internal static void ScanAndPrepare(string gameRoot, ManualLogSource log)
        {
            _loaderLog = log;

            // BepInEx ツリーの外に置くことでスキャン干渉を完全に回避
            PluginDirectory = Path.Combine(gameRoot, "DANO", "mods");
            ConfigDirectory = Path.Combine(gameRoot, "DANO", "configs");
            Directory.CreateDirectory(PluginDirectory);
            Directory.CreateDirectory(ConfigDirectory);

            var dlls = Directory.GetFiles(PluginDirectory, "*.dll", SearchOption.TopDirectoryOnly);
            log.LogInfo($"[PluginLoader] {dlls.Length} 個の DLL をスキャン中 ({PluginDirectory})");

            foreach (var dll in dlls)
            {
                try   { PrepareAssembly(dll, log); }
                catch (Exception ex)
                { log.LogError($"[PluginLoader] {Path.GetFileName(dll)} の準備に失敗: {ex}"); }
            }

            log.LogInfo($"[PluginLoader] {_pending.Count} 個のプラグインを準備しました。シーンロード後に有効化します。");
        }

        // ────────────────────────────────────────────────────────────
        // フェーズ 2 — SteamLobby.Start() Postfix から呼ぶ
        // ────────────────────────────────────────────────────────────

        /// <summary>重複呼び出しを防ぎつつ EnableAll を実行する</summary>
        internal static void TryEnableAll()
        {
            if (_enableAllCalled) return;
            _enableAllCalled = true;
            EnableAll();
        }

        internal static void EnableAll()
        {
            foreach (var p in _pending)
            {
                try
                {
                    p.Plugin.InternalEnable(p.Log, p.ConfigPath);
                    _enabled.Add(p.Plugin);
                    _loaderLog.LogInfo(
                        $"[PluginLoader] {p.Plugin.Id} v{p.Plugin.Version} by {p.Plugin.Author}" +
                        $" → {(p.Plugin.IsEnabled ? "有効" : "無効")}");
                }
                catch (Exception ex)
                {
                    _loaderLog.LogError($"[PluginLoader] {p.Plugin.Id} の有効化に失敗: {ex}");
                }
            }
            _pending.Clear();
            _loaderLog.LogInfo($"[PluginLoader] {_enabled.Count} 個のプラグインを有効化しました。");
        }

        internal static void DisableAll()
        {
            for (int i = _enabled.Count - 1; i >= 0; i--)
                _enabled[i].InternalDisable();
            _enabled.Clear();
        }

        // ────────────────────────────────────────────────────────────
        // 内部処理
        // ────────────────────────────────────────────────────────────

        private static void PrepareAssembly(string path, ManualLogSource log)
        {
            Assembly asm;
            try { asm = Assembly.LoadFrom(path); }
            catch (Exception ex)
            {
                log.LogError($"[PluginLoader] Assembly.LoadFrom({Path.GetFileName(path)}) 失敗: {ex.Message}");
                return;
            }

            foreach (var type in GetSafeTypes(asm, log))
            {
                if (!IsPluginType(type)) continue;

                try
                {
                    var plugin     = (IPlugin)Activator.CreateInstance(type)!;
                    var attr       = (DANOPluginAttribute)Attribute.GetCustomAttribute(type, typeof(DANOPluginAttribute))!;
                    var configPath = Path.Combine(ConfigDirectory, $"{attr.Id}.json");
                    var pluginLog  = Logger.CreateLogSource(attr.Id);

                    _pending.Add(new PendingPlugin(plugin, pluginLog, configPath));
                    log.LogInfo($"[PluginLoader] {attr.Id} を準備完了");
                }
                catch (Exception ex)
                {
                    log.LogError($"[PluginLoader] {type.FullName} のインスタンス化に失敗: {ex}");
                }
            }
        }

        private static bool IsPluginType(Type type)
        {
            if (type.IsAbstract || type.IsInterface) return false;
            if (!typeof(IPlugin).IsAssignableFrom(type)) return false;
            if (Attribute.GetCustomAttribute(type, typeof(DANOPluginAttribute)) == null) return false;
            if (type.GetConstructor(Type.EmptyTypes) == null) return false;
            return true;
        }

        private static IEnumerable<Type> GetSafeTypes(Assembly asm, ManualLogSource log)
        {
            try { return asm.GetTypes(); }
            catch (ReflectionTypeLoadException ex)
            {
                log.LogWarning($"[PluginLoader] {asm.GetName().Name}: 一部の型を読み込めませんでした。");
                foreach (var le in ex.LoaderExceptions)
                    if (le != null) log.LogWarning($"  └ {le.Message}");

                var result = new List<Type>();
                foreach (var t in ex.Types)
                    if (t != null) result.Add(t);
                return result;
            }
        }
    }
}
