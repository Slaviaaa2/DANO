using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DANO.Events;
using UnityEngine;

namespace DANOUE.API
{
    /// <summary>
    /// DANOUEカスタムロールの基底クラス。
    /// このクラスの1インスタンス = ロール「型」1種類を表すシングルトン。
    /// 複数プレイヤーに同じロールを付与できる。
    ///
    /// 使い方:
    /// <code>
    /// public class Assassin : CustomRole
    /// {
    ///     public override uint   Id          => 1;
    ///     public override string RoleName    => "アサシン";
    ///     public override string Description => "暗殺者。近接攻撃が得意。";
    ///     public override float  MaxHealth   => 80f;
    ///     public override string RoleColor   => "#AA00FF";
    ///     public override List&lt;string&gt; SpawnItems => new() { "Knife" };
    /// }
    ///
    /// // プラグインの OnEnabled() で:
    /// CustomRole.Register&lt;Assassin&gt;();
    ///
    /// // ロールを付与:
    /// CustomRole.Get&lt;Assassin&gt;()?.Assign(player);
    /// </code>
    /// </summary>
    public abstract class CustomRole
    {
        // ─── 型情報（サブクラスで定義） ───

        /// <summary>カスタムロールの一意なID（プラグイン内で重複不可）</summary>
        public abstract uint Id { get; }

        /// <summary>カスタムロールの表示名</summary>
        public abstract string RoleName { get; }

        /// <summary>説明文（ロール付与時のヒントに表示）</summary>
        public virtual string Description { get; } = "";

        /// <summary>
        /// ロール付与時に変更するチームID。
        /// -1 の場合はチームを変更しない。
        /// </summary>
        public virtual int TeamId { get; } = -1;

        /// <summary>
        /// ロール付与時に設定する最大HP。
        /// 0f の場合はデフォルトのまま変更しない。
        /// </summary>
        public virtual float MaxHealth { get; } = 0f;

        /// <summary>
        /// ロール付与時にプレイヤーの足元にスポーンするアイテム名リスト。
        /// </summary>
        public virtual List<string> SpawnItems { get; } = new List<string>();

        /// <summary>
        /// ラウンドリセット（RoundStarted）時にロールを維持するか。
        /// false（デフォルト）の場合、新ラウンド開始時にロールが自動解除される。
        /// </summary>
        public virtual bool KeepRoleOnRoundReset { get; } = false;

        /// <summary>
        /// HUDヒントに表示するロール名の色（hex形式）。例: "#AA00FF"
        /// </summary>
        public virtual string RoleColor { get; } = "#FFFFFF";

        // ─── プレイヤー追跡 ───

        private readonly HashSet<int> _players = new HashSet<int>();

        private static readonly Dictionary<uint, CustomRole> _byId =
            new Dictionary<uint, CustomRole>();

        private static readonly Dictionary<string, CustomRole> _byName =
            new Dictionary<string, CustomRole>(StringComparer.OrdinalIgnoreCase);

        /// <summary>プレイヤーID → CustomRole のグローバルルックアップ</summary>
        private static readonly Dictionary<int, CustomRole> _playerRoles =
            new Dictionary<int, CustomRole>();

        private static bool _subscribed = false;

        // ─── 登録 ───

        /// <summary>
        /// カスタムロール型をDANOUEに登録する。
        /// OnEnabled() 内で呼ぶこと。
        /// </summary>
        public static void Register<T>() where T : CustomRole, new()
            => RegisterInstance(new T());

