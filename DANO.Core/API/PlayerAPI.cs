using System.Collections.Generic;
using UnityEngine;

namespace DANO.API
{
    /// <summary>プレイヤー情報へのアクセスAPIまとめ</summary>
    public static class PlayerAPI
    {
        /// <summary>接続中の全プレイヤーを返す</summary>
        public static IReadOnlyDictionary<int, ClientInstance> All =>
            ClientInstance.playerInstances;

        /// <summary>ローカルプレイヤーのClientInstance</summary>
        public static ClientInstance? Local =>
            ClientInstance.Instance;

        /// <summary>IDからClientInstanceを取得</summary>
        public static ClientInstance? Get(int playerId) =>
            ClientInstance.playerInstances.TryGetValue(playerId, out var ci) ? ci : null;

        /// <summary>ClientInstanceからPlayerHealthを取得</summary>
        public static PlayerHealth? GetHealth(ClientInstance player) =>
            player.PlayerSpawner?.player?.GetComponent<PlayerHealth>();

        /// <summary>ClientInstanceからFirstPersonControllerを取得</summary>
        public static FirstPersonController? GetController(ClientInstance player) =>
            player.PlayerSpawner?.player;

        /// <summary>指定プレイヤーが生きているか</summary>
        public static bool IsAlive(int playerId) =>
            GameManager.Instance != null && GameManager.Instance.alivePlayers.Contains(playerId);

        /// <summary>指定プレイヤーのチームIDを返す（ScoreManagerから）</summary>
        public static int GetTeamId(int playerId) =>
            ScoreManager.Instance?.GetTeamId(playerId) ?? -1;
    }
}
