using System.Collections.Generic;
using System.Reflection.Emit;
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

            for (var i = 0; i < codes.Count; i++)
            {
                var curr = codes[i];
                if (curr.LoadsConstant(250))
                {
                    var next = codes[i + 1];
                    var prev = codes[i - 1];
                    if (next.Branches(out Label? dest))
                    {
                        codes[i - 1] = new CodeInstruction(OpCodes.Nop)
                        {
                            labels = prev.labels,
                            blocks = prev.blocks
                        };
                        codes[i] = new CodeInstruction(OpCodes.Nop)
                        {
                            labels = curr.labels,
                            blocks = curr.blocks
                        };
                        codes[i] = new CodeInstruction(OpCodes.Br_S, dest)
                        {
                            labels = next.labels,
                            blocks = next.blocks
                        };
                        LobbyControl.Log.LogDebug("Patched SyncShipUnlockablesServerRpc!!");
                        break;
                    }
                }
            }

            return codes;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.SaveItemsInShip))]
        private static IEnumerable<CodeInstruction> SaveItemsInShipPatch(IEnumerable<CodeInstruction> instructions)
        {
            if (!LobbyControl.PluginConfig.SaveLimit.Enabled.Value)
                return instructions;

            var fieldInfo = typeof(StartOfRound).GetField(nameof(StartOfRound.maxShipItemCapacity));

            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);

            for (var i = 0; i < codes.Count; i++)
            {
                var curr = codes[i];
                if (curr.LoadsField(fieldInfo))
                {
                    var next = codes[i + 1];
                    if (next.Branches(out Label? dest))
                    {
                        codes[i - 2] = new CodeInstruction(OpCodes.Nop)
                        {
                            labels = codes[i - 2].labels,
                            blocks = codes[i - 2].blocks
                        };
                        codes[i - 1] = new CodeInstruction(OpCodes.Nop)
                        {
                            labels = codes[i - 1].labels,
                            blocks = codes[i - 1].blocks
                        };
                        codes[i] = new CodeInstruction(OpCodes.Nop)
                        {
                            labels = codes[i].labels,
                            blocks = codes[i].blocks
                        };
                        codes[i + 1] = new CodeInstruction(OpCodes.Nop)
                        {
                            labels = codes[i + 1].labels,
                            blocks = codes[i + 1].blocks
                        };
                        LobbyControl.Log.LogDebug("Patched SaveItemsInShip!!");
                        break;
                    }
                }
            }

            return codes;
        }
    }
}