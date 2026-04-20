using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DANO.Events;
using UnityEngine;

namespace DANOUE.API
{
    /// <summary>
    /// DANOUEカスタムアイテムの基底クラス。
    /// このクラスの1インスタンス = アイテム「型」1種類を表すシングルトン。
    ///
    /// 使い方:
    /// <code>
    /// public class MagicSword : CustomItem
    /// {
    ///     public override uint   Id             => 1;
    ///     public override string Name           => "マジックソード";
    ///     public override string BaseWeaponName => "Knife";
    ///     public override string Description    => "魔力を宿した剣。";
    ///
    ///     protected override void OnAcquired(DANO.API.Player player, DANO.API.Item item)
    ///         => player.HealFull();
    /// }
    ///
    /// // プラグインの OnEnabled() で:
    /// CustomItem.Register&lt;MagicSword&gt;();
    /// </code>
    /// </summary>
    public abstract class CustomItem
    {
        // ─── 型情報（サブクラスで定義） ───

        /// <summary>カスタムアイテムの一意なID（プラグイン内で重複不可）</summary>
        public abstract uint Id { get; }

        /// <summary>カスタムアイテムの表示名</summary>
        public abstract string Name { get; }

        /// <summary>
        /// ベースとなるSTRAFTAT武器名（例: "AK47", "Knife"）。
        /// Give() / Spawn() はこの武器プレハブを使ってアイテムを生成する。
        /// </summary>
        public abstract string BaseWeaponName { get; }

        /// <summary>説明文</summary>
        public virtual string Description { get; } = "";

        /// <summary>
        /// アイテムを拾い上げた時にローカルプレイヤーへ表示するヒント。
        /// null に設定すると非表示。
        /// </summary>
        public virtual string? PickupHint => $"[カスタムアイテム] {Name} を拾い上げた";

        /// <summary>
        /// アイテムを手に持った時にローカルプレイヤーへ表示するヒント。
        /// null に設定すると非表示。
        /// </summary>
        public virtual string? SelectHint => null;

        // ─── インスタンス追跡（InstanceID ベース、ItemBehaviour 型非露出） ───

        private readonly HashSet<int> _trackedIds = new HashSet<int>();

        private static readonly Dictionary<uint, CustomItem> _byId =
            new Dictionary<uint, CustomItem>();

        private static readonly Dictionary<string, CustomItem> _byName =
            new Dictionary<string, CustomItem>(StringComparer.OrdinalIgnoreCase);

        /// <summary>ItemBehaviour の InstanceID → CustomItem のグローバルルックアップ</summary>
        private static readonly Dictionary<int, CustomItem> _globalTracker =
            new Dictionary<int, CustomItem>();

        private static bool _subscribed = false;

        // ─── 登録 ───

        /// <summary>
        /// カスタムアイテム型をDANOUEに登録する。
        /// OnEnabled() 内で呼ぶこと。
        /// </summary>
        public static void Register<T>() where T : CustomItem, new()
            => RegisterInstance(new T());

