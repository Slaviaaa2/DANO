namespace DANO.API
{
    public class TaserWeapon : Weapon
    {
        public global::Taser TaserBase { get; }

        internal TaserWeapon(global::Taser taser) : base(taser) { TaserBase = taser; }

        public float ChargeTime => TaserBase.chargeTime;
        public float StunTime   => TaserBase.stunTime;

        public override string ToString() => $"TaserWeapon({Name}, Stun={StunTime:F1}s, Ammo={Ammo})";
    }
}
