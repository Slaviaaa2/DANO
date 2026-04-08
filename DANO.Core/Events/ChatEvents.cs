namespace DANO.Events
{
    /// <summary>ローカルプレイヤーがチャットメッセージを送信しようとしたときのイベント</summary>
    public class ChatMessageSendingEvent
    {
        public string Username { get; }
        /// <summary>メッセージ内容（プラグインで書き換え可能）</summary>
        public string Message { get; set; }
        /// <summary>trueにすると送信をキャンセルできる</summary>
        public bool Cancel { get; set; }

        internal ChatMessageSendingEvent(string username, string message)
        {
            Username = username;
            Message = message;
        }
    }

    /// <summary>チャットメッセージを受信したときのイベント</summary>
    public class ChatMessageReceivedEvent
    {
        public string Username { get; }
        public string Message { get; }

        internal ChatMessageReceivedEvent(string username, string message)
        {
            Username = username;
            Message = message;
        }
    }
}
