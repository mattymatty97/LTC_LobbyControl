using System;
using System.Collections.Generic;
using System.Reflection;
using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LobbyControl.Patches
{
    [HarmonyPatch]
    internal class TransparentPlayerFix
    {
        private static readonly Dictionary<ulong, int> ToRespawn = new Dictionary<ulong, int>();
        private static readonly Dictionary<ulong, int> ToDisconnect = new Dictionary<ulong, int>();

        private static bool _isRespawning;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.OnPlayerDC))]
        [HarmonyPriority(Priority.First)]
        private static void OnPlayerDCPatch(StartOfRound __instance, int playerObjectNumber, ulong clientId)
        {
            if (!LobbyControl.PluginConfig.InvisiblePlayer.Enabled.Value)
                return;

            var controller = __instance.allPlayerScripts[playerObjectNumber];

            if (controller.isPlayerDead)
            {
                LobbyControl.Log.LogWarning($"Player {controller.playerUsername} disconnected while Dead!");
                if (__instance.IsServer)
                {
                    LobbyControl.Log.LogInfo($"Player {controller.playerUsername} added to the list of Respawnables");
                    ToRespawn[clientId] = playerObjectNumber;
                }

                LobbyControl.Log.LogDebug($"Model of player {controller.playerUsername} has been re-enabled");
                controller.DisablePlayerModel(controller.gameObject, true, true);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.OnClientDisconnectClientRpc))]
        private static bool preventDc(StartOfRound __instance, int playerObjectNumber, ulong clientId)
        {
            if (!__instance.IsServer || !LobbyControl.PluginConfig.InvisiblePlayer.Enabled.Value)
                return true;

            ToDisconnect[clientId] = playerObjectNumber;

            return !_isRespawning;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.StartGame))]
        private static void MemoizeDeadline(StartOfRound __instance)
        {
            _timeUntilDeadline = (int)TimeOfDay.Instance.timeUntilDeadline;
        }

        private static int _timeUntilDeadline = 3;
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.unloadSceneForAllPlayers))]
        private static void RespawnDcPlayer(StartOfRound __instance)
        {
            if (!LobbyControl.PluginConfig.InvisiblePlayer.Enabled.Value)
                return;

            if (!__instance.IsServer || ToRespawn.Count == 0)
                return;

            try
            {
                _isRespawning = true;
                List<ulong> ulongList = new List<ulong>();
                List<ulong> rpcList = new List<ulong>();
                for (var index = 0; index < __instance.allPlayerObjects.Length; ++index)
                {
                    var component = __instance.allPlayerObjects[index].GetComponent<NetworkObject>();
                    if (ToRespawn.TryGetValue(component.OwnerClientId, out var playerObjectIndex) &&
                        playerObjectIndex != -1)
                    {
                        ulongList.Add(component.OwnerClientId);
                    }
                    else if (!component.IsOwnedByServer)
                    {
                        ulongList.Add(component.OwnerClientId);
                        rpcList.Add(component.OwnerClientId);
                    }
                    else if (index == 0)
                    {
                        ulongList.Add(NetworkManager.Singleton.LocalClientId);
                    }
                    else
                    {
                        ulongList.Add(999UL);
                    }
                }

                var clientRpcParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = rpcList
                    }
                };

                var serverMoneyAmount = Object.FindObjectOfType<Terminal>().groupCredits;
                var profitQuota = TimeOfDay.Instance.profitQuota;
                var quotaFulfilled = TimeOfDay.Instance.quotaFulfilled;
                var timeUntilDeadline = _timeUntilDeadline;
                var levelID = __instance.currentLevelID;
                var connectedPlayers = __instance.connectedPlayersAmount;
                var randomSeed = __instance.randomMapSeed;
                var isChallenge = __instance.isChallengeFile;

                var count = 0;
                foreach (KeyValuePair<ulong, int> dcPlayer in new Dictionary<ulong, int>(ToRespawn))
                {
                    var controller = __instance.allPlayerScripts[dcPlayer.Value];
                    //client Join
                    var bufferWriter = __instance.__beginSendClientRpc(886676601U, clientRpcParams, RpcDelivery.Reliable);
                    BytePacker.WriteValueBitPacked(bufferWriter, dcPlayer.Key);
                    BytePacker.WriteValueBitPacked(bufferWriter, connectedPlayers + count);
                    bufferWriter.WriteValueSafe(true);
                    bufferWriter.WriteValueSafe(ulongList.ToArray());
                    BytePacker.WriteValueBitPacked(bufferWriter, dcPlayer.Value);
                    BytePacker.WriteValueBitPacked(bufferWriter, serverMoneyAmount);
                    BytePacker.WriteValueBitPacked(bufferWriter, levelID);
                    BytePacker.WriteValueBitPacked(bufferWriter, profitQuota);
                    BytePacker.WriteValueBitPacked(bufferWriter, timeUntilDeadline);
                    BytePacker.WriteValueBitPacked(bufferWriter, quotaFulfilled);
                    BytePacker.WriteValueBitPacked(bufferWriter, randomSeed);
                    bufferWriter.WriteValueSafe(in isChallenge);
                    __instance.__endSendClientRpc(ref bufferWriter, 886676601U, clientRpcParams, RpcDelivery.Reliable);
                    LobbyControl.Log.LogInfo($"Player {controller.playerUsername} has been reconnected by host");

                    //Client Kill
                    bufferWriter = controller.__beginSendClientRpc(168339603U, clientRpcParams, RpcDelivery.Reliable);
                    BytePacker.WriteValueBitPacked(bufferWriter, dcPlayer.Value);
                    bufferWriter.WriteValueSafe(false);
                    bufferWriter.WriteValueSafe(Vector3.zero);
                    BytePacker.WriteValueBitPacked(bufferWriter, (int)CauseOfDeath.Kicking);
                    BytePacker.WriteValueBitPacked(bufferWriter, 0);
                    controller.__endSendClientRpc(ref bufferWriter, 168339603U, clientRpcParams, RpcDelivery.Reliable);
                    LobbyControl.Log.LogInfo($"Player {controller.playerUsername} has been killed by host");

                    ToDisconnect[dcPlayer.Key] = dcPlayer.Value;
                }
            }
            catch (Exception ex)
            {
                LobbyControl.Log.LogError($"Exception while respawning dead players {ex}");
            }

            ToRespawn.Clear();
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.__rpc_handler_3083945322))]
        private static void ClearRespawningFlag()
        {
            _isRespawning = false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.__rpc_handler_3083945322))]
        private static void ClearDcPlayerForClient(
            NetworkBehaviour target,
            __RpcParams rpcParams)
        {
            if (!LobbyControl.PluginConfig.InvisiblePlayer.Enabled.Value)
                return;
            var startOfRound = (StartOfRound)target;
            var networkManager = target.NetworkManager;
            if (networkManager == null || !networkManager.IsListening)
                return;

            if (!startOfRound.IsServer || ToDisconnect.Count == 0)
                return;

            try
            {
                var clientRpcParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new[] { rpcParams.Server.Receive.SenderClientId }
                    }
                };

                var client =
                    startOfRound.allPlayerScripts[
                        startOfRound.ClientPlayerList[rpcParams.Server.Receive.SenderClientId]];

                foreach (KeyValuePair<ulong, int> player in new Dictionary<ulong, int>(ToDisconnect))
                {
                    var controller = startOfRound.allPlayerScripts[player.Value];
                    startOfRound.OnClientDisconnectClientRpc(player.Value, player.Key, clientRpcParams);
                    LobbyControl.Log.LogInfo(
                        $"Player {controller.playerUsername} has been removed and should be visible to {client.playerUsername}");
                }
            }
            catch (Exception ex)
            {
                LobbyControl.Log.LogError($"Exception while disconnecting dead players {ex}");
            }

            _isRespawning = false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Awake))]
        private static void ClearOnBoot(StartOfRound __instance)
        {
            if (!__instance.IsServer)
                return;

            _isRespawning = false;
            ToRespawn.Clear();
            ToDisconnect.Clear();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.StartGame))]
        private static void ClearOnLand(StartOfRound __instance)
        {
            if (!__instance.IsServer)
                return;

            _isRespawning = false;
            ToRespawn.Clear();
            ToDisconnect.Clear();
        }
    }
}