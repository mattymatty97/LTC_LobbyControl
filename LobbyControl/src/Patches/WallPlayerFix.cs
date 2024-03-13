using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Unity.Netcode;

namespace LobbyControl.Patches
{
    [HarmonyPatch]
    internal class WallPlayerFix
    {
        [HarmonyPatch]
        internal class Transpliers
        {
            [HarmonyTranspiler]
            [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.Singleton_OnClientConnectedCallback))]
            private static IEnumerable<CodeInstruction> EnqueueConnection(IEnumerable<CodeInstruction> instructions)
            {
                var oldClientConnect =
                    typeof(StartOfRound).GetMethod(nameof(StartOfRound.OnClientConnect));
                var newClientConnect =
                    typeof(WallPlayerFix).GetMethod(nameof(WallPlayerFix.OnClientConnectWrapper), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance);

                if (oldClientConnect == null || newClientConnect == null)
                    return instructions;
            
                var codes = instructions.ToList();

                for (var i = 0; i < codes.Count; i++)
                {
                    var curr = codes[i];
                    if (curr.Calls(oldClientConnect))
                    {
                        codes[i] = new CodeInstruction(OpCodes.Callvirt, newClientConnect)
                        {
                            labels = codes[i].labels,
                            blocks = codes[i].blocks
                        };
                        LobbyControl.Log.LogDebug("Patched Singleton_OnClientConnectedCallback!!");
                        break;
                    }
                }

                return codes;
            }
        }

        private static readonly List<ulong> ConnectionQueue = new List<ulong>();
        private static ulong? _currentConnectingPlayer = null;
        private static ulong _currentConnectingExpiration = 0;
        
        private static void OnClientConnectWrapper(StartOfRound startOfRound, ulong playerUUID)
        {
            if (!startOfRound.IsServer || !LobbyControl.PluginConfig.JoinQueue.Enabled.Value)
                startOfRound.OnClientConnect(playerUUID);
            else
            {
                if (!ConnectionQueue.Contains(playerUUID))
                {
                    LobbyControl.Log.LogInfo($"{playerUUID} attempted connection, Enqueued!");
                    ConnectionQueue.Add(playerUUID);
                }
                else
                {
                    LobbyControl.Log.LogWarning($"{playerUUID} was already connecting, Kicking!");
                    NetworkManager.Singleton.DisconnectClient(playerUUID);
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.Singleton_OnClientDisconnectCallback))]
        private static void OnClientDisconnect(ulong clientId)
        {
            if (!StartOfRound.Instance.IsServer)
                return;
            
            LobbyControl.Log.LogInfo($"{clientId} disconnected");
            ConnectionQueue.Remove(clientId);
            if (_currentConnectingPlayer == clientId)
                _currentConnectingPlayer = null;
        }

        //SyncAlreadyHeldObjectsServerRpc
        [HarmonyPrefix]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.__rpc_handler_682230258))]
        private static void ClientConnectionCompleted(__RpcParams rpcParams)
        {
            if (!StartOfRound.Instance.IsServer)
                return;
            
            var clientId = rpcParams.Server.Receive.SenderClientId;
            if (ConnectionQueue.Remove(clientId))
                LobbyControl.Log.LogInfo($"{clientId} is now connected");
            else if (!LobbyControl.PluginConfig.JoinQueue.Enabled.Value)
                LobbyControl.Log.LogWarning($"{clientId} completed connection but was not in the ConnectionQueue!");
            if (_currentConnectingPlayer == clientId)
                _currentConnectingPlayer = null;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.LateUpdate))]
        private static void ProcessConnectionQueue(StartOfRound __instance)
        {
            if (!StartOfRound.Instance.IsServer)
                return;
            
            if (_currentConnectingPlayer != null)
            {
                if ((ulong)DateTimeOffset.Now.ToUnixTimeMilliseconds() < _currentConnectingExpiration)
                    return;
                
                LobbyControl.Log.LogWarning($"Connection to {_currentConnectingPlayer} expired, Disonnecting!");
                try
                {
                    NetworkManager.Singleton.DisconnectClient(_currentConnectingPlayer.Value);
                }
                catch (Exception ex)
                {
                    LobbyControl.Log.LogError(ex);
                }
                ConnectionQueue.Remove(_currentConnectingPlayer.Value);
                _currentConnectingPlayer = null;
            }
            if (ConnectionQueue.Count <= 0)
                return;

            var clientId = ConnectionQueue[0];
            _currentConnectingPlayer = clientId;
            _currentConnectingExpiration = (ulong)DateTimeOffset.Now.ToUnixTimeMilliseconds() + LobbyControl.PluginConfig.JoinQueue.ConnectionTimeout.Value;
            
            LobbyControl.Log.LogInfo($"Started processing {clientId} connection");
            try
            {
                __instance.OnClientConnect(clientId);                
            }
            catch (Exception ex)
            {
                LobbyControl.Log.LogError(ex);
            }
        }
        
    }
}