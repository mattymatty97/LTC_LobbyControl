using System.Collections.Generic;
using HarmonyLib;
using Unity.Netcode;

namespace LobbyControl.Patches
{
    [HarmonyPatch]
    internal class TransparentPlayerPatch
    {

        private static readonly Dictionary<int,ulong> TrackedPlayers = new Dictionary<int,ulong>();
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.OnPlayerDC))]
        private static void TrackDC(StartOfRound __instance, int playerObjectNumber, ulong clientId)
        {
            if (__instance.IsServer && !__instance.inShipPhase && __instance.allPlayerScripts[playerObjectNumber].isPlayerDead)
            {
                TrackedPlayers[playerObjectNumber] = clientId;
            }
        }
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.ShipHasLeft))]
        private static void respawnDCPlayer(StartOfRound __instance)
        {
            if (__instance.IsServer && TrackedPlayers.Count > 0)
            {            
                StartOfRound startOfRound = __instance;
                List<ulong> ulongList = new List<ulong>();
                for (var index = 0; index < startOfRound.allPlayerObjects.Length; ++index)
                {
                    NetworkObject component = startOfRound.allPlayerObjects[index].GetComponent<NetworkObject>();
                    if (!component.IsOwnedByServer)
                        ulongList.Add(component.OwnerClientId);
                    else if (index == 0)
                        ulongList.Add(NetworkManager.Singleton.LocalClientId);
                    else
                        ulongList.Add(999UL);
                }
                
                var groupCredits = UnityEngine.Object.FindObjectOfType<Terminal>().groupCredits;
                var profitQuota = TimeOfDay.Instance.profitQuota;
                var quotaFulfilled = TimeOfDay.Instance.quotaFulfilled;
                var timeUntilDeadline = (int)TimeOfDay.Instance.timeUntilDeadline;
                foreach (var dcPlayer in TrackedPlayers)
                {
                    startOfRound.OnPlayerConnectedClientRpc(dcPlayer.Value, startOfRound.connectedPlayersAmount - 1,
                        ulongList.ToArray(), dcPlayer.Key, groupCredits,
                        startOfRound.currentLevelID, profitQuota, timeUntilDeadline, quotaFulfilled,
                        startOfRound.randomMapSeed, startOfRound.isChallengeFile);
                }
            }
        }        
        
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.EndOfGame))]
        private static void clearDCPlayer(StartOfRound __instance)
        {
            if (__instance.IsServer && TrackedPlayers.Count > 0)
            {            
                foreach (var dcPlayer in TrackedPlayers)
                {
                    __instance.OnClientDisconnectClientRpc(dcPlayer.Key, dcPlayer.Value);
                }
            }
        }
        
        
    }
}