using HarmonyLib;
using LobbyControl.Components;

namespace LobbyControl.Patches
{
    [HarmonyPatch]
    internal class OutOfBoundsPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Awake))]
        [HarmonyPriority(Priority.First)]
        private static void AddComponent(StartOfRound __instance)
        {
            if (!LobbyControl.PluginConfig.OutOfBounds.Enabled.Value)
                return;

            foreach (var item in __instance.allItemsList.itemsList)
            {
                if (item.spawnPrefab is null)
                    continue;

                if (!item.spawnPrefab.GetComponent<LCOutOfBounds>())
                    item.spawnPrefab.AddComponent<LCOutOfBounds>();
            }
            
        }

    }
}