using DANO.API;

namespace DANO.Events
{
    /// <summary>ドアが操作されようとしているときのイベント（Cancel可）</summary>
    public class DoorInteractingEvent
    {
        /// <summary>操作されるドア</summary>
        public DanoDoor Door { get; }

        /// <summary>インタラクト前の開閉状態（true = 開いていた）</summary>
        public bool WasOpen { get; }

        /// <summary>trueにするとドアの開閉をキャンセルする</summary>
        public bool Cancel { get; set; }

        internal DoorInteractingEvent(Door door, bool wasOpen)
        {
            Door = DanoDoor.Get(door);
            WasOpen = wasOpen;
        }
    }

    /// <summary>ドアが操作された後のイベント（通知のみ）</summary>
    public class DoorInteractedEvent
    {
        /// <summary>操作されたドア</summary>
        public DanoDoor Door { get; }

        /// <summary>インタラクト前の開閉状態（true = 開いていた）</summary>
        public bool WasOpen { get; }

        internal DoorInteractedEvent(DanoDoor door, bool wasOpen)
        {
            Door = door;
            WasOpen = wasOpen;
        }
    }
}
