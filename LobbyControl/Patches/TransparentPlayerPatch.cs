using System.Collections.Generic;
using System.Reflection;
using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace LobbyControl.Patches
{
    [HarmonyPatch]
    internal class TransparentPlayerPatch
    {
        private static readonly MethodInfo StartOfRoundBeginSendClientRpc = typeof(StartOfRound).GetMethod(nameof(StartOfRound.__beginSendClientRpc), BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly MethodInfo StartOfRoundEndSendClientRpc = typeof(StartOfRound).GetMethod(nameof(StartOfRound.__endSendClientRpc), BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly MethodInfo PlayerControllerBBeginSendClientRpc = typeof(PlayerControllerB).GetMethod(nameof(PlayerControllerB.__beginSendClientRpc), BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly MethodInfo PlayerControllerBEndSendClientRpc = typeof(PlayerControllerB).GetMethod(nameof(PlayerControllerB.__endSendClientRpc), BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly Dictionary<ulong, int> ToRespawn = new Dictionary<ulong, int>();
        private static readonly Dictionary<ulong, int> ToDisconnect = new Dictionary<ulong, int>();
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.OnPlayerDC))]
        [HarmonyPriority(Priority.First)]
        private static void OnPlayerDCPatch(StartOfRound __instance, int playerObjectNumber, ulong clientId)
        {
            if (!LobbyControl.PluginConfig.InvisiblePlayer.Enabled.Value)
                return;

            PlayerControllerB controller = __instance.allPlayerScripts[playerObjectNumber];

            if (controller.isPlayerDead)
            {
                if (__instance.IsServer)
                {
                    ToRespawn[clientId] = playerObjectNumber;
                }
                
                controller.DisablePlayerModel(controller.gameObject, true, true);
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.unloadSceneForAllPlayers))]
        private static void RespawnDcPlayer(StartOfRound __instance)
        {
            if (!LobbyControl.PluginConfig.InvisiblePlayer.Enabled.Value)
                return;
            
            if (!__instance.IsServer || ToRespawn.Count == 0)
                return;
            
            List<ulong> ulongList = new List<ulong>();
            List<ulong> rpcList = new List<ulong>();
            for (int index = 0; index < __instance.allPlayerObjects.Length; ++index)
            {
                NetworkObject component = __instance.allPlayerObjects[index].GetComponent<NetworkObject>();
                if (ToRespawn.TryGetValue(component.OwnerClientId, out var playerObjectIndex) && playerObjectIndex != -1)
                    ulongList.Add(component.OwnerClientId);
                else if (!component.IsOwnedByServer)
                {
                    ulongList.Add(component.OwnerClientId);
                    rpcList.Add(component.OwnerClientId);
                }
                else if (index == 0)
                    ulongList.Add(NetworkManager.Singleton.LocalClientId);
                else
                    ulongList.Add(999UL);
            }
            
            ClientRpcParams clientRpcParams = new ClientRpcParams() {
                Send = new ClientRpcSendParams() {
                    TargetClientIds = rpcList,
                },
            };
            
            var serverMoneyAmount = UnityEngine.Object.FindObjectOfType<Terminal>().groupCredits;
            var profitQuota = TimeOfDay.Instance.profitQuota;
            var quotaFulfilled = TimeOfDay.Instance.quotaFulfilled;
            var timeUntilDeadline = (int) TimeOfDay.Instance.timeUntilDeadline;
            var levelID = __instance.currentLevelID;
            var connectedPlayers = __instance.connectedPlayersAmount;
            var randomSeed = __instance.randomMapSeed;
            var isChallenge = __instance.isChallengeFile;
            
            int count = 1;
            foreach (var dcPlayer in new Dictionary<ulong,int>(ToRespawn))
            {
                
                //client Join
                FastBufferWriter bufferWriter = (FastBufferWriter)StartOfRoundBeginSendClientRpc.Invoke(__instance, new object[]{886676601U, clientRpcParams, RpcDelivery.Reliable});
                BytePacker.WriteValueBitPacked(bufferWriter, dcPlayer.Key);
                BytePacker.WriteValueBitPacked(bufferWriter, connectedPlayers + count);
                bufferWriter.WriteValueSafe<bool>(true);
                bufferWriter.WriteValueSafe<ulong>(ulongList.ToArray());
                BytePacker.WriteValueBitPacked(bufferWriter, dcPlayer.Value);
                BytePacker.WriteValueBitPacked(bufferWriter, serverMoneyAmount);
                BytePacker.WriteValueBitPacked(bufferWriter, levelID);
                BytePacker.WriteValueBitPacked(bufferWriter, profitQuota);
                BytePacker.WriteValueBitPacked(bufferWriter, timeUntilDeadline);
                BytePacker.WriteValueBitPacked(bufferWriter, quotaFulfilled);
                BytePacker.WriteValueBitPacked(bufferWriter, randomSeed);
                bufferWriter.WriteValueSafe<bool>(in isChallenge);
                StartOfRoundEndSendClientRpc.Invoke(__instance, new object[]{bufferWriter, 886676601U, clientRpcParams, RpcDelivery.Reliable});
                
                //Client Kill
                PlayerControllerB controller = __instance.allPlayerScripts[dcPlayer.Value];
                bufferWriter = (FastBufferWriter)PlayerControllerBBeginSendClientRpc.Invoke(controller, new object[]{168339603U, clientRpcParams, RpcDelivery.Reliable});
                BytePacker.WriteValueBitPacked(bufferWriter, dcPlayer.Value);
                bufferWriter.WriteValueSafe<bool>(false);
                bufferWriter.WriteValueSafe(Vector3.zero);
                BytePacker.WriteValueBitPacked(bufferWriter, (int)CauseOfDeath.Kicking);
                BytePacker.WriteValueBitPacked(bufferWriter, 0);
                PlayerControllerBEndSendClientRpc.Invoke(controller, new object[]{bufferWriter, 168339603U, clientRpcParams, RpcDelivery.Reliable});
                
                ToDisconnect[dcPlayer.Key] = dcPlayer.Value;
            }
            
            ToRespawn.Clear();
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.EndOfGame))]
        [HarmonyPriority(Priority.VeryLow)]
        private static void ClearDcPlayer(StartOfRound __instance, bool __runOriginal)
        {
            if (!LobbyControl.PluginConfig.InvisiblePlayer.Enabled.Value)
                return;
            
            if (!__runOriginal || !__instance.IsServer || ToDisconnect.Count == 0) 
                return;
            
            List<ulong> ulongList = new List<ulong>();
            
            foreach (KeyValuePair<ulong, int> clientPlayer in __instance.ClientPlayerList)
            {
                if (clientPlayer.Key != __instance.localPlayerController.actualClientId )
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
                //__instance.OnClientDisconnectClientRpc(player.Value, player.Key, clientRpcParams);
                FastBufferWriter bufferWriter = (FastBufferWriter)StartOfRoundBeginSendClientRpc.Invoke(__instance, new object[]{475465488U, clientRpcParams, RpcDelivery.Reliable});
                BytePacker.WriteValueBitPacked(bufferWriter, player.Value);
                BytePacker.WriteValueBitPacked(bufferWriter, player.Key);
                StartOfRoundEndSendClientRpc.Invoke(__instance, new object[]{bufferWriter, 475465488U, clientRpcParams, RpcDelivery.Reliable});
            }
            
            ToDisconnect.Clear();
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Awake))]
        private static void ClearOnBoot(StartOfRound __instance)
        {
            if (!__instance.IsServer)
                return;
            
            ToRespawn.Clear();
            ToDisconnect.Clear();
        }
    }
}