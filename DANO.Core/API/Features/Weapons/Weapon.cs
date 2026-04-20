using UnityEngine;

namespace DANO.API
{
    /// <summary>
    /// STRAFTAT 武器のラッパー基底クラス。
    /// Get() は具象型を返すため、キャストして固有プロパティにアクセスできる。
    /// <code>
    /// var weapon = Weapon.Get(w);
    /// if (weapon is ShotgunWeapon sg)
    ///     int pellets = sg.PelletCount;
    /// // または
    /// if (weapon.Type == WeaponType.Shotgun) { }
    /// </code>
    /// </summary>
    public class Weapon
    {
        // ─── Static Factory ───

        public static Weapon? Get(global::Weapon weapon)
        {
            if (weapon == null) return null;

            return weapon switch
            {
                global::Gun g                  => new GunWeapon(g),
                global::Shotgun sg             => new ShotgunWeapon(sg),
                global::Minigun mg             => new MinigunWeapon(mg),
                global::ChargeGun cg           => new ChargeGunWeapon(cg),
                global::BeamGun bg             => new BeamGunWeapon(bg),
                global::LargeRaycastGun lr     => new LargeRaycastGunWeapon(lr),
                global::DualLauncher dl        => new DualLauncherWeapon(dl),
                global::BumpGun bump           => new BumpGunWeapon(bump),
                global::RepulsiveGun rg        => new RepulsiveGunWeapon(rg),
                global::Taser t                => new TaserWeapon(t),
                global::MeleeWeapon mw         => new MeleeWeapon(mw),
                global::FlashLight fl          => new FlashLightWeapon(fl),
                global::Propeller pr           => new PropellerWeapon(pr),
                global::WeaponHandSpawner whs  => new HandSpawnerWeapon(whs),
                _                              => new Weapon(weapon),
            };
        }

        public static Weapon? FromItem(Item item)
        {
            var weapon = item.WeaponComponent;
            return Get(weapon);
        }

        // ─── インスタンス ───

        public global::Weapon Base { get; }

        protected Weapon(global::Weapon weapon) { Base = weapon; }

        // ─── 型 ───

        /// <summary>武器の種別</summary>
        public WeaponType Type => Base switch
        {
            global::Gun _                 => WeaponType.Gun,
            global::Shotgun _             => WeaponType.Shotgun,
            global::Minigun _             => WeaponType.Minigun,
            global::ChargeGun _           => WeaponType.ChargeGun,
            global::BeamGun _             => WeaponType.BeamGun,
            global::LargeRaycastGun _     => WeaponType.LargeRaycastGun,
            global::DualLauncher _        => WeaponType.DualLauncher,
            global::BumpGun _             => WeaponType.BumpGun,
            global::RepulsiveGun _        => WeaponType.RepulsiveGun,
            global::Taser _               => WeaponType.Taser,
            global::MeleeWeapon _         => WeaponType.Melee,
            global::FlashLight _          => WeaponType.FlashLight,
            global::Propeller _           => WeaponType.Propeller,
            global::WeaponHandSpawner _   => WeaponType.HandSpawner,
            _                             => WeaponType.Unknown,
        };

        /// <summary>銃器かどうか（弾薬を使う全武器タイプ）</summary>
        public bool IsFirearm => Base.needsAmmo && !(Base is global::MeleeWeapon);

        /// <summary>近接武器かどうか</summary>
        public bool IsMelee => Base is global::MeleeWeapon;

        /// <summary>IsFirearm と同義（後方互換）</summary>
        public bool IsGun => IsFirearm;

        // ─── 基本情報 ───

        public string Name => Base.behaviour?.weaponName ?? "";

        // ─── 弾薬 ───

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

        public int ReserveAmmo
        {
            get => Base.currentAmmo;
            set => Base.currentAmmo = value;
        }

        public bool NeedsAmmo => Base.needsAmmo;
        public bool IsReloading => Base.isReloading;

        public float ChargedBullets
        {
            get => Base.chargedBullets;
            set => Base.chargedBullets = value;
        }

        public bool CanReload => Base.reloadWeapon;

        // ─── ダメージ ───

        public float Damage => Base.damage;
        public float HeadMultiplier => Base.headMultiplier;
        public float RagdollEjectForce => Base.ragdollEjectForce;

        // ─── 発射 ───

        public float FireRate => Base.timeBetweenFire;
        public bool IsBurst => Base.burstGun;
        public bool IsSemiAuto => Base.onePressShoot;

        // ─── 精度 ───

        public float MinSpread => Base.minSpread;
        public float MaxSpread => Base.maxSpread;
        public float StandingAccuracy => Base.standingAccuracy;
        public float WalkAccuracy => Base.walkAccuracy;
        public float SprintAccuracy => Base.sprintAccuracy;
        public float HipFireAccuracy => Base.notAimingAccuracy;
        public bool HasScope => Base.ScopeAimWeapon;

        // ─── 移動への影響 ───

        public float MovementFactor => Base.movementFactor;
        public float JumpFactor => Base.jumpFactor;
        public float WallJumpFactor => Base.wallJumpFactor;
        public int MaxWallJumps => Base.maxWallJumps;

        // ─── 発射時スロー ───

        public bool FireSlowDown => Base.fireSlowDown;
        public float FireSlowDownFactor => Base.fireSlowDownFactor;

        // ─── 手の状態 ───

        public bool InRightHand => Base.inRightHand;
        public bool InLeftHand => Base.inLeftHand;

        // ─── 後方互換（基底でのアクセス） ───

        public virtual float ReloadTime => 0f;

        public float MeleeBaseDamage  => Base is global::MeleeWeapon melee ? melee.baseAttackDamage  : 0f;
        public float MeleeSecondDamage => Base is global::MeleeWeapon melee ? melee.secondAttackDamage : 0f;
        public float MeleeKnockback   => Base is global::MeleeWeapon melee ? melee.playerKnockback    : 0f;

        public override string ToString() => $"Weapon({Name}, {Type}, Ammo={Ammo})";
    }
}
