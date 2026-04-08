namespace DANO.Events
{
    /// <summary>アイテムが拾われたときのイベント</summary>
    public class ItemPickedUpEvent
    {
        public API.Item Item { get; }
        /// <summary>拾ったプレイヤー</summary>
        public API.Player? Player { get; }
        /// <summary>右手で拾ったかどうか</summary>
        public bool RightHand { get; }

        internal ItemPickedUpEvent(ItemBehaviour item, bool isOwner, bool rightHand)
        {
            Item = API.Item.Get(item);
            Player = isOwner ? API.Player.Local : Item.Holder;
            RightHand = rightHand;
        }
    }

    /// <summary>アイテムが落とされたときのイベント</summary>
    public class ItemDroppedEvent
    {
        public API.Item Item { get; }
        /// <summary>落としたプレイヤー</summary>
        public API.Player? Player { get; }

        internal ItemDroppedEvent(ItemBehaviour item)
        {
            Item = API.Item.Get(item);
            Player = Item.Holder;
        }
    }
}
