namespace DANO.API
{
    public class BumpGunWeapon : Weapon
    {
        public global::BumpGun BumpGunBase { get; }

        internal BumpGunWeapon(global::BumpGun bumpGun) : base(bumpGun) { BumpGunBase = bumpGun; }

        public float LaunchForce     => BumpGunBase.launchForce;
        public float PlayerKnockback => BumpGunBase.playerKnockback;

        public override string ToString() => $"BumpGunWeapon({Name}, Launch={LaunchForce:F1}, Ammo={Ammo})";
    }
}