        /// <summary>
        /// アセンブリ内の全CustomItemサブクラスを自動登録する。
        /// 抽象クラスはスキップされる。
        /// </summary>
        public static void RegisterAll(Assembly assembly)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (type.IsAbstract || !typeof(CustomItem).IsAssignableFrom(type))
                    continue;
                RegisterInstance((CustomItem)Activator.CreateInstance(type));
            }
        }

        /// <summary>
        /// カスタムアイテム型の登録を解除し、全追跡インスタンスを解放する。
        /// OnDisabled() 内で呼ぶこと。
        /// </summary>
        public static void Unregister<T>() where T : CustomItem
        {
            var target = _byId.Values.OfType<T>().FirstOrDefault();
            if (target == null) return;

            foreach (var id in target._trackedIds)
                _globalTracker.Remove(id);
            target._trackedIds.Clear();

            _byId.Remove(target.Id);
            _byName.Remove(target.Name);
        }

        private static void RegisterInstance(CustomItem instance)
        {
            if (_byId.ContainsKey(instance.Id))
                throw new InvalidOperationException(
                    $"[DANOUE] CustomItem ID {instance.Id} は既に登録されています: {_byId[instance.Id].Name}");

            _byId[instance.Id] = instance;
            _byName[instance.Name] = instance;
            EnsureSubscribed();
        }

        /// <summary>初回 Register 時にEventBusへ遅延サブスクライブする</summary>
        private static void EnsureSubscribed()
        {
            if (_subscribed) return;
            _subscribed = true;

            EventBus.Subscribe<ItemPickingUpEvent>(OnItemPickingUp, priority: -100);
            EventBus.Subscribe<ItemPickedUpEvent>(OnItemPickedUp, priority: -100);
            EventBus.Subscribe<ItemDroppingEvent>(OnItemDropping, priority: -100);
            EventBus.Subscribe<ItemDroppedEvent>(OnItemDropped, priority: -100);
            // 武器イベントはFishNet制約によりローカルプレイヤーのみ発火
            EventBus.Subscribe<WeaponFiringEvent>(OnWeaponFiring, priority: -100);
            EventBus.Subscribe<WeaponFiredEvent>(OnWeaponFired, priority: -100);
            EventBus.Subscribe<WeaponReloadingEvent>(OnWeaponReloading, priority: -100);
            EventBus.Subscribe<WeaponReloadedEvent>(OnWeaponReloaded, priority: -100);
            EventBus.Subscribe<PlayerDamagingEvent>(OnPlayerDamaging, priority: -100);
            EventBus.Subscribe<PlayerDiedEvent>(OnPlayerDied, priority: -100);
            EventBus.Subscribe<RoundStartedEvent>(DispatchRoundStarted, priority: -100);
        }

        // ─── イベントルーティング（静的） ───

        private static void OnItemPickingUp(ItemPickingUpEvent ev)
        {
            if (!TryGetByItem(ev.Item, out var ci)) return;
            ci!.OnPickingUp(ev);
        }

        private static void OnItemPickedUp(ItemPickedUpEvent ev)
        {
            if (!TryGetByItem(ev.Item, out var ci)) return;

            if (ev.Player != null)
            {
                ci!.OnAcquired(ev.Player, ev.Item);
                if (ci.PickupHint != null && ev.Player.IsLocal)
                    DANO.API.Hud.ShowHint(ci.PickupHint, duration: 3f);
            }
            ci!.OnPickedUp(ev);
        }

        private static void OnItemDropping(ItemDroppingEvent ev)
        {
            if (!TryGetByItem(ev.Item, out var ci)) return;
            ci!.OnDropping(ev);
        }

        private static void OnItemDropped(ItemDroppedEvent ev)
        {
            if (!TryGetByItem(ev.Item, out var ci)) return;
            if (ev.Player != null)
                ci!.OnReleased(ev.Player, ev.Item);
            ci!.OnDropped(ev);
        }

        private static void OnWeaponFiring(WeaponFiringEvent ev)
        {
            if (ev.Item == null || !TryGetByItem(ev.Item, out var ci)) return;
            ci!.OnFiring(ev);
        }

        private static void OnWeaponFired(WeaponFiredEvent ev)
        {
            if (ev.Item == null || !TryGetByItem(ev.Item, out var ci)) return;
            ci!.OnFired(ev);
        }

        private static void OnWeaponReloading(WeaponReloadingEvent ev)
        {
            if (ev.Item == null || !TryGetByItem(ev.Item, out var ci)) return;
            ci!.OnReloading(ev);
        }

        private static void OnWeaponReloaded(WeaponReloadedEvent ev)
        {
            if (ev.Item == null || !TryGetByItem(ev.Item, out var ci)) return;
            ci!.OnReloaded(ev);
        }

        private static void OnPlayerDamaging(PlayerDamagingEvent ev)
        {
            if (ev.Player == null) return;
            foreach (var item in ev.Player.GetHeldItems())
            {
                if (TryGetByItem(item, out var ci))
                    ci!.OnOwnerDamaging(ev);
            }
        }

        private static void OnPlayerDied(PlayerDiedEvent ev)
        {
            if (ev.Player == null) return;
            foreach (var item in ev.Player.GetHeldItems())
            {
                if (TryGetByItem(item, out var ci))
                    ci!.OnOwnerDying(ev);
            }
        }

        private static void DispatchRoundStarted(RoundStartedEvent ev)
        {
            PruneDestroyedItems();
            foreach (var ci in _byId.Values)
                ci.OnRoundStarted(ev);
        }

        // ─── 配布 ───

        /// <summary>
        /// 指定プレイヤーの足元にカスタムアイテムをスポーンして渡す。
        /// </summary>
        public DANO.API.Item? Give(DANO.API.Player player)
        {
            var item = DANO.API.Item.Spawn(BaseWeaponName, player.Position + Vector3.up);
            if (item == null) return null;
            TrackItem(item);
            return item;
        }

        /// <summary>
        /// 指定座標にカスタムアイテムをスポーンする。
        /// </summary>
        public DANO.API.Item? Spawn(Vector3 position)
        {
            var item = DANO.API.Item.Spawn(BaseWeaponName, position);
            if (item == null) return null;
            TrackItem(item);
            return item;
        }

        /// <summary>
        /// 既存の Item をこのカスタムアイテムとして追跡登録する。
        /// アイテムを自力でインスタンス化した場合に使う。
        /// </summary>
        public void TrackItem(DANO.API.Item item)
        {
            if (item?.Base == null) return;
            var id = item.Base.GetInstanceID();
            _trackedIds.Add(id);
            _globalTracker[id] = this;
        }

        /// <summary>
        /// 追跡中の生存アイテムを全て取得する。
        /// </summary>
        public IEnumerable<DANO.API.Item> GetTrackedItems()
        {
            foreach (var item in DANO.API.Item.List)
            {
                if (item?.Base == null) continue;
                if (_trackedIds.Contains(item.Base.GetInstanceID()))
                    yield return item;
            }
        }

        // ─── クエリ ───

        /// <summary>指定アイテムがいずれかのカスタムアイテムか確認する</summary>
        public static bool Check(DANO.API.Item item) => TryGetByItem(item, out _);

        /// <summary>指定アイテムに対応するCustomItemを取得する</summary>
        public static bool TryGet(DANO.API.Item item, out CustomItem? ci) => TryGetByItem(item, out ci);

        /// <summary>IDでカスタムアイテムを取得する</summary>
        public static bool TryGet(uint id, out CustomItem? ci) => _byId.TryGetValue(id, out ci);

        /// <summary>名前でカスタムアイテムを取得する（大文字小文字無視）</summary>
        public static bool TryGet(string name, out CustomItem? ci) => _byName.TryGetValue(name, out ci);

        /// <summary>型でカスタムアイテムのインスタンスを取得する</summary>
        public static T? Get<T>() where T : CustomItem =>
            _byId.Values.OfType<T>().FirstOrDefault();

        /// <summary>登録済みの全カスタムアイテムを取得する</summary>
        public static IEnumerable<CustomItem> GetAll() => _byId.Values;

        /// <summary>指定プレイヤーがこのカスタムアイテムを手に持っているか確認する</summary>
        public bool Has(DANO.API.Player player)
        {
            foreach (var item in player.GetHeldItems())
            {
                if (item?.Base != null && _trackedIds.Contains(item.Base.GetInstanceID()))
                    return true;
            }
            return false;
        }

        // ─── 内部ユーティリティ ───

        private static bool TryGetByItem(DANO.API.Item? item, out CustomItem? ci)
        {
            ci = null;
            if (item?.Base == null) return false;
            return _globalTracker.TryGetValue(item.Base.GetInstanceID(), out ci);
        }

        private static void PruneDestroyedItems()
        {
            // InstanceID で追跡しているため、破棄済みオブジェクトのチェックは
            // Item.List との照合で行う
            var liveIds = new HashSet<int>();
            foreach (var item in DANO.API.Item.List)
            {
                if (item?.Base != null)
                    liveIds.Add(item.Base.GetInstanceID());
            }

            var toRemove = _globalTracker.Keys.Where(id => !liveIds.Contains(id)).ToList();
            foreach (var deadId in toRemove)
            {
                if (_globalTracker.TryGetValue(deadId, out var ci))
                    ci._trackedIds.Remove(deadId);
                _globalTracker.Remove(deadId);
            }
        }

        // ─── 仮想フック ───

        /// <summary>
        /// プレイヤーがこのカスタムアイテムを手に持った（OnPickedUpの直後）。
        /// </summary>
        protected virtual void OnAcquired(DANO.API.Player player, DANO.API.Item item) { }

        /// <summary>
        /// プレイヤーがこのカスタムアイテムを手放した（OnDroppedの直後）。
        /// </summary>
        protected virtual void OnReleased(DANO.API.Player player, DANO.API.Item item) { }

        /// <summary>
        /// プレイヤーがこのアイテムを拾おうとしている（Cancel可）。
        /// ev.Cancel = true でキャンセルできる。
        /// </summary>
        protected virtual void OnPickingUp(ItemPickingUpEvent ev) { }

        /// <summary>プレイヤーがこのアイテムを拾った後。</summary>
        protected virtual void OnPickedUp(ItemPickedUpEvent ev) { }

        /// <summary>
        /// プレイヤーがこのアイテムを落とそうとしている（Cancel可）。
        /// ev.Cancel = true でキャンセルできる。
        /// </summary>
        protected virtual void OnDropping(ItemDroppingEvent ev) { }

        /// <summary>プレイヤーがこのアイテムを落とした後。</summary>
        protected virtual void OnDropped(ItemDroppedEvent ev) { }

        /// <summary>
        /// このアイテムを持つローカルプレイヤーが発射しようとしている（Cancel可）。
        /// ev.Cancel = true で発射をキャンセルできる。
        /// <br/>
        /// ⚠ FishNet制約: ローカルプレイヤーのみ発火。リモートプレイヤーの発射は検出不可。
        /// </summary>
        protected virtual void OnFiring(WeaponFiringEvent ev) { }

        /// <summary>
        /// このアイテムを持つローカルプレイヤーが発射した後。
        /// <br/>
        /// ⚠ FishNet制約: ローカルプレイヤーのみ発火。
        /// </summary>
        protected virtual void OnFired(WeaponFiredEvent ev) { }

        /// <summary>
        /// このアイテムを持つローカルプレイヤーがリロードしようとしている（Cancel可）。
        /// <br/>
        /// ⚠ FishNet制約: ローカルプレイヤーのみ発火。
        /// </summary>
        protected virtual void OnReloading(WeaponReloadingEvent ev) { }

        /// <summary>
        /// このアイテムを持つローカルプレイヤーがリロードした後。
        /// <br/>
        /// ⚠ FishNet制約: ローカルプレイヤーのみ発火。
        /// </summary>
        protected virtual void OnReloaded(WeaponReloadedEvent ev) { }

        /// <summary>
        /// このアイテムを持つプレイヤーがダメージを受けようとしている（Cancel可）。
        /// ev.Cancel = true でダメージをキャンセルできる。
        /// </summary>
        protected virtual void OnOwnerDamaging(PlayerDamagingEvent ev) { }

        /// <summary>このアイテムを持つプレイヤーが死亡した。</summary>
        protected virtual void OnOwnerDying(PlayerDiedEvent ev) { }

        /// <summary>
        /// ラウンド開始時に全カスタムアイテムに対して呼ばれる。
        /// 破棄済みアイテムのプルーニングもこのタイミングで行われる。
        /// </summary>
        protected virtual void OnRoundStarted(RoundStartedEvent ev) { }
    }
}
