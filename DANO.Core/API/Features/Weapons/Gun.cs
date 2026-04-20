namespace DANO.API
{
    /// <summary>標準的なヒットスキャン銃（Gun）のラッパー</summary>
    public class Gun : Weapon
    {
        public global::Gun GunBase { get; }

        internal Gun(global::Gun gun) : base(gun) { GunBase = gun; }

        /// <summary>リロード時間（秒）</summary>
        public override float ReloadTime => GunBase.reloadTime;

        public override string ToString() => $"Gun({Name}, Ammo={Ammo})";
    }
}
