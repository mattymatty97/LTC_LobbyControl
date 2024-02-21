using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace LobbyControl.Patches
{
    [HarmonyPatch]
    internal class SavePatches
    {
        
        /// <summary>
        ///     Skip saving the lobby if AutoSave is of
        ///     Write the AutoSave status to the SaveFile
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.SaveGame))]
        private static bool PreventSave(GameNetworkManager __instance)
        {
            if (LobbyControl.CanSave)
                ES3.Save("LC_SavingMethod", LobbyControl.AutoSaveEnabled, __instance.currentSaveFileName);
            
            return LobbyControl.CanSave;
        }

        /// <summary>
        ///     Read the AutoSave status of the current File.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Start))]
        private static void ReadCustomLobbyStatus(StartOfRound __instance)
        {
            if (!__instance.IsServer) 
                return;
            
            LobbyControl.AutoSaveEnabled = LobbyControl.CanSave = ES3.Load("LC_SavingMethod",
                GameNetworkManager.Instance.currentSaveFileName, true);
        }
        
        /// <summary>
        ///     Update Item position on Clients after HotLoad
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.LoadShipGrabbableItems))]
        private static void MarkScrapAsDirty(StartOfRound __instance)
        {
            if (!__instance.IsServer) 
                return;
            
            GameObject ship = GameObject.Find("/Environment/HangarShip");
            foreach (var item in ship.GetComponentsInChildren<GrabbableObject>())
            {
                item.GetComponent<NetworkObject>().MarkVariablesDirty(true);
            }
            
            GameObject closet = GameObject.Find("/Environment/HangarShip/StorageCloset");
            foreach (var item in closet.GetComponentsInChildren<GrabbableObject>())
            {
                item.GetComponent<NetworkObject>().MarkVariablesDirty(true);
            }
        }
    }
}