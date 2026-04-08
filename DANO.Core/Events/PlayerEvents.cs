using UnityEngine;

namespace DANO.Events
{
    /// <summary>プレイヤーがスポーンしたときのイベント</summary>
    public class PlayerSpawnedEvent
    {
        public API.Player Player { get; }

        internal PlayerSpawnedEvent(ClientInstance player)
        {
            Player = API.Player.Get(player);
        }
    }

    /// <summary>プレイヤーがダメージを受けたときのイベント</summary>
    public class PlayerDamagedEvent
    {
        public API.Player? Player { get; }
        public API.Player? Attacker { get; }
        public float Damage { get; set; }
        /// <summary>trueにするとダメージをキャンセルできる</summary>
        public bool Cancel { get; set; }

        internal PlayerDamagedEvent(PlayerHealth victim, float damage, Transform? killer)
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

        internal PlayerDiedEvent(PlayerHealth victim, Transform? killer)
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
        /// <summary>trueにすると発射をキャンセルできる</summary>
        public bool Cancel { get; set; }

        internal WeaponFiredEvent(Gun gun, ClientInstance? owner)
        {
            var ib = gun.GetComponent<ItemBehaviour>();
            Item = ib != null ? API.Item.Get(ib) : null;
            Player = owner != null ? API.Player.Get(owner) : null;
        }
    }
}
