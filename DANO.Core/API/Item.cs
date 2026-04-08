using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DANO.API
{
    /// <summary>
    /// STRAFTAT アイテム/武器のラッパー。
    /// ItemBehaviour 等の生ゲーム型を隠蔽し、直感的なプロパティを提供する。
    /// </summary>
    public class Item
    {
        // ─── Static ───

        /// <summary>ItemBehaviour から Item ラッパーを作成する</summary>
        public static Item Get(ItemBehaviour ib) => new Item(ib);

        /// <summary>シーン内の全アイテム</summary>
        public static IEnumerable<Item> List =>
            Object.FindObjectsOfType<ItemBehaviour>().Select(ib => new Item(ib));

        // ─── インスタンス ───

        /// <summary>生の ItemBehaviour（上級者向け）</summary>
        public ItemBehaviour Base { get; }

        private Item(ItemBehaviour ib) { Base = ib; }

        // ─── プロパティ ───

        /// <summary>武器名</summary>
        public string Name => Base.weaponName ?? "";

        /// <summary>Weapon コンポーネント（生型）</summary>
        public Weapon? WeaponComponent => Base.GetComponent<Weapon>();

        /// <summary>武器ラッパー（詳細プロパティ付き）</summary>
        public DanoWeapon? Weapon => DanoWeapon.FromItem(this);

        /// <summary>現在の弾数（読み書き可）</summary>
        public int Ammo
        {
            get => WeaponComponent?.currentAmmo ?? 0;
            set { if (WeaponComponent != null) WeaponComponent.currentAmmo = value; }
        }

        /// <summary>誰かに持たれているか</summary>
        public bool IsHeld => Base.rootObject != null;

        /// <summary>持っているプレイヤー</summary>
        public Player? Holder
        {
            get
            {
                if (Base.rootObject == null) return null;
                var pv = Base.rootObject.GetComponent<PlayerValues>();
                if (pv?.playerClient == null) return null;
                return Player.Get(pv.playerClient);
            }
        }

        public override string ToString() => $"Item({Name})";
    }
}
