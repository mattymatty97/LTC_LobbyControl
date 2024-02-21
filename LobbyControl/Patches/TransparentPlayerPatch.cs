using System.Collections.Generic;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace LobbyControl.Patches
{
    [HarmonyPatch]
    internal class TransparentPlayerPatch
    {
        private static readonly Dictionary<int, ulong> TrackedPlayers = new Dictionary<int, ulong>();

        [HarmonyPrefix]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.OnPlayerDC))]
        private static void TrackDc(StartOfRound __instance, int playerObjectNumber, ulong clientId)
        {
            if (__instance.IsServer && !__instance.inShipPhase &&
                __instance.allPlayerScripts[playerObjectNumber].isPlayerDead && clientId != __instance.localPlayerController.playerClientId)
                TrackedPlayers[playerObjectNumber] = clientId;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.ShipHasLeft))]
        private static void KillDcPlayer(StartOfRound __instance)
        {
            if (!__instance.IsServer || TrackedPlayers.Count <= 0)
                return;
            
            List<ulong> ulongList = new List<ulong>();
            for (int index = 0; index < __instance.allPlayerObjects.Length; ++index)
            {
                NetworkObject component = __instance.allPlayerObjects[index].GetComponent<NetworkObject>();
                if (!component.IsOwnedByServer)
                    ulongList.Add(component.OwnerClientId);
                else if (index == 0)
                    ulongList.Add(NetworkManager.Singleton.LocalClientId);
                else if (TrackedPlayers.TryGetValue(index, out ulong clientID))
                    ulongList.Add(clientID);
                else
                    ulongList.Add(999UL);
            }
            
            int groupCredits = UnityEngine.Object.FindObjectOfType<Terminal>().groupCredits;
            int profitQuota = TimeOfDay.Instance.profitQuota;
            int quotaFulfilled = TimeOfDay.Instance.quotaFulfilled;
            int timeUntilDeadline = (int) TimeOfDay.Instance.timeUntilDeadline;
            foreach (var dcPlayer in TrackedPlayers)
            {
                __instance.OnPlayerConnectedClientRpc(dcPlayer.Value, __instance.connectedPlayersAmount, ulongList.ToArray(), 
                    dcPlayer.Key, groupCredits, __instance.currentLevelID, profitQuota, 
                    timeUntilDeadline, quotaFulfilled, __instance.randomMapSeed, __instance.isChallengeFile);
                __instance.allPlayerScripts[dcPlayer.Key].KillPlayerClientRpc(dcPlayer.Key,false, Vector3.zero, (int)CauseOfDeath.Kicking, 0);
            }
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.EndOfGame))]
        private static void clearDCPlayer(StartOfRound __instance)
        {
            if (!__instance.IsServer || TrackedPlayers.Count <= 0) 
                return;
            
            foreach (KeyValuePair<int, ulong> dcPlayer in TrackedPlayers)
            {
                __instance.OnClientDisconnectClientRpc(dcPlayer.Key, dcPlayer.Value);
            }
            
            TrackedPlayers.Clear();
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.StartGame))]
        private static void ClearOnStart(StartOfRound __instance)
        {
            if (!__instance.IsServer)
                return;
            
            TrackedPlayers.Clear();
        }
    }
}