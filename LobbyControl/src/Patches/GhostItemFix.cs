using System;
using System.Collections;
using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace LobbyControl.Patches
{
    [HarmonyPatch]
    internal class GhostItemFix
    {
        [HarmonyFinalizer]
        [HarmonyPatch(typeof(PlayerControllerB),nameof(PlayerControllerB.GrabObjectClientRpc))]
        private static Exception CatchGhostItemCreation(Exception __exception, PlayerControllerB __instance, NetworkObjectReference grabbedObject)
        {
            
            if (!LobbyControl.PluginConfig.GhostItems.Enabled.Value)
                return __exception;
            try
            {
                if (StartOfRound.Instance.IsServer && __exception != null)
                {
                    //if this did generate a ghost item force the attempting player to drop all held items :smirk:
                    __instance.StartCoroutine(DropAll(__instance));
                    return null;
                }
            }
            catch (Exception ex)
            {
                LobbyControl.Log.LogError($"Exception while dropping Items {ex}");
            }

            return __exception;
        }

        private static IEnumerator DropAll(PlayerControllerB playerControllerB)
        {
            yield return new WaitForSeconds(1);
            playerControllerB.DropAllHeldItemsServerRpc();
            HUDManager.Instance.AddTextToChatOnServer(
                $"{playerControllerB.playerUsername} was forced to drop all Items!!");
        }
    }
}