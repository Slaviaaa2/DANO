using DANO.Events;
using HarmonyLib;

namespace DANO.Patches
{
    /// <summary>PauseManager.InvokeBeforeSpawn をフックしてスポーンフェーズイベントを発火する</summary>
    [HarmonyPatch(typeof(PauseManager), nameof(PauseManager.InvokeBeforeSpawn))]
    internal static class SpawnPhasePatch
    {
        private static void Postfix()
        {
            EventBus.Raise(new SpawnPhaseStartedEvent());
        }
    }
}
