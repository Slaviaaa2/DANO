using DANO.API;
using UnityEngine;

namespace DANO.Events
{
    /// <summary>ドアがインタラクトされた時に発火するイベント</summary>
    public class DoorInteractEvent
    {
        /// <summary>操作されたドア</summary>
        public DanoDoor Door { get; }

        /// <summary>操作したプレイヤー（不明な場合 null）</summary>
        public API.Player? Player { get; }

        /// <summary>インタラクト前の開閉状態（true = 開いていた）</summary>
        public bool WasOpen { get; }

        /// <summary>キャンセルすると開閉が実行されない</summary>
        public bool Cancel { get; set; }

        internal DoorInteractEvent(Door door, Transform? playerTransform)
        {
            Door = DanoDoor.Get(door);
            WasOpen = door.sync___get_value_isOpen();
            Player = API.Player.FromTransform(playerTransform);
        }
    }
}
