using DANO.Events;
using HarmonyLib;

namespace DANO.Patches
{
    /// <summary>GameManager.StartGame をフックしてゲーム開始イベントを発火する</summary>
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.StartGame))]
    internal static class GameStartPatch
    {
        private static bool _logged;

        private static void Postfix()
        {
            if (!_logged)
            {
                _logged = true;
                DANOLoader.Log.LogInfo("[GameStartPatch] Postfix 初回発火確認！");
            }
            EventBus.Raise(new GameStartedEvent());
        }
    }
}
