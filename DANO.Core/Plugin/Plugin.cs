using System;
using System.IO;
using BepInEx.Logging;
using Newtonsoft.Json;

namespace DANO.Plugin
{
    /// <summary>
    /// DANO プラグインの基底クラス。
    /// <typeparamref name="TConfig"/> を設定型として指定する。
    /// 設定不要なら <see cref="PluginConfig"/> をそのまま使う。
    /// </summary>
    /// <example>
    /// <code>
    /// [DANOPlugin("my-mod", "1.0.0", "MyName", "説明")]
    /// public class MyMod : Plugin&lt;MyMod.Cfg&gt;
    /// {
    ///     public override void OnEnabled()
    ///     {
    ///         Log.LogInfo("MyMod enabled!");
    ///         EventBus.Subscribe&lt;PlayerDiedEvent&gt;(OnDied);
    ///     }
    ///     public override void OnDisabled()
    ///         => EventBus.Unsubscribe&lt;PlayerDiedEvent&gt;(OnDied);
    ///
    ///     void OnDied(PlayerDiedEvent ev) { }
    ///
    ///     public class Cfg : PluginConfig
    ///     {
    ///         public string Message { get; set; } = "Hello!";
    ///     }
    /// }
    /// </code>
    /// </example>
    public abstract class Plugin<TConfig> : IPlugin
        where TConfig : PluginConfig, new()
    {
        private static readonly JsonSerializerSettings _jsonSettings = new()
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Include,
        };

        // ────────── プラグイン情報 ──────────

        /// <summary>プラグインの一意な識別子</summary>
        public string Id { get; private set; } = "";
        /// <summary>バージョン</summary>
        public string Version { get; private set; } = "";
        /// <summary>作者</summary>
        public string Author { get; private set; } = "";
        /// <summary>説明</summary>
        public string Description { get; private set; } = "";
        /// <summary>現在有効かどうか</summary>
        public bool IsEnabled { get; private set; }

        // ────────── プラグイン開発者が使うメンバー ──────────

        /// <summary>このプラグイン専用のロガー（BepInEx ManualLogSource）</summary>
        protected ManualLogSource Log { get; private set; } = null!;

        /// <summary>構造化ロガー（Debug/Info/Warning/Error メソッド付き）</summary>
        protected DANOLogger Logger { get; private set; } = null!;

        /// <summary>
        /// 設定オブジェクト。
        /// <see cref="OnEnabled"/> の前に JSON から読み込まれる。
        /// </summary>
        protected TConfig Config { get; private set; } = null!;

        // ────────── 実装必須メソッド ──────────

        /// <summary>プラグイン有効化時に呼ばれる。イベント購読はここで行う。</summary>
        public abstract void OnEnabled();

        /// <summary>プラグイン無効化時に呼ばれる。イベント解除はここで行う。</summary>
        public abstract void OnDisabled();

        // ────────── 内部処理 ──────────

        void IPlugin.InternalEnable(ManualLogSource log, string configPath)
        {
            // アトリビュートからメタデータを読む
            var attr = (DANOPluginAttribute?)Attribute.GetCustomAttribute(GetType(), typeof(DANOPluginAttribute));
            Id          = attr?.Id          ?? GetType().Name;
            Version     = attr?.Version     ?? "?";
            Author      = attr?.Author      ?? "?";
            Description = attr?.Description ?? "";

            Log = log;
            Logger = new DANOLogger(log);
            Config = LoadOrCreateConfig(configPath);

            if (!Config.IsEnabled)
            {
                Log.LogInfo($"[{Id}] IsEnabled=false のためスキップします。");
                IsEnabled = false;
                return;
            }

            IsEnabled = true;
            try
            {
                OnEnabled();
            }
            catch (Exception ex)
            {
                Log.LogError($"[{Id}] OnEnabled() で例外: {ex}");
                IsEnabled = false;
            }
        }

        void IPlugin.InternalDisable()
        {
            if (!IsEnabled) return;
            try
            {
                OnDisabled();
            }
            catch (Exception ex)
            {
                Log.LogError($"[{Id}] OnDisabled() で例外: {ex}");
            }
            IsEnabled = false;
        }

        private TConfig LoadOrCreateConfig(string configPath)
        {
            try
            {
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    var cfg = JsonConvert.DeserializeObject<TConfig>(json, _jsonSettings);
                    if (cfg != null)
                    {
                        // 新しいプロパティが追加された場合に備えて上書き保存
                        File.WriteAllText(configPath, JsonConvert.SerializeObject(cfg, _jsonSettings));
                        return cfg;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogWarning($"[{Id}] 設定ファイルの読み込みに失敗: {ex.Message}");
            }

            var defaultCfg = new TConfig();
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
                File.WriteAllText(configPath, JsonConvert.SerializeObject(defaultCfg, _jsonSettings));
                Log.LogInfo($"[{Id}] デフォルト設定を作成: {configPath}");
            }
            catch (Exception ex)
            {
                Log.LogWarning($"[{Id}] 設定ファイルの書き込みに失敗: {ex.Message}");
            }
            return defaultCfg;
        }
    }
}
