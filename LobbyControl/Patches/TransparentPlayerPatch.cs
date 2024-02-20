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
            
            List<int> toRemove = new List<int>();
            foreach (var dcPlayer in TrackedPlayers)
            {
                if (!__instance.allPlayerScripts[dcPlayer.Key].IsOwnedByServer)
                {
                    toRemove.Add(dcPlayer.Key);
                }
                __instance.allPlayerScripts[dcPlayer.Key].KillPlayerClientRpc(dcPlayer.Key,false, Vector3.zero, (int)CauseOfDeath.Kicking, 0);
            }

            foreach (var key in toRemove)
            {
                TrackedPlayers.Remove(key);
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