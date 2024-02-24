using System;
using GameNetcodeStuff;
using HarmonyLib;

namespace LobbyControl.Patches
{
    [HarmonyPatch]
    internal class GhostItemFix
    {
        [HarmonyFinalizer]
        [HarmonyPatch(typeof(PlayerControllerB),nameof(PlayerControllerB.GrabObjectClientRpc))]
        private static Exception CatchGhostItemCreation(Exception __exception, PlayerControllerB __instance)
        {
            if (StartOfRound.Instance.IsServer && __exception is IndexOutOfRangeException)
            {
                //if this did generate a ghost item force the attempting player to drop all held items :smirk:
                __instance.DropAllHeldItemsServerRpc();
                HUDManager.Instance.AddTextToChatOnServer($"{__instance.playerUsername} was forced to drop all Items!!");
                return null;
            }

            return __exception;
        }
    }
}