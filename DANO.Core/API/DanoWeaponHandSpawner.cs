namespace DANO.API
{
    /// <summary>設置型武器のラッパー（地雷、クレイモア等）</summary>
    public class DanoWeaponHandSpawner : DanoWeapon
    {
        public WeaponHandSpawner SpawnerBase { get; }

        internal DanoWeaponHandSpawner(WeaponHandSpawner spawner) : base(spawner) { SpawnerBase = spawner; }

        /// <summary>近接地雷かどうか</summary>
        public bool IsProximityMine => SpawnerBase.proximityMine;

        /// <summary>クレイモアかどうか</summary>
        public bool IsClaymore => SpawnerBase.claymore;

        /// <summary>対人地雷かどうか</summary>
        public bool IsAPMine => SpawnerBase.apmine;

        /// <summary>設置可能かどうか</summary>
        public bool CanPlace => SpawnerBase.canPlace;

        /// <summary>最大設置距離</summary>
        public float MaxPlaceDistance => SpawnerBase.maxInteractionDistance;

        /// <summary>最大距離に設置可能かどうか</summary>
        public bool CanPlaceAtMaxDistance => SpawnerBase.canPlaceMaxDistance;

        public override string ToString() => $"DanoWeaponHandSpawner({Name}, CanPlace={CanPlace})";
    }
}
