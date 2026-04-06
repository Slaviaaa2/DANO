using System;
using UnityEngine;

namespace DANO.Events
{
    /// <summary>プレイヤーがスポーンしたときのイベントデータ</summary>
    public class PlayerSpawnedEvent
    {
        public ClientInstance Player { get; }
        public int PlayerId { get; }

        internal PlayerSpawnedEvent(ClientInstance player)
        {
            Player = player;
            PlayerId = player.PlayerId;
        }
    }

    /// <summary>プレイヤーがダメージを受けたときのイベントデータ</summary>
    public class PlayerDamagedEvent
    {
        public PlayerHealth Victim { get; }
        public float Damage { get; internal set; }
        public Transform? Killer { get; }
        /// <summary>trueにするとダメージをキャンセルできる</summary>
        public bool Cancel { get; set; }

        internal PlayerDamagedEvent(PlayerHealth victim, float damage, Transform? killer)
        {
            Victim = victim;
            Damage = damage;
            Killer = killer;
        }
    }

    /// <summary>プレイヤーが死亡したときのイベントデータ</summary>
    public class PlayerDiedEvent
    {
        public PlayerHealth Victim { get; }
        public Transform? Killer { get; }
        public int PlayerId { get; }

        internal PlayerDiedEvent(PlayerHealth victim, Transform? killer, int playerId)
        {
            Victim = victim;
            Killer = killer;
            PlayerId = playerId;
        }
    }

    /// <summary>武器が発射されたときのイベントデータ</summary>
    public class WeaponFiredEvent
    {
        public Weapon Weapon { get; }
        public ClientInstance? Owner { get; }
        /// <summary>trueにすると発射をキャンセルできる</summary>
        public bool Cancel { get; set; }

        internal WeaponFiredEvent(Weapon weapon, ClientInstance? owner)
        {
            Weapon = weapon;
            Owner = owner;
        }
    }
}
