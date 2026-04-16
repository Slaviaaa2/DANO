namespace DANO.API
{
    /// <summary>デュアルランチャーのラッパー（複数タイプのプロジェクタイル発射）</summary>
    public class DanoDualLauncher : DanoWeapon
    {
        public DualLauncher DualLauncherBase { get; }

        internal DanoDualLauncher(DualLauncher launcher) : base(launcher) { DualLauncherBase = launcher; }

        /// <summary>発射力（プロジェクタイル速度）</summary>
        public float LaunchForce => DualLauncherBase.launchForce;

        /// <summary>プレイヤーノックバック力</summary>
        public float PlayerKnockback => DualLauncherBase.playerKnockback;

        /// <summary>バウンド弾かどうか</summary>
        public bool IsRebond => DualLauncherBase.rebond;

        /// <summary>グレネードモードかどうか</summary>
        public bool IsGrenade => DualLauncherBase.grenade;

        /// <summary>榴弾モードかどうか</summary>
        public bool IsObus => DualLauncherBase.obus;

        /// <summary>シュラプネルモードかどうか</summary>
        public bool IsShrapnel => DualLauncherBase.shrapnel;

        /// <summary>バブルモードかどうか</summary>
        public bool IsBubble => DualLauncherBase.bubble;

        /// <summary>カニエモードかどうか</summary>
        public bool IsKanye => DualLauncherBase.kanye;

        /// <summary>グレネードの起爆までの時間</summary>
        public float TimeBeforeExplosion => DualLauncherBase.timeBeforeExplosion;

        public override string ToString() => $"DanoDualLauncher({Name}, Launch={LaunchForce:F1}, Ammo={Ammo})";
    }
}
