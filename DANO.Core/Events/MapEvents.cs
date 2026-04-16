namespace DANO.Events
{
    /// <summary>マップが変更されたときのイベント</summary>
    public class MapChangedEvent
    {
        /// <summary>新しいマップ名</summary>
        public string MapName { get; }
        /// <summary>前のマップ名（初回は空文字）</summary>
        public string PreviousMapName { get; }

        internal MapChangedEvent(string mapName, string previousMapName)
        {
            MapName = mapName;
            PreviousMapName = previousMapName;
        }
    }
}