        /// <summary>
        /// アセンブリ内の全CustomRoleサブクラスを自動登録する。
        /// 抽象クラスはスキップされる。
        /// </summary>
        public static void RegisterAll(Assembly assembly)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (type.IsAbstract || !typeof(CustomRole).IsAssignableFrom(type))
                    continue;
                RegisterInstance((CustomRole)Activator.CreateInstance(type));
            }
        }

        /// <summary>
        /// カスタムロール型の登録を解除し、全プレイヤーからロールを削除する。
        /// OnDisabled() 内で呼ぶこと。
        /// </summary>
        public static void Unregister<T>() where T : CustomRole
        {
            var target = _byId.Values.OfType<T>().FirstOrDefault();
            if (target == null) return;
            target.RemoveAll();
            _byId.Remove(target.Id);
            _byName.Remove(target.RoleName);
        }

        private static void RegisterInstance(CustomRole instance)
        {
            if (_byId.ContainsKey(instance.Id))
                throw new InvalidOperationException(
                    $"[DANOUE] CustomRole ID {instance.Id} は既に登録されています: {_byId[instance.Id].RoleName}");

            _byId[instance.Id] = instance;
            _byName[instance.RoleName] = instance;
            EnsureSubscribed();
        }

        /// <summary>初回 Register 時にEventBusへ遅延サブスクライブする</summary>
        private static void EnsureSubscribed()
        {
            if (_subscribed) return;
            _subscribed = true;

            EventBus.Subscribe<PlayerSpawnedEvent>(OnPlayerSpawned, priority: -100);
            EventBus.Subscribe<PlayerDamagingEvent>(OnPlayerDamaging, priority: -100);
            EventBus.Subscribe<PlayerDamagedEvent>(OnPlayerDamaged, priority: -100);
            EventBus.Subscribe<PlayerDiedEvent>(OnPlayerDied, priority: -100);
            EventBus.Subscribe<TeamChangedEvent>(DispatchTeamChanged, priority: -100);
            EventBus.Subscribe<PlayerDisconnectedEvent>(OnPlayerDisconnected, priority: -100);
            EventBus.Subscribe<RoundStartedEvent>(DispatchRoundStarted, priority: -100);
            EventBus.Subscribe<RoundEndedEvent>(DispatchRoundEnded, priority: -100);
        }

        // ─── イベントルーティング（静的） ───

        private static void OnPlayerSpawned(PlayerSpawnedEvent ev)
        {
            if (ev.Player == null) return;
            if (!_playerRoles.TryGetValue(ev.Player.Id, out var role)) return;
            role.OnSpawned(ev);
        }

        private static void OnPlayerDamaging(PlayerDamagingEvent ev)
        {
            if (ev.Player == null) return;
            if (!_playerRoles.TryGetValue(ev.Player.Id, out var role)) return;
            role.OnDamaging(ev);
        }

        private static void OnPlayerDamaged(PlayerDamagedEvent ev)
        {
            if (ev.Player == null) return;
            if (!_playerRoles.TryGetValue(ev.Player.Id, out var role)) return;
            role.OnDamaged(ev);
        }

        private static void OnPlayerDied(PlayerDiedEvent ev)
        {
            if (ev.Player == null) return;
            if (!_playerRoles.TryGetValue(ev.Player.Id, out var role)) return;
            role.OnDying(ev);
        }

        private static void DispatchTeamChanged(TeamChangedEvent ev)
        {
            if (!_playerRoles.TryGetValue(ev.PlayerId, out var role)) return;
            role.OnTeamChanged(ev);
        }

        private static void OnPlayerDisconnected(PlayerDisconnectedEvent ev)
        {
            if (!_playerRoles.TryGetValue(ev.PlayerId, out var role)) return;
            role._players.Remove(ev.PlayerId);
            _playerRoles.Remove(ev.PlayerId);
        }

        private static void DispatchRoundStarted(RoundStartedEvent ev)
        {
            foreach (var role in _byId.Values)
            {
                if (!role.KeepRoleOnRoundReset)
                {
                    foreach (var pid in role._players.ToList())
                        _playerRoles.Remove(pid);
                    role._players.Clear();
                }
                role.OnRoundStarted(ev);
            }
        }

        private static void DispatchRoundEnded(RoundEndedEvent ev)
        {
            foreach (var role in _byId.Values)
                role.OnRoundEnded(ev);
        }

        // ─── ロール割り当て ───

        /// <summary>
        /// 指定プレイヤーにこのロールを付与する。
        /// 既に別のカスタムロールを持っている場合は先に解除される。
        /// MaxHealth / TeamId / SpawnItems が設定されていれば自動適用する。
        /// </summary>
        public void Assign(DANO.API.Player player)
        {
            if (_playerRoles.TryGetValue(player.Id, out var existing) && existing != this)
                existing.Remove(player);

            _players.Add(player.Id);
            _playerRoles[player.Id] = this;

            ApplyLoadout(player);
            ShowRoleHint(player);
            OnRoleAssigned(player);
        }

        /// <summary>指定プレイヤーからこのロールを解除する。</summary>
        public void Remove(DANO.API.Player player)
        {
            if (!_players.Contains(player.Id)) return;
            _players.Remove(player.Id);
            _playerRoles.Remove(player.Id);
            OnRoleRemoved(player);
        }

        /// <summary>このロールを持つ全プレイヤーからロールを解除する。</summary>
        public void RemoveAll()
        {
            foreach (var pid in _players.ToList())
            {
                _playerRoles.Remove(pid);
                var player = DANO.API.Player.Get(pid);
                if (player != null) OnRoleRemoved(player);
            }
            _players.Clear();
        }

        // ─── クエリ ───

        /// <summary>指定プレイヤーがいずれかのカスタムロールを持っているか確認する</summary>
        public static bool Check(DANO.API.Player player) => _playerRoles.ContainsKey(player.Id);

        /// <summary>指定プレイヤーに割り当てられているカスタムロールを取得する</summary>
        public static bool TryGet(DANO.API.Player player, out CustomRole? role) =>
            _playerRoles.TryGetValue(player.Id, out role);

        /// <summary>IDでカスタムロールを取得する</summary>
        public static bool TryGet(uint id, out CustomRole? role) => _byId.TryGetValue(id, out role);

        /// <summary>名前でカスタムロールを取得する（大文字小文字無視）</summary>
        public static bool TryGet(string name, out CustomRole? role) => _byName.TryGetValue(name, out role);

        /// <summary>型でカスタムロールのインスタンスを取得する</summary>
        public static T? Get<T>() where T : CustomRole =>
            _byId.Values.OfType<T>().FirstOrDefault();

        /// <summary>登録済みの全カスタムロールを取得する</summary>
        public static IEnumerable<CustomRole> GetAll() => _byId.Values;

        /// <summary>指定プレイヤーがこのロールを持っているか確認する</summary>
        public bool Has(DANO.API.Player player) => _players.Contains(player.Id);

        /// <summary>このロールを持つ全プレイヤーを取得する</summary>
        public IEnumerable<DANO.API.Player> GetPlayers()
        {
            foreach (var pid in _players)
            {
                var p = DANO.API.Player.Get(pid);
                if (p != null) yield return p;
            }
        }

        // ─── 内部ユーティリティ ───

        private void ApplyLoadout(DANO.API.Player player)
        {
            if (MaxHealth > 0f)
            {
                var hp = player.HealthComponent;
                if (hp != null)
                {
                    hp.fullHealth = MaxHealth;
                    hp.health = MaxHealth;
                }
            }

            if (TeamId >= 0)
                player.SetTeam(TeamId);

            foreach (var weaponName in SpawnItems)
                player.GiveItem(weaponName);
        }

        private void ShowRoleHint(DANO.API.Player player)
        {
            if (!player.IsLocal) return;
            var text = string.IsNullOrEmpty(Description)
                ? $"<color={RoleColor}>[{RoleName}]</color>"
                : $"<color={RoleColor}>[{RoleName}]</color> {Description}";
            DANO.API.Hud.ShowHint(text, duration: 5f);
        }

        // ─── 仮想フック ───

        /// <summary>
        /// このロールがプレイヤーに付与された直後に呼ばれる。
        /// MaxHealth / TeamId / SpawnItems の適用後に呼ばれる。
        /// </summary>
        protected virtual void OnRoleAssigned(DANO.API.Player player) { }

        /// <summary>このロールがプレイヤーから解除された直後に呼ばれる。</summary>
        protected virtual void OnRoleRemoved(DANO.API.Player player) { }

        /// <summary>このロールを持つプレイヤーがスポーンした。</summary>
        protected virtual void OnSpawned(PlayerSpawnedEvent ev) { }

        /// <summary>
        /// このロールを持つプレイヤーがダメージを受けようとしている（Cancel可）。
        /// ev.Cancel = true でダメージをキャンセルできる。
        /// </summary>
        protected virtual void OnDamaging(PlayerDamagingEvent ev) { }

        /// <summary>このロールを持つプレイヤーがダメージを受けた後。</summary>
        protected virtual void OnDamaged(PlayerDamagedEvent ev) { }

        /// <summary>
        /// このロールを持つプレイヤーが死亡した。
        /// KeepRoleOnRoundReset=false の場合、次ラウンド開始でロールは自動解除される。
        /// </summary>
        protected virtual void OnDying(PlayerDiedEvent ev) { }

        /// <summary>
        /// ラウンド開始時に呼ばれる。
        /// KeepRoleOnRoundReset=false の場合、このメソッド呼び出し前にロールは既に解除済み。
        /// </summary>
        protected virtual void OnRoundStarted(RoundStartedEvent ev) { }

        /// <summary>
        /// ラウンド終了時に呼ばれる。
        /// ロール解除はまだ行われていない（次のOnRoundStartedで解除される）。
        /// </summary>
        protected virtual void OnRoundEnded(RoundEndedEvent ev) { }

        /// <summary>このロールを持つプレイヤーのチームが変更された。</summary>
        protected virtual void OnTeamChanged(TeamChangedEvent ev) { }
    }
}
