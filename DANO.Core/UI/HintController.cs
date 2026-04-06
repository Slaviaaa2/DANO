using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace DANO.UI
{
    /// <summary>
    /// ヒントメッセージのキューと表示を管理する MonoBehaviour。
    /// DANOCanvas の HintArea 上に生成される。
    /// </summary>
    internal class HintController : MonoBehaviour
    {
        internal static HintController Instance { get; private set; } = null!;

        private readonly Queue<HintRequest> _queue = new();
        private bool _isShowing;

        internal static HintController GetOrCreate()
        {
            if (Instance != null) return Instance;

            var canvas = DANOCanvas.GetOrCreate();
            var go = new GameObject("[DANO]HintController");
            go.transform.SetParent(canvas.HintArea, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            return go.AddComponent<HintController>();
        }

        private void Awake()
        {
            Instance = this;
        }

        internal void Show(string text, float duration, Color color, float fontSize)
        {
            _queue.Enqueue(new HintRequest(text, duration, color, fontSize));
            if (!_isShowing)
                StartCoroutine(ProcessQueue());
        }

        private IEnumerator ProcessQueue()
        {
            _isShowing = true;
            while (_queue.Count > 0)
            {
                var req = _queue.Dequeue();
                yield return ShowOne(req);
            }
            _isShowing = false;
        }

        private IEnumerator ShowOne(HintRequest req)
        {
            var (go, tmp) = CreateHintObject(req);

            // フェードイン
            float elapsed = 0f;
            const float fadeTime = 0.2f;
            while (elapsed < fadeTime)
            {
                elapsed += Time.unscaledDeltaTime;
                tmp.alpha = Mathf.Clamp01(elapsed / fadeTime);
                yield return null;
            }
            tmp.alpha = 1f;

            // 表示中（フェードアウト開始の少し前まで）
            float remaining = req.Duration - fadeTime * 2;
            if (remaining > 0f)
                yield return new WaitForSecondsRealtime(remaining);

            // フェードアウト
            elapsed = 0f;
            while (elapsed < fadeTime)
            {
                elapsed += Time.unscaledDeltaTime;
                tmp.alpha = 1f - Mathf.Clamp01(elapsed / fadeTime);
                yield return null;
            }

            Destroy(go);
        }

        private static (GameObject go, TMP_Text tmp) CreateHintObject(HintRequest req)
        {
            var canvas = DANOCanvas.GetOrCreate();

            var go = new GameObject("Hint");
            go.transform.SetParent(canvas.HintArea, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 0f);
            rt.sizeDelta = new Vector2(0f, req.FontSize * 1.6f);
            rt.pivot = new Vector2(0.5f, 0f);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = req.Text;
            tmp.fontSize = req.FontSize;
            tmp.color = req.Color;
            tmp.richText = true;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = true;
            tmp.alpha = 0f;

            // TMP 組み込みのアウトラインで視認性を確保
            tmp.outlineWidth = 0.15f;
            tmp.outlineColor = new Color32(0, 0, 0, 200);

            return (go, tmp);
        }

        private readonly struct HintRequest
        {
            internal readonly string Text;
            internal readonly float Duration;
            internal readonly Color Color;
            internal readonly float FontSize;

            internal HintRequest(string text, float duration, Color color, float fontSize)
            {
                Text = text;
                Duration = Mathf.Max(duration, 0.4f);
                Color = color;
                FontSize = fontSize;
            }
        }
    }
}
