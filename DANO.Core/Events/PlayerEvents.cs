namespace DANO.Events
{
    /// <summary>プレイヤーがスポーンしたときのイベント</summary>
    public class PlayerSpawnedEvent
    {
        public API.Player Player { get; }

        internal PlayerSpawnedEvent(int playerId)
        {
            Player = API.Player.Get(playerId);
        }
    }

    /// <summary>プレイヤーがダメージを受けたときのイベント</summary>
    public class PlayerDamagedEvent
    {
        public API.Player? Player { get; }
        public API.Player? Attacker { get; }
        public float Damage { get; set; }
        /// <summary>trueにするとダメージを巻き戻す（HP を回復する）</summary>
        public bool Cancel { get; set; }

        internal PlayerDamagedEvent(PlayerHealth victim, float damage, UnityEngine.Transform? killer)
        {
            Player = API.Player.FromHealth(victim);
            Attacker = API.Player.FromTransform(killer);
            Damage = damage;
        }
    }

    /// <summary>プレイヤーが死亡したときのイベント</summary>
    public class PlayerDiedEvent
    {
        public API.Player? Player { get; }
        public API.Player? Attacker { get; }

        internal PlayerDiedEvent(PlayerHealth victim, UnityEngine.Transform? killer)
        {
            Player = API.Player.FromHealth(victim);
            Attacker = API.Player.FromTransform(killer);
        }
    }

    /// <summary>武器が発射されたときのイベント</summary>
    public class WeaponFiredEvent
    {
        public API.Item? Item { get; }
        public API.Player? Player { get; }
        /// <summary>trueにすると弾数を巻き戻す</summary>
        public bool Cancel { get; set; }

        internal WeaponFiredEvent(Weapon weapon)
        {
            var ib = weapon.GetComponent<ItemBehaviour>();
            Item = ib != null ? API.Item.Get(ib) : null;
            Player = API.Player.Local;
        }
    }
}
