namespace DANO.API
{
    public class ChargeGunWeapon : Weapon
    {
        public global::ChargeGun ChargeGunBase { get; }

        internal ChargeGunWeapon(global::ChargeGun chargeGun) : base(chargeGun) { ChargeGunBase = chargeGun; }

        public float MaxChargeTime        => ChargeGunBase.maxChargeTime;
        public float AccumulatedPower     => ChargeGunBase.accumulatedPower;
        public bool  HasIntermediateStates => ChargeGunBase.hasIntermediateStates;
        public float Radius               => ChargeGunBase.radius;
        public float PlayerKnockback      => ChargeGunBase.playerKnockback;

        public override string ToString() => $"ChargeGunWeapon({Name}, MaxCharge={MaxChargeTime:F2}s, Ammo={Ammo})";
    }
}
