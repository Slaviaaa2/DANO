namespace DANO.Events
{
    /// <summary>武器がリロードされようとしているときのイベント（Cancel可）</summary>
    public class WeaponReloadingEvent
    {
        public API.Item? Item { get; }
        public API.Player? Player { get; }
        /// <summary>trueにするとリロードをキャンセルする</summary>
        public bool Cancel { get; set; }

        internal WeaponReloadingEvent(Weapon weapon)
        {
            var ib = weapon.GetComponent<ItemBehaviour>();
            Item = ib != null ? API.Item.Get(ib) : null;
            Player = API.Player.Local;
        }
    }

    /// <summary>武器がリロードされた後のイベント（通知のみ）</summary>
    public class WeaponReloadedEvent
    {
        public API.Item? Item { get; }
        public API.Player? Player { get; }

        internal WeaponReloadedEvent(Weapon weapon)
        {
            var ib = weapon.GetComponent<ItemBehaviour>();
            Item = ib != null ? API.Item.Get(ib) : null;
            Player = API.Player.Local;
        }
    }
}
