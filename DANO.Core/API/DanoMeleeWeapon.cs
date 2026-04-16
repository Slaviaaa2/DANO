namespace DANO.API
{
    /// <summary>近接武器のラッパー</summary>
    public class DanoMeleeWeapon : DanoWeapon
    {
        public MeleeWeapon MeleeBase { get; }

        internal DanoMeleeWeapon(MeleeWeapon melee) : base(melee) { MeleeBase = melee; }

        /// <summary>基本攻撃のダメージ</summary>
        public float BaseAttackDamage => MeleeBase.baseAttackDamage;

        /// <summary>第二攻撃のダメージ</summary>
        public float SecondAttackDamage => MeleeBase.secondAttackDamage;

        /// <summary>基本攻撃の間隔（秒）</summary>
        public float BaseAttackInterval => MeleeBase.timeBetweenBaseAttack;

        /// <summary>第二攻撃の間隔（秒）</summary>
        public float SecondAttackInterval => MeleeBase.timeBetweenSecondAttack;

        /// <summary>基本攻撃の持続時間（秒）</summary>
        public float BaseAttackDuration => MeleeBase.baseAttackDuration;

        /// <summary>第二攻撃の持続時間（秒）</summary>
        public float SecondAttackDuration => MeleeBase.secondAttackDuration;

        /// <summary>プレイヤーノックバック力</summary>
        public float PlayerKnockback => MeleeBase.playerKnockback;

        /// <summary>反発力</summary>
        public float RepulseForce => MeleeBase.repulseForce;

        /// <summary>攻撃時に前進するかどうか</summary>
        public bool HasPropulsion => MeleeBase.propulsion;

        /// <summary>基本攻撃の前進量</summary>
        public float BasePropulsionAmount => MeleeBase.basePropulsionAmount;

        /// <summary>第二攻撃の前進量</summary>
        public float SecondPropulsionAmount => MeleeBase.secondPropulsionAmount;

        /// <summary>ホルダーをバウンスさせるかどうか</summary>
        public bool BounceHolder => MeleeBase.bounceHolder;

        /// <summary>一振りあたりのヒット数</summary>
        public int HitsAmount => MeleeBase.hitsAmount;

        public override string ToString() => $"DanoMeleeWeapon({Name}, BaseDmg={BaseAttackDamage:F1})";
    }
}
