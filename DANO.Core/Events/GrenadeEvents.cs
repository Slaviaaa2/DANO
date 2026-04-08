using UnityEngine;

namespace DANO.Events
{
    /// <summary>グレネードが爆発したときのイベント</summary>
    public class GrenadeExplodedEvent
    {
        /// <summary>爆発位置</summary>
        public Vector3 Position { get; }
        /// <summary>爆発半径</summary>
        public float Radius { get; }
        /// <summary>フラググレネードかどうか</summary>
        public bool IsFragGrenade { get; }
        /// <summary>スタングレネードかどうか</summary>
        public bool IsStunGrenade { get; }

        internal GrenadeExplodedEvent(Vector3 position, float radius, bool isFrag, bool isStun)
        {
            Position = position;
            Radius = radius;
            IsFragGrenade = isFrag;
            IsStunGrenade = isStun;
        }
    }
}
