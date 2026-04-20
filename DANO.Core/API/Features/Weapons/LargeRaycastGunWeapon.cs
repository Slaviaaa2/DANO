using UnityEngine;

namespace DANO.API
{
    public class LargeRaycastGunWeapon : Weapon
    {
        public global::LargeRaycastGun LargeRaycastGunBase { get; }

        internal LargeRaycastGunWeapon(global::LargeRaycastGun gun) : base(gun) { LargeRaycastGunBase = gun; }

        public float   BulletRadius    => LargeRaycastGunBase.bulletRadius;
        public bool    IsBoxcast       => LargeRaycastGunBase.boxcast;
        public Vector3 BoxDimensions   => LargeRaycastGunBase.boxdimensions;
        public float   PlayerKnockback => LargeRaycastGunBase.playerKnockback;

        public override float ReloadTime => LargeRaycastGunBase.reloadTime;

        public override string ToString() => $"LargeRaycastGunWeapon({Name}, Radius={BulletRadius:F2}, Ammo={Ammo})";
    }
}
