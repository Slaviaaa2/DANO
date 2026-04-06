using DANO.UI;
using DANO.UI.Elements;
using UnityEngine;

namespace DANO.API
{
    /// <summary>
    /// DANO の UI / HUD 操作 API。
    /// Mod から呼び出すエントリーポイント。
    /// </summary>
    public static class HudAPI
    {
        // ────────────────────────────────────────
        // ヒント（一時メッセージ）
        // ────────────────────────────────────────

        /// <summary>
        /// 画面下部に一時的なヒントメッセージを表示する。
        /// 複数呼んだ場合はキューに積まれて順番に表示される。
        /// </summary>
        /// <param name="text">表示テキスト（TMPリッチテキスト可）</param>
        /// <param name="duration">表示秒数</param>
        /// <param name="color">文字色（省略時は白）</param>
        /// <param name="fontSize">フォントサイズ（省略時 28）</param>
        public static void ShowHint(
            string text,
            float duration = 3f,
            Color? color = null,
            float fontSize = 28f)
        {
            HintController.GetOrCreate().Show(text, duration, color ?? Color.white, fontSize);
        }

        // ────────────────────────────────────────
        // ログ・ブロードキャスト
        // ────────────────────────────────────────

        /// <summary>
        /// ゲーム内ログに全員が見えるメッセージを送る（サーバー経由）。
        /// <para>ゲームの MatchLogs システムを使用する。</para>
        /// </summary>
        /// <param name="text">メッセージ（TMPリッチテキスト可）</param>
        public static void Broadcast(string text)
        {
            if (PauseManager.Instance != null)
                PauseManager.Instance.WriteLog(text);
        }

        /// <summary>
        /// ゲーム内ログに自分だけ見えるメッセージを表示する（ネットワーク不使用）。
        /// </summary>
        /// <param name="text">メッセージ（TMPリッチテキスト可）</param>
        public static void LocalMessage(string text)
        {
            if (MatchLogsOffline.Instance != null)
                MatchLogsOffline.Instance.WriteLog(text);
            else if (MatchLogs.Instance != null)
                MatchLogs.Instance.WriteLocalLog(text);
        }

        // ────────────────────────────────────────
        // カスタム要素（永続 UI）
        // ────────────────────────────────────────

        /// <summary>
        /// 画面上に永続テキストラベルを作成して返す。
        /// 不要になったら <see cref="TextLabel.Destroy"/> を呼ぶこと。
        /// </summary>
        /// <param name="anchor">
        /// 画面上の基準点（例: <c>new Vector2(0f, 1f)</c> = 左上、
        /// <c>new Vector2(0.5f, 0.5f)</c> = 中央）
        /// </param>
        /// <param name="text">初期テキスト</param>
        /// <param name="fontSize">フォントサイズ</param>
        /// <param name="color">文字色（省略時は白）</param>
        public static TextLabel CreateTextLabel(
            Vector2 anchor,
            string text = "",
            float fontSize = 24f,
            Color? color = null)
            => new TextLabel(anchor, text, fontSize, color);

        /// <summary>
        /// 画面上にプログレスバーを作成して返す。
        /// 不要になったら <see cref="ProgressBar.Destroy"/> を呼ぶこと。
        /// </summary>
        /// <param name="anchor">画面上の基準点</param>
        /// <param name="width">バー幅（1920x1080基準ピクセル）</param>
        /// <param name="height">バー高さ</param>
        /// <param name="fillColor">バー色（省略時は白）</param>
        /// <param name="backgroundColor">背景色（省略時は半透明黒）</param>
        public static ProgressBar CreateProgressBar(
            Vector2 anchor,
            float width = 300f,
            float height = 20f,
            Color? fillColor = null,
            Color? backgroundColor = null)
            => new ProgressBar(anchor, width, height, fillColor, backgroundColor);

        /// <summary>
        /// 画面上に矩形パネルを作成して返す。
        /// 背景・ボックス・区切りなどに利用する。
        /// 不要になったら <see cref="ImagePanel.Destroy"/> を呼ぶこと。
        /// </summary>
        /// <param name="anchor">画面上の基準点</param>
        /// <param name="width">幅</param>
        /// <param name="height">高さ</param>
        /// <param name="color">色（省略時は半透明黒）</param>
        public static ImagePanel CreatePanel(
            Vector2 anchor,
            float width = 200f,
            float height = 100f,
            Color? color = null)
            => new ImagePanel(anchor, width, height, color);
    }
}
