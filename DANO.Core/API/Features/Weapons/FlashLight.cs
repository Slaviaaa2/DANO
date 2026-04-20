namespace DANO.API
{
    /// <summary>フラッシュライトのラッパー</summary>
    public class FlashLight : Weapon
    {
        public global::FlashLight FlashLightBase { get; }

        internal FlashLight(global::FlashLight flashLight) : base(flashLight) { FlashLightBase = flashLight; }

        /// <summary>ライトが点灯しているかどうか</summary>
        public bool IsOn => FlashLightBase.isOn;

        /// <summary>リロード時間（秒）</summary>
        public override float ReloadTime => FlashLightBase.reloadTime;

        public override string ToString() => $"FlashLight({Name}, On={IsOn})";
    }
}
