using System;

namespace DANO.Plugin
{
    /// <summary>
    /// DANO プラグインであることを示す属性。
    /// このアトリビュートを持つ <see cref="Plugin{TConfig}"/> 派生クラスが自動ロードされる。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class DANOPluginAttribute : Attribute
    {
        /// <summary>プラグインの一意な識別子（例: "my-cool-mod"）</summary>
        public string Id { get; }
        /// <summary>バージョン文字列（例: "1.0.0"）</summary>
        public string Version { get; }
        /// <summary>作者名</summary>
        public string Author { get; }
        /// <summary>プラグインの説明</summary>
        public string Description { get; }

        public DANOPluginAttribute(
            string id,
            string version = "1.0.0",
            string author = "Unknown",
            string description = "")
        {
            Id = id;
            Version = version;
            Author = author;
            Description = description;
        }
    }
}
