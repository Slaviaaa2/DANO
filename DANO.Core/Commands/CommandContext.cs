namespace DANO.API
{
    /// <summary>コマンド実行時にハンドラーに渡されるコンテキスト</summary>
    public class CommandContext
    {
        /// <summary>コマンド名（先頭の / なし）</summary>
        public string CommandName { get; }
        /// <summary>分割された引数</summary>
        public string[] Args { get; }
        /// <summary>コマンド名以降の生テキスト</summary>
        public string RawArgs { get; }
        /// <summary>コマンドを実行したプレイヤー</summary>
        public Player? Sender { get; }

        internal CommandContext(string commandName, string[] args, string rawArgs, Player? sender)
        {
            CommandName = commandName;
            Args = args;
            RawArgs = rawArgs;
            Sender = sender;
        }

        /// <summary>ローカルにメッセージを表示する。改行を含む場合は 1 行ずつ送る。</summary>
        public void Reply(string message)
        {
            foreach (var line in message.Split('\n'))
            {
                var trimmed = line.TrimEnd('\r');
                if (trimmed.Length > 0)
                    HudAPI.LocalMessage(trimmed);
            }
        }
    }
}
