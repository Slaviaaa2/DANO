using System.Collections.Generic;

namespace DANO.API
{
    /// <summary>マップ情報へのアクセス</summary>
    public static class Map
    {
        /// <summary>現在のマップ名</summary>
        public static string CurrentMap =>
            SceneMotor.Instance?.currentSceneName ?? "";

        /// <summary>ロード中かどうか</summary>
        public static bool IsLoading =>
            SceneMotor.Instance?.inLoadingScreen ?? false;

        /// <summary>テストマップかどうか</summary>
        public static bool IsTestMap =>
            SceneMotor.Instance?.testMap ?? false;

        /// <summary>探索マップかどうか</summary>
        public static bool IsExplorationMap =>
            MapsManager.Instance?.inExplorationMap ?? false;

        /// <summary>プレイリストに入っているマップ名一覧</summary>
        public static IReadOnlyList<string> Playlist
        {
            get
            {
                var sm = SceneMotor.Instance;
                if (sm?.PlayListMaps == null) return new List<string>();
                return sm.PlayListMaps.AsReadOnly();
            }
        }

        /// <summary>既にプレイしたマップ名一覧</summary>
        public static IReadOnlyCollection<string> PlayedMaps
        {
            get
            {
                var sm = SceneMotor.Instance;
                if (sm?.PlayedMaps == null) return new List<string>();
                return sm.PlayedMaps;
            }
        }
    }
}
