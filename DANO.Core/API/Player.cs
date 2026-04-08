using System.Collections.Generic;
using System.Linq;
using FishNet.Connection;
using FishNet.Object;
using UnityEngine;

namespace DANO.API
{
    /// <summary>
    /// STRAFTAT プレイヤーのラッパー。
    /// ClientInstance 等の生ゲーム型を隠蔽し、直感的なプロパティを提供する。
    /// </summary>
    public class Player
    {
        // ─── キャッシュ ───
        private static readonly Dictionary<int, Player> _cache = new Dictionary<int, Player>();

        // ─── Static ルックアップ ───

        /// <summary>プレイヤーIDからPlayerを取得</summary>
        public static Player? Get(int playerId)
        {
            if (_cache.TryGetValue(playerId, out var cached) && cached.Base != null)
                return cached;

            if (!ClientInstance.playerInstances.TryGetValue(playerId, out var ci))
                return null;

            var p = new Player(ci);
            _cache[playerId] = p;
            return p;
        }

        /// <summary>ClientInstanceからPlayerを取得（内部用）</summary>
        public static Player Get(ClientInstance ci)
        {
            var id = ci.PlayerId;
            if (_cache.TryGetValue(id, out var cached) && cached.Base == ci)
                return cached;

            var p = new Player(ci);
            _cache[id] = p;
            return p;
        }

        /// <summary>ローカルプレイヤー</summary>
        public static Player? Local =>
            ClientInstance.Instance != null ? Get(ClientInstance.Instance) : null;

        /// <summary>接続中の全プレイヤー</summary>
        public static IEnumerable<Player> List =>
            ClientInstance.playerInstances.Values
                .Where(ci => ci != null)
                .Select(ci => Get(ci));

        /// <summary>Transform からプレイヤーを解決する（Killer 解決用）</summary>
        public static Player? FromTransform(Transform? transform)
        {
            if (transform == null) return null;
            var netObj = transform.GetComponent<NetworkObject>()
                      ?? transform.GetComponentInParent<NetworkObject>();
            if (netObj == null) return null;
            return Get(netObj.OwnerId);
        }

        /// <summary>PlayerHealth からプレイヤーを解決する（パッチ用）</summary>
        public static Player? FromHealth(PlayerHealth health)
        {
            var netObj = health.GetComponent<NetworkObject>();
            if (netObj == null) return null;
            return Get(netObj.OwnerId);
        }

        /// <summary>キャッシュからプレイヤーを削除する（切断時に呼ばれる）</summary>
        internal static void Invalidate(int playerId) => _cache.Remove(playerId);

        // ─── インスタンス ───

        /// <summary>生の ClientInstance（上級者向け）</summary>
        public ClientInstance Base { get; }

        private Player(ClientInstance ci) { Base = ci; }

        // ─── プロパティ ───

        /// <summary>プレイヤーID</summary>
        public int Id => Base.PlayerId;

        /// <summary>プレイヤー名</summary>
        public string Name => Base.PlayerName ?? "";

        /// <summary>SteamID</summary>
        public ulong SteamId => Base.PlayerSteamID;

        /// <summary>ローカルプレイヤーかどうか</summary>
        public bool IsLocal => Base == ClientInstance.Instance;

        /// <summary>ホスト（サーバー）かどうか</summary>
        public bool IsHost => Base.IsHost;

        /// <summary>FirstPersonController（スポーン済みキャラクター）</summary>
        public FirstPersonController? Controller => Base.PlayerSpawner?.player;

        /// <summary>PlayerHealth コンポーネント</summary>
        public PlayerHealth? HealthComponent => Controller?.GetComponent<PlayerHealth>();

        /// <summary>現在HP（読み書き可）</summary>
        public float Health
        {
            get => HealthComponent?.health ?? 0f;
            set { if (HealthComponent != null) HealthComponent.health = value; }
        }

        /// <summary>最大HP</summary>
        public float MaxHealth => HealthComponent?.fullHealth ?? 100f;

        /// <summary>生存中かどうか</summary>
        public bool IsAlive =>
            GameManager.Instance != null && GameManager.Instance.alivePlayers.Contains(Id);

        /// <summary>チームID</summary>
        public int TeamId => ScoreManager.Instance?.GetTeamId(Id) ?? -1;

        /// <summary>ワールド座標（読み書き可）</summary>
        public Vector3 Position
        {
            get => Controller?.transform.position ?? Vector3.zero;
            set { if (Controller != null) Controller.transform.position = value; }
        }

        /// <summary>回転（読み書き可）</summary>
        public Quaternion Rotation
        {
            get => Controller?.transform.rotation ?? Quaternion.identity;
            set { if (Controller != null) Controller.transform.rotation = value; }
        }

        // ─── 移動状態 ───

        /// <summary>走行中かどうか</summary>
        public bool IsSprinting => Controller?.isSprinting ?? false;

        /// <summary>歩行中かどうか</summary>
        public bool IsWalking => Controller?.isWalking ?? false;

