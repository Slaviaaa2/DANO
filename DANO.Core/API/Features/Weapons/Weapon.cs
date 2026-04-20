using UnityEngine;

namespace DANO.API
{
    /// <summary>
    /// STRAFTAT 武器のラッパー基底クラス。
    /// Get() は具象型（Gun, Shotgun 等）を返すため、キャストして固有プロパティにアクセスできる。
    /// <code>
    /// var weapon = Weapon.Get(w);
    /// if (weapon is Shotgun sg)
    ///     int pellets = sg.PelletCount;
    /// </code>
    /// </summary>
    public class Weapon
    {
        // ─── Static Factory ───

        /// <summary>
        /// Weapon コンポーネントから適切な具象型のラッパーを作成する。
        /// 返り値は Gun, Shotgun 等にキャスト可能。
        /// </summary>
        public static Weapon? Get(global::Weapon weapon)
        {
            if (weapon == null) return null;

            return weapon switch
            {
                global::Gun g              => new Gun(g),
                global::Shotgun sg         => new Shotgun(sg),
                global::Minigun mg         => new Minigun(mg),
                global::ChargeGun cg       => new ChargeGun(cg),
                global::BeamGun bg         => new BeamGun(bg),
                global::LargeRaycastGun lr => new LargeRaycastGun(lr),
                global::DualLauncher dl    => new DualLauncher(dl),
                global::BumpGun bump       => new BumpGun(bump),
                global::RepulsiveGun rg    => new RepulsiveGun(rg),
                global::Taser t            => new Taser(t),
                global::MeleeWeapon mw     => new MeleeWeapon(mw),
                global::FlashLight fl      => new FlashLight(fl),
                global::Propeller pr       => new Propeller(pr),
                global::WeaponHandSpawner whs => new WeaponHandSpawner(whs),
                _                          => new Weapon(weapon),
            };
        }

        /// <summary>Item からラップされた武器情報を取得する</summary>
        public static Weapon? FromItem(Item item)
        {
            var weapon = item.WeaponComponent;
            return Get(weapon);
        }

        // ─── インスタンス ───

        /// <summary>生の Weapon コンポーネント（上級者向け）</summary>
        public global::Weapon Base { get; }

        protected Weapon(global::Weapon weapon) { Base = weapon; }

        // ─── 基本プロパティ ───

        /// <summary>武器名（ItemBehaviour.weaponName 経由）</summary>
        public string Name => Base.behaviour?.weaponName ?? "";

        /// <summary>銃器かどうか（弾薬を使う全武器タイプ: Gun, Shotgun, Minigun, ChargeGun 等）</summary>
        public bool IsFirearm => Base.needsAmmo && !(Base is global::MeleeWeapon);

        /// <summary>近接武器かどうか (MeleeWeapon 派生)</summary>
        public bool IsMelee => Base is global::MeleeWeapon;

        /// <summary>武器の具体的な型名（Gun, Shotgun, Minigun, ChargeGun, BeamGun, MeleeWeapon 等）</summary>
        public string WeaponType => Base.GetType().Name;

        // 後方互換
        /// <summary>銃器かどうか（IsFirearm と同義）</summary>
        public bool IsGun => IsFirearm;

        // ─── 弾薬 ───

        /// <summary>現在の弾数（読み書き可）。非 reload 武器は currentAmmo、reload 武器は chargedBullets。</summary>
        public int Ammo
        {
            get => Base.reloadWeapon ? (int)Base.chargedBullets : Base.currentAmmo;
            set
            {
                if (Base.reloadWeapon)
                    Base.chargedBullets = value;
                else
                    Base.currentAmmo = value;
            }
        }

        /// <summary>予備弾数（reload 武器のみ意味がある）</summary>
        public int ReserveAmmo
        {
            get => Base.currentAmmo;
            set => Base.currentAmmo = value;
        }

        /// <summary>弾薬が必要な武器かどうか</summary>
        public bool NeedsAmmo => Base.needsAmmo;

