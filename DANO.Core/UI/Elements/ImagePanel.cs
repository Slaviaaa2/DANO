using UnityEngine;
using UnityEngine.UI;

namespace DANO.UI.Elements
{
    /// <summary>
    /// 画面の任意位置に配置できる矩形パネル（背景・区切り線・カラーボックス等に）。
    /// </summary>
    public class ImagePanel
    {
        private readonly GameObject _go;
        private readonly RectTransform _rt;
        private readonly Image _image;

        /// <summary>パネルの色</summary>
        public Color Color
        {
            get => _image.color;
            set => _image.color = value;
        }

        /// <summary>表示・非表示</summary>
        public bool Visible
        {
            get => _go.activeSelf;
            set => _go.SetActive(value);
        }

        /// <param name="anchor">画面上の位置（左下=0,0 / 右上=1,1）</param>
        /// <param name="width">幅（ピクセル基準）</param>
        /// <param name="height">高さ</param>
        /// <param name="color">色（デフォルト半透明黒）</param>
        public ImagePanel(
            Vector2 anchor,
            float width = 200f,
            float height = 100f,
            Color? color = null)
        {
            var canvas = DANOCanvas.GetOrCreate();

            _go = new GameObject("[DANO]ImagePanel");
            _go.transform.SetParent(canvas.ElementArea, false);

            _rt = _go.AddComponent<RectTransform>();
            _rt.anchorMin = anchor;
            _rt.anchorMax = anchor;
            _rt.pivot = anchor;
            _rt.anchoredPosition = Vector2.zero;
            _rt.sizeDelta = new Vector2(width, height);

            _image = _go.AddComponent<Image>();
            _image.color = color ?? new Color(0f, 0f, 0f, 0.5f);
        }

        /// <summary>この要素を画面から削除する</summary>
        public void Destroy() => Object.Destroy(_go);

        /// <summary>アンカー基準からのオフセットを設定する</summary>
        public void SetOffset(Vector2 offset) =>
            _rt.anchoredPosition = offset;

        /// <summary>サイズを変更する</summary>
        public void SetSize(float width, float height) =>
            _rt.sizeDelta = new Vector2(width, height);

        /// <summary>子要素をこのパネルの親にする（パネル上にUIを乗せるとき）</summary>
        public RectTransform RectTransform => _rt;
    }
}
