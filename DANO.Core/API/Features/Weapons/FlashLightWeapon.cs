namespace DANO.API
{
    public class FlashLightWeapon : Weapon
    {
        public global::FlashLight FlashLightBase { get; }

        internal FlashLightWeapon(global::FlashLight flashLight) : base(flashLight) { FlashLightBase = flashLight; }

        public bool IsOn => FlashLightBase.isOn;

        public override float ReloadTime => FlashLightBase.reloadTime;

        public override string ToString() => $"FlashLightWeapon({Name}, On={IsOn})";
    }
}