        /// <summary>しゃがみ中かどうか</summary>
        public bool IsCrouching => Controller?.isCrouching ?? false;

        /// <summary>接地しているかどうか</summary>
        public bool IsGrounded => Controller?.isGrounded ?? false;

        /// <summary>スライディング中かどうか</summary>
        public bool IsSliding => Controller?.isSliding ?? false;

        /// <summary>リーン中かどうか</summary>
        public bool IsLeaning => Controller?.isLeaning ?? false;

        /// <summary>エイム（ADS）中かどうか</summary>
        public bool IsAiming => Controller?.isAiming ?? false;

        /// <summary>スコープ覗き中かどうか</summary>
        public bool IsScopeAiming => Controller?.isScopeAiming ?? false;

        /// <summary>現在の移動速度</summary>
        public float Speed => Controller?.playerSpeed ?? 0f;

        /// <summary>移動可能かどうか（読み書き可）</summary>
        public bool CanMove
        {
            get => Controller?.canMove ?? false;
            set { if (Controller != null) Controller.canMove = value; }
        }

        // ─── メソッド ───

        /// <summary>体力を全て削って即死させる</summary>
        public void Kill()
        {
            HealthComponent?.RemoveHealth(float.MaxValue);
        }

        /// <summary>指定量のダメージを与える</summary>
        public void Damage(float amount)
        {
            HealthComponent?.RemoveHealth(amount);
        }

        /// <summary>指定量回復する（最大HPまで）</summary>
        public void Heal(float amount)
        {
            var hp = HealthComponent;
            if (hp == null) return;
            hp.health = Mathf.Min(hp.health + amount, hp.fullHealth);
        }

        /// <summary>体力を全回復する</summary>
        public void HealFull()
        {
            var hp = HealthComponent;
            if (hp == null) return;
            hp.health = hp.fullHealth;
        }

        /// <summary>指定座標にテレポートする</summary>
        public void Teleport(Vector3 position)
        {
            if (Controller != null)
                Controller.transform.position = position;
        }

        /// <summary>指定座標・回転にテレポートする</summary>
        public void Teleport(Vector3 position, Quaternion rotation)
        {
            if (Controller == null) return;
            Controller.transform.position = position;
            Controller.transform.rotation = rotation;
        }

        /// <summary>チームを変更する</summary>
        public void SetTeam(int teamId)
        {
            ScoreManager.Instance?.SetTeamId(Id, teamId);
        }

        /// <summary>移動を一時的に無効化する（フリーズ）</summary>
        public void Freeze()
        {
            if (Controller != null) Controller.canMove = false;
        }

        /// <summary>移動を再有効化する（アンフリーズ）</summary>
        public void Unfreeze()
        {
            if (Controller != null) Controller.canMove = true;
        }

        /// <summary>スタンさせる（指定秒数後に自動解除）</summary>
        public void Stun(float duration)
        {
            var hp = HealthComponent;
            var localHp = API.Player.Local?.HealthComponent;
            if (hp == null || localHp == null) return;
            localHp.TaserEnemy(hp, duration);
        }

        /// <summary>物理的な力を加える</summary>
        public void AddForce(Vector3 force, float factor = 1f)
        {
            HealthComponent?.AddForce(force, factor);
        }

        /// <summary>プレイヤーをサーバーからキックする</summary>
        public void Kick(string message = "")
        {
            if (GameManager.Instance == null) return;
            var conn = Base.GetComponent<NetworkObject>()?.Owner;
            if (conn != null)
                GameManager.Instance.CmdKickPlayer(conn, message);
        }

        /// <summary>手に持っているアイテムを取得する</summary>
        public Item? GetHeldItem(bool rightHand = true)
        {
            var pickup = Controller?.GetComponent<PlayerPickup>();
            if (pickup == null) return null;
            var obj = rightHand ? pickup.objInHand : pickup.objInLeftHand;
            if (obj == null) return null;
            var ib = obj.GetComponent<ItemBehaviour>();
            return ib != null ? Item.Get(ib) : null;
        }

        /// <summary>リスポーンする（ローカルプレイヤーのみ）</summary>
        public void Respawn()
        {
            Base.PlayerSpawner?.TryRespawn();
        }

        /// <summary>アニメーションを再生する（エモート等）</summary>
        public void PlayAnimation(int animationIndex)
        {
            Base.MenuAnimationServer(animationIndex, Id);
        }

        /// <summary>レディ状態を設定する</summary>
        public void SetReady(bool ready)
        {
            Base.ServerSetPlayerReadyState(ready);
        }

        // ─── 等値・ハッシュ ───

        public override bool Equals(object obj) =>
            obj is Player other && other.Id == Id;

        public override int GetHashCode() => Id;

        public override string ToString() => $"Player({Id}, {Name})";

        public static bool operator ==(Player? a, Player? b) =>
            ReferenceEquals(a, b) || (a is not null && b is not null && a.Id == b.Id);

        public static bool operator !=(Player? a, Player? b) => !(a == b);
    }
}
