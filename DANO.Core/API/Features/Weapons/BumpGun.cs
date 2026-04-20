namespace DANO.API
{
    /// <summary>バンプガンのラッパー（ノックバックプロジェクタイル）</summary>
    public class BumpGun : Weapon
    {
        public global::BumpGun BumpGunBase { get; }

        internal BumpGun(global::BumpGun bumpGun) : base(bumpGun) { BumpGunBase = bumpGun; }

        /// <summary>発射力（プロジェクタイル速度）</summary>
        public float LaunchForce => BumpGunBase.launchForce;

        /// <summary>プレイヤーノックバック力</summary>
        public float PlayerKnockback => BumpGunBase.playerKnockback;

        public override string ToString() => $"BumpGun({Name}, Launch={LaunchForce:F1}, Ammo={Ammo})";
    }
}
