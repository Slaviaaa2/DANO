namespace DANO.Events
{
    /// <summary>アイテムが拾われようとしているときのイベント（Cancel可）</summary>
    public class ItemPickingUpEvent
    {
        public API.Item Item { get; }
        /// <summary>拾おうとしているプレイヤー</summary>
        public API.Player? Player { get; }
        /// <summary>trueにするとアイテム拾得をキャンセルする</summary>
        public bool Cancel { get; set; }

        internal ItemPickingUpEvent(ItemBehaviour item)
        {
            Item = API.Item.Get(item);
            Player = API.Player.Local;
        }
    }

    /// <summary>アイテムが拾われた後のイベント（通知のみ）</summary>
    public class ItemPickedUpEvent
    {
        public API.Item Item { get; }
        /// <summary>拾ったプレイヤー</summary>
        public API.Player? Player { get; }

        internal ItemPickedUpEvent(ItemBehaviour item)
        {
            Item = API.Item.Get(item);
            Player = Item.Holder;
        }
    }

    /// <summary>アイテムが落とされようとしているときのイベント（Cancel可）</summary>
    public class ItemDroppingEvent
    {
        public API.Item Item { get; }
        /// <summary>落とそうとしているプレイヤー</summary>
        public API.Player? Player { get; }
        /// <summary>trueにするとアイテム投棄をキャンセルする</summary>
        public bool Cancel { get; set; }

        internal ItemDroppingEvent(ItemBehaviour item, API.Player? lastHolder)
        {
            Item = API.Item.Get(item);
            Player = lastHolder;
        }
    }

    /// <summary>アイテムが落とされた後のイベント（通知のみ）</summary>
    public class ItemDroppedEvent
    {
        public API.Item Item { get; }
        /// <summary>落としたプレイヤー（ドロップ後は null になる可能性がある）</summary>
        public API.Player? Player { get; }

        internal ItemDroppedEvent(ItemBehaviour item, API.Player? lastHolder)
        {
            Item = API.Item.Get(item);
            Player = lastHolder;
        }
    }
}
