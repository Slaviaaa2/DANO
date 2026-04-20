using System.Collections.Generic;
using System.Linq;
using FishNet;
using FishNet.Object;
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
        public global::Weapon? WeaponComponent => Base.GetComponent<global::Weapon>();

        /// <summary>武器ラッパー（詳細プロパティ付き）</summary>
        public Weapon? Weapon => Weapon.FromItem(this);

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

        /// <summary>アイテムの種別</summary>
        public ItemType Type
        {
            get
            {
                if (WeaponComponent != null) return ItemType.Weapon;
                if (Base.GetComponent<PhysicsGrenade>() != null) return ItemType.Grenade;
                return ItemType.Unknown;
            }
        }

        /// <summary>アイテムをネットワーク上から削除する</summary>
        public void Destroy()
        {
            var netObj = Base.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsSpawned)
                netObj.Despawn();
            else
                Object.Destroy(Base.gameObject);
        }

        /// <summary>持ち主の手から強制的にドロップさせる</summary>
        public void Drop()
        {
            var holder = Holder;
            if (holder == null) return;
            var pickup = holder.Controller?.GetComponent<PlayerPickup>();
            if (pickup == null) return;
            if (pickup.objInHand == Base.gameObject)
                pickup.DropObjectServer(Base.gameObject, true);
            else if (pickup.objInLeftHand == Base.gameObject)
                pickup.DropObjectServer(Base.gameObject, false);
        }

        /// <summary>指定座標にアイテムをスポーンする（プレハブ指定）</summary>
        public static Item? Spawn(GameObject prefab, Vector3 position)
        {
            if (prefab == null) return null;
            var go = Object.Instantiate(prefab, position, Quaternion.identity);
            InstanceFinder.ServerManager?.Spawn(go);
            var ib = go.GetComponent<ItemBehaviour>();
            return ib != null ? Get(ib) : null;
        }

        /// <summary>指定座標にアイテムをスポーンする（武器名指定）</summary>
        public static Item? Spawn(string weaponName, Vector3 position)
        {
            SpawnerManager.PopulateAllWeapons();
            var prefab = SpawnerManager.AllWeapons?.FirstOrDefault(
                g => string.Equals(g.GetComponent<ItemBehaviour>()?.weaponName, weaponName,
                    System.StringComparison.OrdinalIgnoreCase));
            return prefab != null ? Spawn(prefab, position) : null;
        }

        /// <summary>このアイテムを指定プレイヤーの足元に移動し渡す</summary>
        public void GiveTo(Player player)
        {
            Drop();
            Base.transform.position = player.Position + Vector3.up;
        }

        public override string ToString() => $"Item({Name})";
    }
}
