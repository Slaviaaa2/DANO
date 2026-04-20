namespace DANO.API
{
    /// <summary>ショットガンのラッパー（複数ペレット射出）</summary>
    public class Shotgun : Weapon
    {
        public global::Shotgun ShotgunBase { get; }

        internal Shotgun(global::Shotgun shotgun) : base(shotgun) { ShotgunBase = shotgun; }

        /// <summary>一発あたりのペレット数</summary>
        public int PelletCount => ShotgunBase.bulletAmount;

        /// <summary>ペレットの拡散度</summary>
        public float Spread => ShotgunBase.spread;

        /// <summary>リロード時間（秒）</summary>
        public override float ReloadTime => ShotgunBase.reloadTime;

        public override string ToString() => $"Shotgun({Name}, Pellets={PelletCount}, Ammo={Ammo})";
    }
}
