namespace DANO.Events
{
    /// <summary>アイテムが拾われたときのイベント</summary>
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

    /// <summary>アイテムが落とされたときのイベント</summary>
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
