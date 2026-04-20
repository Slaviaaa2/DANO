using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DANO.API
{
    /// <summary>
    /// グレネードのラッパー。PhysicsGrenade を隠蔽し、直感的なプロパティを提供する。
    /// </summary>
    public class Grenade
    {
        // ─── Static ───

        /// <summary>PhysicsGrenade からラッパーを作成する</summary>
        public static Grenade? Get(PhysicsGrenade grenade) =>
            grenade != null ? new Grenade(grenade) : null;

        /// <summary>シーン内の全グレネードを取得</summary>
        public static IEnumerable<Grenade> List =>
            Object.FindObjectsOfType<PhysicsGrenade>().Select(g => new Grenade(g));

        /// <summary>アクティブな（まだ爆発していない）グレネードを取得</summary>
        public static IEnumerable<Grenade> Active =>
            Object.FindObjectsOfType<PhysicsGrenade>()
                .Where(g => g.enabled)
                .Select(g => new Grenade(g));

        // ─── インスタンス ───

        /// <summary>生の PhysicsGrenade コンポーネント（上級者向け）</summary>
        public PhysicsGrenade Base { get; }

        private Grenade(PhysicsGrenade grenade) { Base = grenade; }

        // ─── プロパティ ───

        /// <summary>武器名</summary>
        public string Name => Base.weaponName ?? "";

        /// <summary>ワールド座標</summary>
        public Vector3 Position => Base.transform.position;

        /// <summary>爆発半径</summary>
        public float ExplosionRadius => Base.explosionRadius;

        /// <summary>爆発までの時間（設定値）</summary>
        public float TimeBeforeExplosion => Base.timeBeforeExplosion;

        /// <summary>現在の爆発タイマー</summary>
        public float ExplosionTimer => Base.explosionTimer;

        /// <summary>フラググレネードかどうか</summary>
        public bool IsFragGrenade => Base.fragGrenade;

        /// <summary>スタングレネードかどうか</summary>
        public bool IsStunGrenade => Base.stunGrenade;

        /// <summary>スタン持続時間（スタングレネードの場合）</summary>
        public float StunTime => Base.stunTime;

        /// <summary>ラグドール吹き飛ばし力</summary>
        public float RagdollEjectForce => Base.ragdollEjectForce;

        /// <summary>接地しているかどうか</summary>
        public bool IsGrounded => Base.isGrounded;

        /// <summary>現在の速度</summary>
        public Vector3 Velocity => Base.velocity;

        /// <summary>最大ヒット数</summary>
        public int MaxHits => Base.maxHits;

        /// <summary>現在のヒット数</summary>
        public int Hits => Base.hits;

        /// <summary>アクティブかどうか（爆発前）</summary>
        public bool IsActive => Base.enabled;

        public override string ToString() => $"Grenade({Name}, Frag={IsFragGrenade}, Stun={IsStunGrenade})";
    }
}
