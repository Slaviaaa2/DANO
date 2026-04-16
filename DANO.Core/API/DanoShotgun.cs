namespace DANO.API
{
    /// <summary>ショットガンのラッパー（複数ペレット射出）</summary>
    public class DanoShotgun : DanoWeapon
    {
        public Shotgun ShotgunBase { get; }

        internal DanoShotgun(Shotgun shotgun) : base(shotgun) { ShotgunBase = shotgun; }

        /// <summary>一発あたりのペレット数</summary>
        public int PelletCount => ShotgunBase.bulletAmount;

        /// <summary>ペレットの拡散度</summary>
        public float Spread => ShotgunBase.spread;

        /// <summary>リロード時間（秒）</summary>
        public override float ReloadTime => ShotgunBase.reloadTime;

        public override string ToString() => $"DanoShotgun({Name}, Pellets={PelletCount}, Ammo={Ammo})";
    }
}
