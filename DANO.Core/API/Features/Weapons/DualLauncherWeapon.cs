namespace DANO.API
{
    public class DualLauncherWeapon : Weapon
    {
        public global::DualLauncher DualLauncherBase { get; }

        internal DualLauncherWeapon(global::DualLauncher launcher) : base(launcher) { DualLauncherBase = launcher; }

        public float LaunchForce          => DualLauncherBase.launchForce;
        public float PlayerKnockback      => DualLauncherBase.playerKnockback;
        public bool  IsRebond             => DualLauncherBase.rebond;
        public bool  IsGrenade            => DualLauncherBase.grenade;
        public bool  IsObus               => DualLauncherBase.obus;
        public bool  IsShrapnel           => DualLauncherBase.shrapnel;
        public bool  IsBubble             => DualLauncherBase.bubble;
        public bool  IsKanye              => DualLauncherBase.kanye;
        public float TimeBeforeExplosion  => DualLauncherBase.timeBeforeExplosion;

        public override string ToString() => $"DualLauncherWeapon({Name}, Launch={LaunchForce:F1}, Ammo={Ammo})";
    }
}
