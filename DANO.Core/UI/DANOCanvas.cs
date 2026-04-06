using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DANO.UI
{
    /// <summary>
    /// DANO が管理するオーバーレイ Canvas。
    /// ゲーム内の他 Canvas より手前に描画される。
    /// </summary>
    internal class DANOCanvas : MonoBehaviour
    {
        internal static DANOCanvas Instance { get; private set; } = null!;

        internal Canvas Canvas { get; private set; } = null!;
        internal RectTransform Root { get; private set; } = null!;

        // ヒント表示用の専用エリア（画面下部中央）
        internal RectTransform HintArea { get; private set; } = null!;
        // 自由配置要素のルート
        internal RectTransform ElementArea { get; private set; } = null!;

        internal static DANOCanvas GetOrCreate()
        {
            if (Instance != null) return Instance;

            var go = new GameObject("[DANO]Canvas");
            DontDestroyOnLoad(go);
            return go.AddComponent<DANOCanvas>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            BuildCanvas();
        }

        private void BuildCanvas()
        {
            Canvas = gameObject.AddComponent<Canvas>();
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Canvas.sortingOrder = 32767; // 最前面

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            gameObject.AddComponent<GraphicRaycaster>();

            Root = GetComponent<RectTransform>();

            // ヒントエリア（画面下部中央）
            HintArea = NewRect("HintArea", Root);
            HintArea.anchorMin = new Vector2(0.1f, 0.04f);
            HintArea.anchorMax = new Vector2(0.9f, 0.22f);
            HintArea.offsetMin = HintArea.offsetMax = Vector2.zero;
            HintArea.gameObject.AddComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.LowerCenter;

            // 自由配置エリア（全画面）
            ElementArea = NewRect("ElementArea", Root);
            ElementArea.anchorMin = Vector2.zero;
            ElementArea.anchorMax = Vector2.one;
            ElementArea.offsetMin = ElementArea.offsetMax = Vector2.zero;
        }

        private static RectTransform NewRect(string name, RectTransform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<RectTransform>();
        }
    }
}
