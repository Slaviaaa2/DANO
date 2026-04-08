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

    /// <summary>近接武器がプレイヤーにヒットしたときのイベント</summary>
    public class MeleeHitEvent
    {
        public API.Player? Attacker { get; }
        public API.Player? Victim { get; }
        public API.Item? Item { get; }

        internal MeleeHitEvent(MeleeWeapon weapon, PlayerHealth enemyHealth)
        {
            var ib = weapon.GetComponent<ItemBehaviour>();
            Item = ib != null ? API.Item.Get(ib) : null;
            Attacker = API.Player.Local;
            Victim = API.Player.FromHealth(enemyHealth);
        }
    }
}
