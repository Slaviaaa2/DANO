namespace DANO.API
{
    public class ShotgunWeapon : Weapon
    {
        public global::Shotgun ShotgunBase { get; }

        internal ShotgunWeapon(global::Shotgun shotgun) : base(shotgun) { ShotgunBase = shotgun; }

        public int PelletCount => ShotgunBase.bulletAmount;
        public float Spread    => ShotgunBase.spread;

        public override float ReloadTime => ShotgunBase.reloadTime;

        public override string ToString() => $"ShotgunWeapon({Name}, Pellets={PelletCount}, Ammo={Ammo})";
    }
}
