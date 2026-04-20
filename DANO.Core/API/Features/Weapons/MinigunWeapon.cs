namespace DANO.API
{
    public class MinigunWeapon : Weapon
    {
        public global::Minigun MinigunBase { get; }

        internal MinigunWeapon(global::Minigun minigun) : base(minigun) { MinigunBase = minigun; }

        public float SpinUpTime    => MinigunBase.timeBeforeShooting;
        public float RotationSpeed => MinigunBase.rotationSpeed;

        public override float ReloadTime => MinigunBase.reloadTime;

        public override string ToString() => $"MinigunWeapon({Name}, SpinUp={SpinUpTime:F2}s, Ammo={Ammo})";
    }
}
