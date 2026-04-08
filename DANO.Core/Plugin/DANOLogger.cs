using System;
using BepInEx.Logging;

namespace DANO.Plugin
{
    /// <summary>プラグイン向け構造化ロガー</summary>
    public class DANOLogger
    {
        private readonly ManualLogSource _source;

        internal DANOLogger(ManualLogSource source)
        {
            _source = source;
        }

        public void Debug(string message)   => _source.LogDebug(message);
        public void Info(string message)    => _source.LogInfo(message);
        public void Warning(string message) => _source.LogWarning(message);
        public void Error(string message)   => _source.LogError(message);

        public void Error(string message, Exception ex) =>
            _source.LogError($"{message}: {ex}");
    }
}