        /// <summary>リロード中かどうか</summary>
        public bool IsReloading => Base.isReloading;

        /// <summary>装填済み弾数（reload 武器のマガジン内弾数）</summary>
        public float ChargedBullets
        {
            get => Base.chargedBullets;
            set => Base.chargedBullets = value;
        }

        /// <summary>リロード可能な武器かどうか</summary>
        public bool CanReload => Base.reloadWeapon;

        // ─── ダメージ ───

        /// <summary>基礎ダメージ</summary>
        public float Damage => Base.damage;

        /// <summary>ヘッドショット倍率</summary>
        public float HeadMultiplier => Base.headMultiplier;

        /// <summary>キル時のラグドール吹き飛ばし力</summary>
        public float RagdollEjectForce => Base.ragdollEjectForce;

        // ─── 発射 ───

        /// <summary>発射間隔（秒）</summary>
        public float FireRate => Base.timeBetweenFire;

        /// <summary>バースト射撃かどうか</summary>
        public bool IsBurst => Base.burstGun;

        /// <summary>セミオート（ワンプレス）かどうか</summary>
        public bool IsSemiAuto => Base.onePressShoot;

        // ─── 精度 ───

        /// <summary>最小拡散</summary>
        public float MinSpread => Base.minSpread;

        /// <summary>最大拡散</summary>
        public float MaxSpread => Base.maxSpread;

        /// <summary>静止時精度</summary>
        public float StandingAccuracy => Base.standingAccuracy;

        /// <summary>歩行時精度</summary>
        public float WalkAccuracy => Base.walkAccuracy;

        /// <summary>走行時精度</summary>
        public float SprintAccuracy => Base.sprintAccuracy;

        /// <summary>非エイム時精度</summary>
        public float HipFireAccuracy => Base.notAimingAccuracy;

        /// <summary>スコープ付き武器かどうか</summary>
        public bool HasScope => Base.ScopeAimWeapon;

        // ─── 移動への影響 ───

        /// <summary>移動速度倍率</summary>
        public float MovementFactor => Base.movementFactor;

        /// <summary>ジャンプ力倍率</summary>
        public float JumpFactor => Base.jumpFactor;

        /// <summary>壁ジャンプ力倍率</summary>
        public float WallJumpFactor => Base.wallJumpFactor;

        /// <summary>最大壁ジャンプ回数</summary>
        public int MaxWallJumps => Base.maxWallJumps;

        // ─── 発射時スロー ───

        /// <summary>発射時に減速するかどうか</summary>
        public bool FireSlowDown => Base.fireSlowDown;

        /// <summary>発射時減速率</summary>
        public float FireSlowDownFactor => Base.fireSlowDownFactor;

        // ─── 手の状態 ───

        /// <summary>右手に持っているか</summary>
        public bool InRightHand => Base.inRightHand;

        /// <summary>左手に持っているか</summary>
        public bool InLeftHand => Base.inLeftHand;

        // ─── Gun 固有（後方互換） ───

        /// <summary>リロード時間（Gun 系のみ、それ以外は 0）</summary>
        public virtual float ReloadTime => 0f;

        // ─── MeleeWeapon 固有（後方互換） ───

        /// <summary>近接基本攻撃ダメージ（MeleeWeapon のみ）</summary>
        public float MeleeBaseDamage => Base is global::MeleeWeapon melee ? melee.baseAttackDamage : 0f;

        /// <summary>近接第二攻撃ダメージ（MeleeWeapon のみ）</summary>
        public float MeleeSecondDamage => Base is global::MeleeWeapon melee ? melee.secondAttackDamage : 0f;

        /// <summary>近接ノックバック力（MeleeWeapon のみ）</summary>
        public float MeleeKnockback => Base is global::MeleeWeapon melee ? melee.playerKnockback : 0f;

        public override string ToString() => $"Weapon({Name}, {WeaponType}, Ammo={Ammo})";
    }
}
