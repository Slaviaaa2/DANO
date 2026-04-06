using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DANO.UI.Elements
{
    /// <summary>
    /// 画面の任意位置に配置できる永続テキスト要素。
    /// <para>pivot / anchor は <see cref="UnityEngine.Vector2"/> で指定（0〜1）。</para>
    /// </summary>
    public class TextLabel
    {
        private readonly GameObject _go;
        private readonly RectTransform _rt;
        private readonly TextMeshProUGUI _tmp;

        /// <summary>表示テキスト</summary>
        public string Text
        {
            get => _tmp.text;
            set => _tmp.text = value;
        }

        /// <summary>フォントサイズ</summary>
        public float FontSize
        {
            get => _tmp.fontSize;
            set => _tmp.fontSize = value;
        }

        /// <summary>文字色</summary>
        public Color Color
        {
            get => _tmp.color;
            set => _tmp.color = value;
        }

        /// <summary>表示・非表示</summary>
        public bool Visible
        {
            get => _go.activeSelf;
            set => _go.SetActive(value);
        }

        /// <summary>
        /// テキストラベルを作成する。
        /// </summary>
        /// <param name="anchor">画面上の位置（左下=0,0 / 右上=1,1）</param>
        /// <param name="text">初期テキスト</param>
        /// <param name="fontSize">フォントサイズ</param>
        /// <param name="color">文字色</param>
        /// <param name="alignment">テキスト揃え</param>
        public TextLabel(
            Vector2 anchor,
            string text = "",
            float fontSize = 24f,
            Color? color = null,
            TextAlignmentOptions alignment = TextAlignmentOptions.TopLeft)
        {
            var canvas = DANOCanvas.GetOrCreate();

            _go = new GameObject("[DANO]TextLabel");
            _go.transform.SetParent(canvas.ElementArea, false);

            _rt = _go.AddComponent<RectTransform>();
            _rt.anchorMin = anchor;
            _rt.anchorMax = anchor;
            _rt.pivot = anchor;
            _rt.anchoredPosition = Vector2.zero;
            _rt.sizeDelta = new Vector2(600f, 200f);

            _tmp = _go.AddComponent<TextMeshProUGUI>();
            _tmp.text = text;
            _tmp.fontSize = fontSize;
            _tmp.color = color ?? Color.white;
            _tmp.alignment = alignment;
            _tmp.enableWordWrapping = true;

            // TMP 組み込みのアウトラインで視認性を確保
            _tmp.outlineWidth = 0.15f;
            _tmp.outlineColor = new Color32(0, 0, 0, 180);
        }

        /// <summary>この要素を画面から削除する</summary>
        public void Destroy() => Object.Destroy(_go);

        /// <summary>表示サイズを上書きする（ピクセル単位）</summary>
        public void SetSize(float width, float height) =>
            _rt.sizeDelta = new Vector2(width, height);

        /// <summary>アンカー基準からのオフセット（ピクセル）を設定する</summary>
        public void SetOffset(Vector2 offset) =>
            _rt.anchoredPosition = offset;
    }
}
