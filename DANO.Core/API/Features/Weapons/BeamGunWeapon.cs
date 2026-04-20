namespace DANO.API
{
    public class BeamGunWeapon : Weapon
    {
        public global::BeamGun BeamGunBase { get; }

        internal BeamGunWeapon(global::BeamGun beamGun) : base(beamGun) { BeamGunBase = beamGun; }

        public float LaunchForce          => BeamGunBase.launchForce;
        public float PlayerKnockback      => BeamGunBase.playerKnockback;
        public float MaxChargeTime        => BeamGunBase.maxChargeTime;
        public float AccumulatedPower     => BeamGunBase.accumulatedPower;
        public bool  HasIntermediateStates => BeamGunBase.hasIntermediateStates;
        public float Radius               => BeamGunBase.radius;

        public override string ToString() => $"BeamGunWeapon({Name}, Launch={LaunchForce:F1}, Ammo={Ammo})";
    }
}
