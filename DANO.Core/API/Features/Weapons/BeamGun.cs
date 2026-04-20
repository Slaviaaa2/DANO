namespace DANO.API
{
    /// <summary>ビーム式銃のラッパー（チャージ + プロジェクタイル発射）</summary>
    public class BeamGun : Weapon
    {
        public global::BeamGun BeamGunBase { get; }

        internal BeamGun(global::BeamGun beamGun) : base(beamGun) { BeamGunBase = beamGun; }

        /// <summary>発射力（プロジェクタイル速度）</summary>
        public float LaunchForce => BeamGunBase.launchForce;

        /// <summary>プレイヤーノックバック力</summary>
        public float PlayerKnockback => BeamGunBase.playerKnockback;

        /// <summary>最大チャージ時間（秒）</summary>
        public float MaxChargeTime => BeamGunBase.maxChargeTime;

        /// <summary>現在のチャージ量</summary>
        public float AccumulatedPower => BeamGunBase.accumulatedPower;

        /// <summary>中間段階があるかどうか</summary>
        public bool HasIntermediateStates => BeamGunBase.hasIntermediateStates;

        /// <summary>ビーム半径</summary>
        public float Radius => BeamGunBase.radius;

        public override string ToString() => $"BeamGun({Name}, Launch={LaunchForce:F1}, Ammo={Ammo})";
    }
}
