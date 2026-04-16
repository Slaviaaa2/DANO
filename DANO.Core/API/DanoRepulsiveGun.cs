using UnityEngine;

namespace DANO.API
{
    /// <summary>反発銃のラッパー（敵を押し飛ばす）</summary>
    public class DanoRepulsiveGun : DanoWeapon
    {
        public RepulsiveGun RepulsiveGunBase { get; }

        internal DanoRepulsiveGun(RepulsiveGun gun) : base(gun) { RepulsiveGunBase = gun; }

        /// <summary>反発力</summary>
        public float RepulseForce => RepulsiveGunBase.repulseForce;

        /// <summary>プレイヤーノックバック力</summary>
        public float PlayerKnockback => RepulsiveGunBase.playerKnockback;

        /// <summary>判定ボックスの寸法</summary>
        public Vector3 BoxDimensions => RepulsiveGunBase.boxdimensions;

        /// <summary>リロード時間（秒）</summary>
        public override float ReloadTime => RepulsiveGunBase.reloadTime;

        public override string ToString() => $"DanoRepulsiveGun({Name}, Repulse={RepulseForce:F1}, Ammo={Ammo})";
    }
}
