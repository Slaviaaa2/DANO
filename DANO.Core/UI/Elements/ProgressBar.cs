using UnityEngine;
using UnityEngine.UI;

namespace DANO.UI.Elements
{
    /// <summary>
    /// 画面の任意位置に配置できるプログレスバー要素。
    /// ヘルスバー・タイマー・チャージ表示などに使用する。
    /// </summary>
    public class ProgressBar
    {
        private readonly GameObject _go;
        private readonly RectTransform _rt;
        private readonly Image _fill;
        private float _value = 1f;

        /// <summary>0〜1 の進捗値（クランプされる）</summary>
        public float Value
        {
            get => _value;
            set
            {
                _value = Mathf.Clamp01(value);
                _fill.fillAmount = _value;
            }
        }

        /// <summary>バーの背景色</summary>
        public Color BackgroundColor
        {
            get => _go.GetComponent<Image>().color;
            set => _go.GetComponent<Image>().color = value;
        }

        /// <summary>バーの前景（fill）色</summary>
        public Color FillColor
        {
            get => _fill.color;
            set => _fill.color = value;
        }

        /// <summary>表示・非表示</summary>
        public bool Visible
        {
            get => _go.activeSelf;
            set => _go.SetActive(value);
        }

        /// <summary>
        /// プログレスバーを作成する。
        /// </summary>
        /// <param name="anchor">画面上の位置（左下=0,0 / 右上=1,1）</param>
        /// <param name="width">バー幅（ピクセル基準 1920x1080）</param>
        /// <param name="height">バー高さ</param>
        /// <param name="fillColor">バー色（デフォルト白）</param>
        /// <param name="backgroundColor">背景色（デフォルト半透明黒）</param>
        public ProgressBar(
            Vector2 anchor,
            float width = 300f,
            float height = 20f,
            Color? fillColor = null,
            Color? backgroundColor = null)
        {
            var canvas = DANOCanvas.GetOrCreate();

            // 背景
            _go = new GameObject("[DANO]ProgressBar");
            _go.transform.SetParent(canvas.ElementArea, false);

            _rt = _go.AddComponent<RectTransform>();
            _rt.anchorMin = anchor;
            _rt.anchorMax = anchor;
            _rt.pivot = anchor;
            _rt.anchoredPosition = Vector2.zero;
            _rt.sizeDelta = new Vector2(width, height);

            var bg = _go.AddComponent<Image>();
            bg.color = backgroundColor ?? new Color(0f, 0f, 0f, 0.5f);

            // フィル
            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(_go.transform, false);

            var fillRt = fillGo.AddComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = new Vector2(1f, 1f);
            fillRt.offsetMin = new Vector2(2f, 2f);
            fillRt.offsetMax = new Vector2(-2f, -2f);

            _fill = fillGo.AddComponent<Image>();
            _fill.color = fillColor ?? Color.white;
            _fill.type = Image.Type.Filled;
            _fill.fillMethod = Image.FillMethod.Horizontal;
            _fill.fillOrigin = 0; // 左から
            _fill.fillAmount = 1f;
        }

        /// <summary>この要素を画面から削除する</summary>
        public void Destroy() => Object.Destroy(_go);

        /// <summary>アンカー基準からのオフセット（ピクセル）を設定する</summary>
        public void SetOffset(Vector2 offset) =>
            _rt.anchoredPosition = offset;

        /// <summary>サイズを変更する</summary>
        public void SetSize(float width, float height) =>
            _rt.sizeDelta = new Vector2(width, height);
    }
}
