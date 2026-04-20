namespace DANO.Events
{
    /// <summary>ドアが操作されようとしているときのイベント（Cancel可）</summary>
    public class DoorInteractingEvent
    {
        /// <summary>操作されるドア</summary>
        public API.Door Door { get; }

        /// <summary>インタラクト前の開閉状態（true = 開いていた）</summary>
        public bool WasOpen { get; }

        /// <summary>trueにするとドアの開閉をキャンセルする</summary>
        public bool Cancel { get; set; }

        internal DoorInteractingEvent(global::Door door, bool wasOpen)
        {
            Door = API.Door.Get(door);
            WasOpen = wasOpen;
        }
    }

    /// <summary>ドアが操作された後のイベント（通知のみ）</summary>
    public class DoorInteractedEvent
    {
        /// <summary>操作されたドア</summary>
        public API.Door Door { get; }

        /// <summary>インタラクト前の開閉状態（true = 開いていた）</summary>
        public bool WasOpen { get; }

        internal DoorInteractedEvent(API.Door door, bool wasOpen)
        {
            Door = door;
            WasOpen = wasOpen;
        }
    }
}
