namespace DANO.API
{
    /// <summary>ミニガンのラッパー（スピンアップ → 連射）</summary>
    public class DanoMinigun : DanoWeapon
    {
        public Minigun MinigunBase { get; }

        internal DanoMinigun(Minigun minigun) : base(minigun) { MinigunBase = minigun; }

        /// <summary>射撃開始までのスピンアップ時間（秒）</summary>
        public float SpinUpTime => MinigunBase.timeBeforeShooting;

        /// <summary>バレル回転速度</summary>
        public float RotationSpeed => MinigunBase.rotationSpeed;

        /// <summary>リロード時間（秒）</summary>
        public override float ReloadTime => MinigunBase.reloadTime;

        public override string ToString() => $"DanoMinigun({Name}, SpinUp={SpinUpTime:F2}s, Ammo={Ammo})";
    }
}
