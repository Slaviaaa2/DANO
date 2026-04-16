namespace DANO.API
{
    /// <summary>バンプガンのラッパー（ノックバックプロジェクタイル）</summary>
    public class DanoBumpGun : DanoWeapon
    {
        public BumpGun BumpGunBase { get; }

        internal DanoBumpGun(BumpGun bumpGun) : base(bumpGun) { BumpGunBase = bumpGun; }

        /// <summary>発射力（プロジェクタイル速度）</summary>
        public float LaunchForce => BumpGunBase.launchForce;

        /// <summary>プレイヤーノックバック力</summary>
        public float PlayerKnockback => BumpGunBase.playerKnockback;

        public override string ToString() => $"DanoBumpGun({Name}, Launch={LaunchForce:F1}, Ammo={Ammo})";
    }
}
