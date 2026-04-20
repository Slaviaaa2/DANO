using System;
using System.Collections.Generic;
using UnityEngine;

namespace DANO.API
{
    /// <summary>
    /// プラグイン向けタイマーユーティリティ。
    /// Update ループで駆動される遅延実行・繰り返し実行を提供する。
    /// </summary>
    public static class Timer
    {
        private static readonly List<TimerEntry> _timers = new List<TimerEntry>();
        private static readonly List<TimerEntry> _toAdd = new List<TimerEntry>();
        private static readonly List<TimerEntry> _toRemove = new List<TimerEntry>();
        private static int _nextId;

        /// <summary>指定秒数後に一度だけ実行する</summary>
        public static int After(float delay, Action callback)
        {
            var entry = new TimerEntry(++_nextId, delay, 0f, 1, callback);
            _toAdd.Add(entry);
            return entry.Id;
        }

        /// <summary>指定間隔で繰り返し実行する（-1 = 無限）</summary>
        public static int Every(float interval, Action callback, int repeatCount = -1)
        {
            var entry = new TimerEntry(++_nextId, interval, interval, repeatCount, callback);
            _toAdd.Add(entry);
            return entry.Id;
        }

        /// <summary>指定秒数後に開始し、指定間隔で繰り返し実行する</summary>
        public static int AfterThenEvery(float initialDelay, float interval, Action callback, int repeatCount = -1)
        {
            var entry = new TimerEntry(++_nextId, initialDelay, interval, repeatCount, callback);
            _toAdd.Add(entry);
            return entry.Id;
        }

        /// <summary>次のフレームで実行する</summary>
        public static int NextFrame(Action callback)
        {
            return After(0f, callback);
        }

        /// <summary>タイマーをキャンセルする</summary>
        public static void Cancel(int timerId)
        {
            for (int i = 0; i < _timers.Count; i++)
            {
                if (_timers[i].Id == timerId)
                {
                    _toRemove.Add(_timers[i]);
                    return;
                }
            }
            _toAdd.RemoveAll(t => t.Id == timerId);
        }

        /// <summary>全タイマーをキャンセルする</summary>
        public static void CancelAll()
        {
            _timers.Clear();
            _toAdd.Clear();
            _toRemove.Clear();
        }

        /// <summary>DANOSentinel.Update() から毎フレーム呼ばれる</summary>
        internal static void Tick()
        {
            if (_toAdd.Count > 0)
            {
                _timers.AddRange(_toAdd);
                _toAdd.Clear();
            }

            float dt = Time.deltaTime;

            for (int i = 0; i < _timers.Count; i++)
            {
                var t = _timers[i];
                t.Remaining -= dt;

                if (t.Remaining <= 0f)
                {
                    try
                    {
                        t.Callback.Invoke();
                    }
                    catch (Exception ex)
                    {
                        DANOLoader.Log.LogError($"[Timer] タイマー #{t.Id} でエラー: {ex}");
                    }

                    t.ExecutedCount++;

                    if (t.RepeatCount > 0 && t.ExecutedCount >= t.RepeatCount)
                    {
                        _toRemove.Add(t);
                    }
                    else
                    {
                        t.Remaining = t.Interval;
                    }
                }
            }

            if (_toRemove.Count > 0)
            {
                foreach (var r in _toRemove)
                    _timers.Remove(r);
                _toRemove.Clear();
            }
        }

        private class TimerEntry
        {
            public int Id;
            public float Remaining;
            public float Interval;
            public int RepeatCount;
            public int ExecutedCount;
            public Action Callback;

            public TimerEntry(int id, float initialDelay, float interval, int repeatCount, Action callback)
            {
                Id = id;
                Remaining = initialDelay;
                Interval = interval;
                RepeatCount = repeatCount;
                ExecutedCount = 0;
                Callback = callback;
            }
        }
    }
}
