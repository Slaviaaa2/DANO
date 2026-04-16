using UnityEngine;

namespace DANO.API
{
    /// <summary>大口径レイキャスト銃のラッパー（広範囲ヒットスキャン）</summary>
    public class DanoLargeRaycastGun : DanoWeapon
    {
        public LargeRaycastGun LargeRaycastGunBase { get; }

        internal DanoLargeRaycastGun(LargeRaycastGun gun) : base(gun) { LargeRaycastGunBase = gun; }

        /// <summary>弾丸の半径</summary>
        public float BulletRadius => LargeRaycastGunBase.bulletRadius;

        /// <summary>ボックスキャストかどうか</summary>
        public bool IsBoxcast => LargeRaycastGunBase.boxcast;

        /// <summary>ボックスの寸法</summary>
        public Vector3 BoxDimensions => LargeRaycastGunBase.boxdimensions;

        /// <summary>プレイヤーノックバック力</summary>
        public float PlayerKnockback => LargeRaycastGunBase.playerKnockback;

        /// <summary>リロード時間（秒）</summary>
        public override float ReloadTime => LargeRaycastGunBase.reloadTime;

        public override string ToString() => $"DanoLargeRaycastGun({Name}, Radius={BulletRadius:F2}, Ammo={Ammo})";
    }
}
