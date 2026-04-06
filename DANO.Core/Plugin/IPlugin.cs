namespace DANO.Plugin
{
    /// <summary>プラグインローダーが使う非ジェネリックなインターフェース（内部用）</summary>
    internal interface IPlugin
    {
        string Id { get; }
        string Version { get; }
        string Author { get; }
        string Description { get; }
        bool IsEnabled { get; }

        /// <summary>ローダーがコンフィグ注入後に呼ぶ初期化メソッド</summary>
        void InternalEnable(BepInEx.Logging.ManualLogSource log, string configPath);
        void InternalDisable();
    }
}
