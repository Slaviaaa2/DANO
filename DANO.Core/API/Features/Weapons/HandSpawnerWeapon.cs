namespace DANO.API
{
    public class HandSpawnerWeapon : Weapon
    {
        public global::WeaponHandSpawner SpawnerBase { get; }

        internal HandSpawnerWeapon(global::WeaponHandSpawner spawner) : base(spawner) { SpawnerBase = spawner; }

        public bool  IsProximityMine      => SpawnerBase.proximityMine;
        public bool  IsClaymore           => SpawnerBase.claymore;
        public bool  IsAPMine             => SpawnerBase.apmine;
        public bool  CanPlace             => SpawnerBase.canPlace;
        public float MaxPlaceDistance     => SpawnerBase.maxInteractionDistance;
        public bool  CanPlaceAtMaxDistance => SpawnerBase.canPlaceMaxDistance;

        public override string ToString() => $"HandSpawnerWeapon({Name}, CanPlace={CanPlace})";
    }
}
