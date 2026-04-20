using UnityEngine;

namespace DANO.API
{
    public class RepulsiveGunWeapon : Weapon
    {
        public global::RepulsiveGun RepulsiveGunBase { get; }

        internal RepulsiveGunWeapon(global::RepulsiveGun gun) : base(gun) { RepulsiveGunBase = gun; }

        public float   RepulseForce    => RepulsiveGunBase.repulseForce;
        public float   PlayerKnockback => RepulsiveGunBase.playerKnockback;
        public Vector3 BoxDimensions   => RepulsiveGunBase.boxdimensions;

        public override float ReloadTime => RepulsiveGunBase.reloadTime;

        public override string ToString() => $"RepulsiveGunWeapon({Name}, Repulse={RepulseForce:F1}, Ammo={Ammo})";
    }
}
