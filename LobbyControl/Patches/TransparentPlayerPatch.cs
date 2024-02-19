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
                __instance.allPlayerScripts[playerObjectNumber].isPlayerDead)
                TrackedPlayers[playerObjectNumber] = clientId;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.ShipHasLeft))]
        private static void RespawnDcPlayer(StartOfRound __instance)
        {
            if (!__instance.IsServer || TrackedPlayers.Count <= 0)
                return;

            List<ulong> ulongList = new List<ulong>();
            for (var index = 0; index < __instance.allPlayerObjects.Length; ++index)
            {
                var component = __instance.allPlayerObjects[index].GetComponent<NetworkObject>();
                if (!component.IsOwnedByServer)
                    ulongList.Add(component.OwnerClientId);
                else if (index == 0)
                    ulongList.Add(NetworkManager.Singleton.LocalClientId);
                else
                    ulongList.Add(999UL);
            }

            var groupCredits = Object.FindObjectOfType<Terminal>().groupCredits;
            var profitQuota = TimeOfDay.Instance.profitQuota;
            var quotaFulfilled = TimeOfDay.Instance.quotaFulfilled;
            var timeUntilDeadline = (int)TimeOfDay.Instance.timeUntilDeadline;
            foreach (KeyValuePair<int, ulong> dcPlayer in TrackedPlayers)
                __instance.OnPlayerConnectedClientRpc(dcPlayer.Value, __instance.connectedPlayersAmount - 1,
                    ulongList.ToArray(), dcPlayer.Key, groupCredits,
                    __instance.currentLevelID, profitQuota, timeUntilDeadline, quotaFulfilled,
                    __instance.randomMapSeed, __instance.isChallengeFile);
        }


        [HarmonyPostfix]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.EndOfGame))]
        private static void ClearDcPlayer(StartOfRound __instance)
        {
            if (!__instance.IsServer || TrackedPlayers.Count <= 0)
                return;

            foreach (KeyValuePair<int, ulong> dcPlayer in TrackedPlayers)
                __instance.OnClientDisconnectClientRpc(dcPlayer.Key, dcPlayer.Value);
            
            TrackedPlayers.Clear();
        }
    }
}