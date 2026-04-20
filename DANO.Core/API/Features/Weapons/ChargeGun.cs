namespace DANO.API
{
    /// <summary>チャージ式銃のラッパー（溜めて発射）</summary>
    public class ChargeGun : Weapon
    {
        public global::ChargeGun ChargeGunBase { get; }

        internal ChargeGun(global::ChargeGun chargeGun) : base(chargeGun) { ChargeGunBase = chargeGun; }

        /// <summary>最大チャージ時間（秒）</summary>
        public float MaxChargeTime => ChargeGunBase.maxChargeTime;

        /// <summary>現在のチャージ量</summary>
        public float AccumulatedPower => ChargeGunBase.accumulatedPower;

        /// <summary>中間段階があるかどうか</summary>
        public bool HasIntermediateStates => ChargeGunBase.hasIntermediateStates;

        /// <summary>爆発半径</summary>
        public float Radius => ChargeGunBase.radius;

        /// <summary>プレイヤーノックバック力</summary>
        public float PlayerKnockback => ChargeGunBase.playerKnockback;

        public override string ToString() => $"ChargeGun({Name}, MaxCharge={MaxChargeTime:F2}s, Ammo={Ammo})";
    }
}
