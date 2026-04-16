namespace DANO.API
{
    /// <summary>標準的なヒットスキャン銃（Gun）のラッパー</summary>
    public class DanoGun : DanoWeapon
    {
        public Gun GunBase { get; }

        internal DanoGun(Gun gun) : base(gun) { GunBase = gun; }

        /// <summary>リロード時間（秒）</summary>
        public override float ReloadTime => GunBase.reloadTime;

        public override string ToString() => $"DanoGun({Name}, Ammo={Ammo})";
    }
}
