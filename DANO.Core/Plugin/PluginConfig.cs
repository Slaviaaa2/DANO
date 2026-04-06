namespace DANO.Plugin
{
    /// <summary>
    /// 全プラグイン設定の基底クラス。
    /// プロパティを追加するだけで JSON に自動保存/読み込みされる。
    /// </summary>
    public class PluginConfig
    {
        /// <summary>false にするとプラグインが無効化される</summary>
        public bool IsEnabled { get; set; } = true;
    }
}
