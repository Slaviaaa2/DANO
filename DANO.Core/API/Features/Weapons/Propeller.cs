namespace DANO.API
{
    /// <summary>プロペラのラッパー（飛行補助武器）</summary>
    public class Propeller : Weapon
    {
        public global::Propeller PropellerBase { get; }

        internal Propeller(global::Propeller propeller) : base(propeller) { PropellerBase = propeller; }

        /// <summary>飛行速度</summary>
        public float FlySpeed => PropellerBase.flySpeed;

        /// <summary>減速速度</summary>
        public float DecelSpeed => PropellerBase.decelSpeed;

        /// <summary>最大パワー</summary>
        public float MaxPower => PropellerBase.maxPower;

        /// <summary>現在のパワー</summary>
        public float Power => PropellerBase.power;

        /// <summary>飛行中かどうか</summary>
        public bool IsFlying => PropellerBase.isflying;

        /// <summary>回転速度</summary>
        public float RotationSpeed => PropellerBase.rotationSpeed;

        public override string ToString() => $"Propeller({Name}, Flying={IsFlying})";
    }
}
