namespace DANO.API
{
    public class GunWeapon : Weapon
    {
        public global::Gun GunBase { get; }

        internal GunWeapon(global::Gun gun) : base(gun) { GunBase = gun; }

        public override float ReloadTime => GunBase.reloadTime;

        public override string ToString() => $"GunWeapon({Name}, Ammo={Ammo})";
    }
}
