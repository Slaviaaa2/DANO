using System;
using System.Collections.Generic;
using BepInEx.Logging;

namespace DANO.Events
{
    /// <summary>
    /// DANOのイベントバス。
    /// Subscribe&lt;T&gt; でリスナー登録、Raise&lt;T&gt; でイベント発火。
    /// </summary>
    public static class EventBus
    {
        private static readonly Dictionary<Type, List<Delegate>> _handlers = new();
        private static ManualLogSource? _log;

        internal static void Initialize(ManualLogSource log)
        {
            _log = log;
        }

        /// <summary>イベントTのハンドラーを登録する</summary>
        public static void Subscribe<T>(Action<T> handler)
        {
            var type = typeof(T);
            if (!_handlers.TryGetValue(type, out var list))
            {
                list = new List<Delegate>();
                _handlers[type] = list;
            }
            list.Add(handler);
        }

        /// <summary>イベントTのハンドラーを解除する</summary>
        public static void Unsubscribe<T>(Action<T> handler)
        {
            if (_handlers.TryGetValue(typeof(T), out var list))
                list.Remove(handler);
        }

        /// <summary>イベントTを全ハンドラーに発火する</summary>
        public static void Raise<T>(T ev)
        {
            if (!_handlers.TryGetValue(typeof(T), out var list)) return;

            // コピーして反復（発火中のUnsub対策）
            var snapshot = list.ToArray();
            foreach (var del in snapshot)
            {
                try
                {
                    ((Action<T>)del)(ev);
                }
                catch (Exception ex)
                {
                    _log?.LogError($"[EventBus] {typeof(T).Name} ハンドラーで例外: {ex}");
                }
            }
        }
    }
}
