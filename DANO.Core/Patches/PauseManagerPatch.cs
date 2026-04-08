using DANO.Events;
using HarmonyLib;

namespace DANO.Patches
{
    /// <summary>PauseManager.InvokeBeforeSpawn をフックしてスポーンフェーズイベントを発火する</summary>
    [HarmonyPatch(typeof(PauseManager), nameof(PauseManager.InvokeBeforeSpawn))]
    internal static class SpawnPhasePatch
    {
        private static bool _logged;

        private static void Postfix()
        {
            if (!_logged)
            {
                _logged = true;
                DANOLoader.Log.LogInfo("[SpawnPhasePatch] Postfix 初回発火確認！");
            }
            EventBus.Raise(new SpawnPhaseStartedEvent());
        }
    }
}
