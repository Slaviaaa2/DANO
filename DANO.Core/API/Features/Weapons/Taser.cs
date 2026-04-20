namespace DANO.API
{
    /// <summary>テーザーのラッパー（チャージ → スタン攻撃）</summary>
    public class Taser : Weapon
    {
        public global::Taser TaserBase { get; }

        internal Taser(global::Taser taser) : base(taser) { TaserBase = taser; }

        /// <summary>チャージ時間（秒）</summary>
        public float ChargeTime => TaserBase.chargeTime;

        /// <summary>スタン持続時間（秒）</summary>
        public float StunTime => TaserBase.stunTime;

        public override string ToString() => $"Taser({Name}, Stun={StunTime:F1}s, Ammo={Ammo})";
    }
}
