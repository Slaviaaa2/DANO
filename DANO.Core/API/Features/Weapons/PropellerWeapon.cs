namespace DANO.API
{
    public class PropellerWeapon : Weapon
    {
        public global::Propeller PropellerBase { get; }

        internal PropellerWeapon(global::Propeller propeller) : base(propeller) { PropellerBase = propeller; }

        public float FlySpeed      => PropellerBase.flySpeed;
        public float DecelSpeed    => PropellerBase.decelSpeed;
        public float MaxPower      => PropellerBase.maxPower;
        public float Power         => PropellerBase.power;
        public bool  IsFlying      => PropellerBase.isflying;
        public float RotationSpeed => PropellerBase.rotationSpeed;

        public override string ToString() => $"PropellerWeapon({Name}, Flying={IsFlying})";
    }
}
