namespace DANO.API
{
    /// <summary>フラッシュライトのラッパー</summary>
    public class DanoFlashLight : DanoWeapon
    {
        public FlashLight FlashLightBase { get; }

        internal DanoFlashLight(FlashLight flashLight) : base(flashLight) { FlashLightBase = flashLight; }

        /// <summary>ライトが点灯しているかどうか</summary>
        public bool IsOn => FlashLightBase.isOn;

        /// <summary>リロード時間（秒）</summary>
        public override float ReloadTime => FlashLightBase.reloadTime;

        public override string ToString() => $"DanoFlashLight({Name}, On={IsOn})";
    }
}
