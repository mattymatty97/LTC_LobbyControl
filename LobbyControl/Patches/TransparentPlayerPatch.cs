using System.Collections.Generic;
using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace LobbyControl.Patches
{
    [HarmonyPatch]
    internal class TransparentPlayerPatch
    {
        private static readonly Dictionary<ulong, int> ToRespawn = new Dictionary<ulong, int>();
        private static readonly Dictionary<ulong, int> ToDisconnect = new Dictionary<ulong, int>();
        private static bool _alreadyReconnected = false;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.OnClientDisconnect))]
        private static void TrackDc(StartOfRound __instance, ulong clientId)
        {
            if (!LobbyControl.PluginConfig.InvisiblePlayer.Enabled.Value)
                return;
            
            if (__instance.IsServer && !__instance.inShipPhase && !ToRespawn.ContainsKey(clientId) && clientId != __instance.localPlayerController.playerClientId)
                ToRespawn.Add(clientId, -1);
        }
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.OnPlayerDC))]
        [HarmonyPriority(Priority.First)]
        private static bool OnPlayerDCPatch(StartOfRound __instance, int playerObjectNumber, ulong clientId)
        {
            if (!LobbyControl.PluginConfig.InvisiblePlayer.Enabled.Value)
                return true;
            
            if (!__instance.IsServer || __instance.inShipPhase || !ToRespawn.ContainsKey(clientId))
                return true;

            PlayerControllerB controller = __instance.allPlayerScripts[playerObjectNumber];

            if (controller.isPlayerDead)
            {
                ToRespawn[clientId] = playerObjectNumber;
                if (_alreadyReconnected)
                    ToDisconnect[clientId] = playerObjectNumber;
                return !_alreadyReconnected;
            }

            return true;
        }        
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.OnClientDisconnectClientRpc))]
        [HarmonyPriority(Priority.First)]
        private static bool OnClientDCPatch(StartOfRound __instance, int playerObjectNumber, ulong clientId)
        {
            if (!LobbyControl.PluginConfig.InvisiblePlayer.Enabled.Value)
                return true;
            
            if (!__instance.IsServer || __instance.inShipPhase || !ToRespawn.ContainsKey(clientId))
                return true;

            PlayerControllerB controller = __instance.allPlayerScripts[playerObjectNumber];

            if (controller.isPlayerDead)
            {
                ToRespawn[clientId] = playerObjectNumber;
                if (_alreadyReconnected)
                    ToDisconnect[clientId] = playerObjectNumber;
                return !_alreadyReconnected;
            }

            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.EndOfGameClientRpc))]
        private static void RespawnDcPlayer(StartOfRound __instance)
        {
            if (!LobbyControl.PluginConfig.InvisiblePlayer.Enabled.Value)
                return;
            
            if (!__instance.IsServer || ToRespawn.Count <= 0)
                return;
            
            List<ulong> ulongList = new List<ulong>();
            for (int index = 0; index < __instance.allPlayerObjects.Length; ++index)
            {
                NetworkObject component = __instance.allPlayerObjects[index].GetComponent<NetworkObject>();
                if (ToRespawn.TryGetValue(component.OwnerClientId, out var playerObjectIndex) && playerObjectIndex != -1)
                    ulongList.Add(999UL);
                else if (!component.IsOwnedByServer)
                    ulongList.Add(component.OwnerClientId);
                else if (index == 0)
                    ulongList.Add(NetworkManager.Singleton.LocalClientId);
                else
                    ulongList.Add(999UL);
            }
            
            var groupCredits = UnityEngine.Object.FindObjectOfType<Terminal>().groupCredits;
            var profitQuota = TimeOfDay.Instance.profitQuota;
            var quotaFulfilled = TimeOfDay.Instance.quotaFulfilled;
            var timeUntilDeadline = (int) TimeOfDay.Instance.timeUntilDeadline;
            foreach (var dcPlayer in new Dictionary<ulong,int>(ToRespawn))
            {
                if (dcPlayer.Value == -1)
                    continue;
                
                __instance.OnPlayerConnectedClientRpc(dcPlayer.Key, __instance.connectedPlayersAmount, ulongList.ToArray(), 
                    dcPlayer.Value, groupCredits, __instance.currentLevelID, profitQuota, 
                    timeUntilDeadline, quotaFulfilled, __instance.randomMapSeed, __instance.isChallengeFile);
                
                __instance.allPlayerScripts[dcPlayer.Value].KillPlayerClientRpc(dcPlayer.Value,false, Vector3.zero, (int)CauseOfDeath.Kicking, 0);
                
                ToDisconnect[dcPlayer.Key] = dcPlayer.Value;
            }
            _alreadyReconnected = true;
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.EndOfGame))]
        [HarmonyPriority(Priority.VeryLow)]
        private static void ClearDcPlayer(StartOfRound __instance, bool __runOriginal)
        {
            if (!LobbyControl.PluginConfig.InvisiblePlayer.Enabled.Value)
                return;
            
            if (!__runOriginal || !__instance.IsServer || ToDisconnect.Count <= 0) 
                return;
            
            _alreadyReconnected = false;
            
            List<ulong> ulongList = new List<ulong>();
            foreach (KeyValuePair<ulong, int> clientPlayer in __instance.ClientPlayerList)
            {
                if (!ToDisconnect.TryGetValue(clientPlayer.Key, out var playerObjectIndex) || playerObjectIndex == -1)
                    ulongList.Add(clientPlayer.Key);
            }
            ClientRpcParams clientRpcParams = new ClientRpcParams()
            {
                Send = new ClientRpcSendParams()
                {
                    TargetClientIds = ulongList.ToArray()
                }
            };
            foreach (var player in new Dictionary<ulong,int>(ToDisconnect))
            {
                __instance.OnPlayerDC(player.Value, player.Key);
                __instance.OnClientDisconnectClientRpc(player.Value, player.Key, clientRpcParams);
            }
            
            ToRespawn.Clear();
            ToDisconnect.Clear();
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.StartGame))]
        private static void ClearOnStart(StartOfRound __instance)
        {
            if (!__instance.IsServer)
                return;
            
            ToRespawn.Clear();
            ToDisconnect.Clear();
            _alreadyReconnected = false;
        }
    }
}