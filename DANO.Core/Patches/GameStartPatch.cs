using DANO.Events;
using HarmonyLib;

namespace DANO.Patches
{
    /// <summary>GameManager.StartGame をフックしてゲーム開始イベントを発火する</summary>
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.StartGame))]
    internal static class GameStartPatch
    {
        private static void Postfix()
        {
            EventBus.Raise(new GameStartedEvent());
        }
    }
}
