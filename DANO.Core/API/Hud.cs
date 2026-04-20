using DANO.UI;
using DANO.UI.Elements;
using UnityEngine;

namespace DANO.API
{
    /// <summary>
    /// DANO の UI / HUD 操作 API。
    /// Mod から呼び出すエントリーポイント。
    /// </summary>
    public static class Hud
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
        /// </summary>
        public static void Broadcast(string text)
        {
            if (PauseManager.Instance != null)
                PauseManager.Instance.WriteLog(text);
        }

        /// <summary>
        /// ゲーム内ログに自分だけ見えるメッセージを表示する（ネットワーク不使用）。
        /// </summary>
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
        public static ProgressBar CreateProgressBar(
            Vector2 anchor,
            float width = 300f,
            float height = 20f,
            Color? fillColor = null,
            Color? backgroundColor = null)
            => new ProgressBar(anchor, width, height, fillColor, backgroundColor);

        /// <summary>
        /// 画面上に矩形パネルを作成して返す。
        /// 不要になったら <see cref="ImagePanel.Destroy"/> を呼ぶこと。
        /// </summary>
        public static ImagePanel CreatePanel(
            Vector2 anchor,
            float width = 200f,
            float height = 100f,
            Color? color = null)
            => new ImagePanel(anchor, width, height, color);
    }
}
