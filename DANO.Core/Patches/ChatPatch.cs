using DANO.API;
using DANO.Events;
using HeathenEngineering.SteamworksIntegration;
using TMPro;
using UnityEngine;
using Input = UnityEngine.Input;

namespace DANO.Patches
{
    /// <summary>
    /// チャット関連の Harmony パッチ。
    /// ゲームには2つのチャットパスがある:
    ///   1. LobbyChatUILogic — ロビー画面（Steam Lobby Chat API 経由）
    ///   2. ChatBroadcast    — ゲーム内（FishNet Broadcast 経由）
    /// 両方をフックして、コマンドインターセプトとイベント発火を行う。
    /// </summary>
    internal static class ChatPatches
    {
        internal static LobbyManager? _lobbyManager;

        /// <summary>Steam Lobby Chat 経由でメッセージを送信する</summary>
        internal static void SendLobbyChat(string message)
        {
            if (_lobbyManager?.Lobby != null)
                _lobbyManager.Lobby.SendChatMessage(message);
        }
    }

    // ─────────────────────────────────────────────
    //  ロビーチャット (LobbyChatUILogic)
    // ─────────────────────────────────────────────

    /// <summary>
    /// LobbyChatUILogic.Start() Postfix — LobbyManager をキャプチャ。
    /// </summary>
    internal static class ChatInitPatch
    {
        internal static void Postfix(LobbyManager ___lobbyManager)
        {
            ChatPatches._lobbyManager = ___lobbyManager;
            DANOLoader.Log.LogInfo("[ChatInitPatch] LobbyManager キャプチャ完了");
        }
    }

    /// <summary>
    /// LobbyChatUILogic.OnSendChatMessage() Prefix — ロビーチャットのコマンドインターセプト。
    /// </summary>
    internal static class ChatSendPatch
    {
        internal static bool Prefix(TMP_InputField ___inputField)
        {
            var text = ___inputField?.text?.Trim() ?? "";
            if (string.IsNullOrEmpty(text)) return true;

            DANOLoader.Log.LogInfo($"[ChatSendPatch] ロビーチャット インターセプト: \"{text}\"");

            if (text.StartsWith("/") && CommandManager.TryExecute(text))
            {
                ___inputField.text = "";
                return false;
            }

            var username = ClientInstance.Instance?.PlayerName ?? "";
            var ev = new ChatMessageSendingEvent(username, text);
            EventBus.Raise(ev);

            if (ev.Cancel)
            {
                ___inputField.text = "";
                return false;
            }

            if (ev.Message != text)
                ___inputField.text = ev.Message;

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
    /// ChatBroadcast は FishNet Broadcast でメッセージを送信する。
    /// playerMessage フィールドに入力テキストが入っている。
    /// </summary>
    internal static class ChatBroadcastSendPatch
    {
        internal static bool Prefix(TMP_InputField ___playerMessage)
        {
            var text = ___playerMessage?.text?.Trim() ?? "";
            if (string.IsNullOrEmpty(text)) return true;

            DANOLoader.Log.LogInfo($"[ChatBroadcastSendPatch] ゲーム内チャット インターセプト: \"{text}\"");

            if (text.StartsWith("/") && CommandManager.TryExecute(text))
            {
                ___playerMessage.text = "";
                return false;
            }

            var username = ClientInstance.Instance?.PlayerName ?? "";
            var ev = new ChatMessageSendingEvent(username, text);
            EventBus.Raise(ev);

            if (ev.Cancel)
            {
                ___playerMessage.text = "";
                return false;
            }

            if (ev.Message != text)
                ___playerMessage.text = ev.Message;

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

    // ─────────────────────────────────────────────
    //  MatchChat（UI制御）
    // ─────────────────────────────────────────────

    /// <summary>
    /// MatchChat.Update() Prefix — コマンドインターセプトの最終防衛線。
    /// MatchChat.Update は Enter キーで ChatBox の表示/非表示をトグルするだけ。
    /// ChatBroadcast が存在しない場合（演習場等）でも、ここで直接テキストを処理する。
    /// Prefix なので、ChatBox.activeSelf がまだ true の状態で Enter を検出できる。
    /// </summary>
    internal static class MatchChatPatch
    {
        private static bool _logged;

        internal static void Prefix(GameObject ___ChatBox, TMP_InputField ___inputLine)
        {
            if (!_logged)
            {
                _logged = true;
                DANOLoader.Log.LogInfo("[MatchChatPatch] Prefix 初回発火確認");
            }

            // / キーでチャットを開いてプレフィックスを自動入力
            if (Input.GetKeyDown(KeyCode.Slash) && !___ChatBox.activeSelf)
            {
                ___ChatBox.SetActive(true);
                ___inputLine.text = "/";
                ___inputLine.MoveToEndOfLine(false, true);
                return;
            }

            // Enter キーが押された & チャットが開いている → ユーザーがメッセージを送信しようとしている
            // （元の Update() が ChatBox.SetActive(false) する前にテキストを処理する）
            if (Input.GetKeyDown(KeyCode.Return) && ___ChatBox.activeSelf)
            {
                var text = ___inputLine?.text?.Trim() ?? "";
                if (!string.IsNullOrEmpty(text))
                {
                    DANOLoader.Log.LogInfo($"[MatchChatPatch] チャット入力検出: \"{text}\"");

                    // コマンドインターセプト
                    if (text.StartsWith("/") && CommandManager.TryExecute(text))
                    {
                        ___inputLine.text = "";
                    }
                    else
                    {
                        // チャット送信イベント
                        var username = ClientInstance.Instance?.PlayerName ?? "";
                        var ev = new ChatMessageSendingEvent(username, text);
                        EventBus.Raise(ev);

                        if (ev.Cancel)
                            ___inputLine.text = "";
                        else if (ev.Message != text)
                            ___inputLine.text = ev.Message;
                    }
                }
            }
        }
    }
}
