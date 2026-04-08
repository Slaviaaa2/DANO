using System;
using System.Collections.Generic;
using BepInEx.Logging;

namespace DANO.Events
{
    /// <summary>
    /// DANOのイベントバス。
    /// Subscribe&lt;T&gt; でリスナー登録、Raise&lt;T&gt; でイベント発火。
    /// priority が小さいほど先に実行される。
    /// </summary>
    public static class EventBus
    {
        private struct HandlerEntry
        {
            public int Priority;
            public Delegate Handler;
        }

        private static readonly Dictionary<Type, List<HandlerEntry>> _handlers = new();
        private static ManualLogSource? _log;

        internal static void Initialize(ManualLogSource log)
        {
            _log = log;
        }

        /// <summary>イベントTのハンドラーを優先度0で登録する</summary>
        public static void Subscribe<T>(Action<T> handler)
        {
            Subscribe(handler, 0);
        }

        /// <summary>イベントTのハンドラーを指定優先度で登録する（小さいほど先に実行）</summary>
        public static void Subscribe<T>(Action<T> handler, int priority)
        {
            var type = typeof(T);
            if (!_handlers.TryGetValue(type, out var list))
            {
                list = new List<HandlerEntry>();
                _handlers[type] = list;
            }

            var entry = new HandlerEntry { Priority = priority, Handler = handler };

            // 優先度順に挿入
            int insertIndex = list.Count;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Priority > priority)
                {
                    insertIndex = i;
                    break;
                }
            }
            list.Insert(insertIndex, entry);
        }

        /// <summary>イベントTのハンドラーを解除する</summary>
        public static void Unsubscribe<T>(Action<T> handler)
        {
            if (!_handlers.TryGetValue(typeof(T), out var list)) return;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (list[i].Handler.Equals(handler))
                {
                    list.RemoveAt(i);
                    break;
                }
            }
        }

        /// <summary>全イベント型の登録ハンドラー総数を返す</summary>
        public static int GetSubscriberCount()
        {
            int count = 0;
            foreach (var list in _handlers.Values)
                count += list.Count;
            return count;
        }

        /// <summary>イベントTを全ハンドラーに発火する（優先度順）</summary>
        public static void Raise<T>(T ev)
        {
            if (!_handlers.TryGetValue(typeof(T), out var list)) return;

            // コピーして反復（発火中のUnsub対策）
            var snapshot = list.ToArray();
            foreach (var entry in snapshot)
            {
                try
                {
                    ((Action<T>)entry.Handler)(ev);
                }
                catch (Exception ex)
                {
                    _log?.LogError($"[EventBus] {typeof(T).Name} ハンドラーで例外: {ex}");
                }
            }
        }
    }
}
