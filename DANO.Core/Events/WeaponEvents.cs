namespace DANO.Events
{
    /// <summary>武器がリロードされたときのイベント</summary>
    public class WeaponReloadEvent
    {
        public API.Item? Item { get; }
        public API.Player? Player { get; }

        internal WeaponReloadEvent(Weapon weapon)
        {
            var ib = weapon.GetComponent<ItemBehaviour>();
            Item = ib != null ? API.Item.Get(ib) : null;
            Player = API.Player.Local;
        }
    }

    // MeleeHitEvent は削除（ポーリングでの信頼性ある検出方法がないため）
}
