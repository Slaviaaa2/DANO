using DANO.API;

namespace DANO.Events
{
    /// <summary>ドアの開閉状態が変化した時に発火するイベント</summary>
    public class DoorInteractEvent
    {
        /// <summary>操作されたドア</summary>
        public DanoDoor Door { get; }

        /// <summary>インタラクト前の開閉状態（true = 開いていた）</summary>
        public bool WasOpen { get; }

        /// <summary>trueにするとドアの開閉を巻き戻す</summary>
        public bool Cancel { get; set; }

        internal DoorInteractEvent(Door door, bool wasOpen)
        {
            Door = DanoDoor.Get(door);
            WasOpen = wasOpen;
        }
    }
}
