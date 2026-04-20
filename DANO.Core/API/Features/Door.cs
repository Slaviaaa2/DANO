using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DANO.API
{
    /// <summary>
    /// ドアのラッパー。ゲームの Door クラスを隠蔽する。
    /// </summary>
    public class Door
    {
        /// <summary>生の Door コンポーネント（上級者向け）</summary>
        public global::Door Base { get; }

        internal Door(global::Door door) { Base = door; }

        // ─── Static ルックアップ ───

        /// <summary>Door コンポーネントからラッパーを取得</summary>
        public static Door Get(global::Door door) => new Door(door);

        /// <summary>シーン内の全ドアを取得</summary>
        public static IEnumerable<Door> List =>
            Object.FindObjectsOfType<global::Door>().Select(d => new Door(d));

        // ─── プロパティ ───

        /// <summary>ドアが開いているかどうか</summary>
        public bool IsOpen => Base.sync___get_value_isOpen();

        /// <summary>ドアのワールド座標</summary>
        public Vector3 Position => Base.transform.position;

        /// <summary>ドア名（GameObject 名）</summary>
        public string Name => Base.gameObject.name;

        // ─── メソッド ───

        /// <summary>ドアを開閉トグルする</summary>
        public void Toggle(Player? interactor = null)
        {
            var transform = interactor?.Controller?.transform;
            Base.OnInteract(transform ?? Base.transform);
        }

        /// <summary>ドアを開く（既に開いている場合は何もしない）</summary>
        public void Open(Player? interactor = null)
        {
            if (!IsOpen)
                Toggle(interactor);
        }

        /// <summary>ドアを閉じる（既に閉じている場合は何もしない）</summary>
        public void Close(Player? interactor = null)
        {
            if (IsOpen)
                Toggle(interactor);
        }

        public override string ToString() => $"Door({Name}, {(IsOpen ? "Open" : "Closed")})";
    }
}
