using DANO.API;
using DANO.Events;
using HeathenEngineering.SteamworksIntegration;
using TMPro;

namespace DANO.Patches
{
    /// <summary>
    /// チャット関連の Harmony パッチ。
    /// ゲームには2つのチャットパスがある:
    ///   1. LobbyChatUILogic — ロビー画面（Steam Lobby Chat API 経由）
    ///   2. ChatBroadcast    — ゲーム内（FishNet Broadcast 経由）
    ///
    /// ChatMessageSendingEvent は ConnectionMonitor.OnChatSubmit が一元管理。
    /// Harmony パッチはコマンドインターセプトのバックアップと受信イベントのみ担当。
    ///
    /// 削除済み:
    /// - ChatInitPatch (LobbyChatUILogic.Start) — Unity ライフサイクルで発火しない
    /// - MatchChatPatch (MatchChat.Update) — Unity ライフサイクルで発火しない可能性大
    /// </summary>

    /// <summary>
    /// LobbyChatUILogic.OnSendChatMessage() Prefix — ロビーチャットのコマンドインターセプト。
    /// ChatMessageSendingEvent は ConnectionMonitor.OnChatSubmit で発火済みなので、
    /// ここではコマンドインターセプトのバックアップのみ行う。
    /// </summary>
    internal static class ChatSendPatch
    {
        private static bool _logged;

        internal static bool Prefix(TMP_InputField ___inputField)
        {
            if (!_logged)
            {
                _logged = true;
                DANOLoader.Log.LogInfo("[ChatSendPatch] Prefix 初回発火確認！");
            }

            var text = ___inputField?.text?.Trim() ?? "";
            if (string.IsNullOrEmpty(text)) return true;

            // コマンドインターセプト（ConnectionMonitor が先に処理するはずだが、フォールバック）
            if (text.StartsWith("/") && CommandManager.TryExecute(text))
            {
                ___inputField.text = "";
                return false;
            }

            // ChatMessageSendingEvent は ConnectionMonitor.OnChatSubmit で既に発火済み
            // ここで重複発火しない
            return true;
        }
    }

    /// <summary>
    /// LobbyChatUILogic.HandleChatMessage() Prefix — ロビーチャット受信イベント。
    /// </summary>
    internal static class ChatReceivePatch
    {
        internal static void Prefix(LobbyChatMsg message)
        {
            var senderName = message.sender.Name ?? "";
            var text = System.Text.Encoding.UTF8.GetString(message.data ?? System.Array.Empty<byte>());
            EventBus.Raise(new ChatMessageReceivedEvent(senderName, text));
        }
    }

    // ─────────────────────────────────────────────
    //  ゲーム内チャット (ChatBroadcast)
    // ─────────────────────────────────────────────

    /// <summary>
    /// ChatBroadcast.SendMessage() Prefix — ゲーム内チャットのコマンドインターセプト。
    /// ChatMessageSendingEvent は ConnectionMonitor.OnChatSubmit で発火済みなので、
    /// ここではコマンドインターセプトのバックアップのみ行う。
    /// </summary>
    internal static class ChatBroadcastSendPatch
    {
        private static bool _logged;

        internal static bool Prefix(TMP_InputField ___playerMessage)
        {
            if (!_logged)
            {
                _logged = true;
                DANOLoader.Log.LogInfo("[ChatBroadcastSendPatch] Prefix 初回発火確認！");
            }

            var text = ___playerMessage?.text?.Trim() ?? "";
            if (string.IsNullOrEmpty(text)) return true;

            // コマンドインターセプト（ConnectionMonitor が先に処理するはずだが、フォールバック）
            if (text.StartsWith("/") && CommandManager.TryExecute(text))
            {
                ___playerMessage.text = "";
                return false;
            }

            // ChatMessageSendingEvent は ConnectionMonitor.OnChatSubmit で既に発火済み
            // ここで重複発火しない
            return true;
        }
    }

    /// <summary>
    /// ChatBroadcast.OnMessageReceived() Prefix — ゲーム内チャット受信イベント。
    /// </summary>
    internal static class ChatBroadcastReceivePatch
    {
        internal static void Prefix(ChatBroadcast.Message msg)
        {
            EventBus.Raise(new ChatMessageReceivedEvent(msg.username ?? "", msg.message ?? ""));
        }
    }

    // MatchChatPatch (MatchChat.Update Prefix) は Unity ライフサイクルメソッドのため発火しない可能性大。
    // → ConnectionMonitor の onSubmit フックがコマンドインターセプトを担当。削除。
}
