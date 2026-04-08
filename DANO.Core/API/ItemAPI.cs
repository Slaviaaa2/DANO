using System.Collections.Generic;

namespace DANO.API
{
    /// <summary>アイテム・武器情報へのアクセスAPI</summary>
    public static class ItemAPI
    {
        /// <summary>シーン内の全アイテム</summary>
        public static IEnumerable<API.Item> GetAll() => API.Item.List;

        /// <summary>プレイヤーが手に持っているアイテムを返す</summary>
        public static API.Item? GetHeldItem(API.Player player, bool rightHand = true) =>
            player.GetHeldItem(rightHand);

        /// <summary>アイテムが誰かに持たれているかどうか</summary>
        public static bool IsHeld(API.Item item) => item.IsHeld;

        /// <summary>アイテムを持っているプレイヤーを返す</summary>
        public static API.Player? GetHolder(API.Item item) => item.Holder;
    }
}
