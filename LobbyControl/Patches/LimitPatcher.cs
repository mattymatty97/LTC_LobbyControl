using System;
using System.Collections.Generic;
using HarmonyLib;

namespace LobbyControl.Patches
{
    [HarmonyPatch]
    internal class LimitPatcher
    {
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SyncShipUnlockablesServerRpc))]
        private static IEnumerable<CodeInstruction> SyncUnlockablesPatch(IEnumerable<CodeInstruction> instructions)
        {
            if (!LobbyControl.PluginConfig.SaveLimit.Enabled.Value)
                return instructions;
            
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            var found = false;
            try
            {
                for (var i = 0; i < codes.Count; i++)
                {
                    var curr = codes[i];
                    if (curr.LoadsConstant(250))
                    {
                        curr.operand = int.MaxValue;
                        found = true;
                    }
                }

                if (!found)
                    LobbyControl.Log.LogError(nameof(SyncUnlockablesPatch) + "failed to Patch");
                else
                    LobbyControl.Log.LogWarning(nameof(SyncUnlockablesPatch) + "successfully Patched");
            }
            catch (Exception ex)
            {
                LobbyControl.Log.LogError("Exception patching " + nameof(SyncUnlockablesPatch) + "\n" + ex);
            }

            return codes;
        }

        [HarmonyPrefix]
        [HarmonyPriority(0)]
        [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.SaveItemsInShip))]
        private static void SaveItemsInShipPatch(GameNetworkManager __instance)
        { 
            if (!LobbyControl.PluginConfig.SaveLimit.Enabled.Value)
                return;
            
            StartOfRound.Instance.maxShipItemCapacity = int.MaxValue;
        }

    }
}