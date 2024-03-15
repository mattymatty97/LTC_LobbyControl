using System;
using System.Collections.Concurrent;
using System.Threading;
using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;

namespace LobbyControl.Patches
{
    [HarmonyPatch]
    internal class WallPlayerFix
    {

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.ConnectionApproval))]
        [HarmonyPriority(10)]
        private static void ThrottleApprovals(bool __runOriginal,
            NetworkManager.ConnectionApprovalRequest request,
            NetworkManager.ConnectionApprovalResponse response)
        {
            if (!__runOriginal || !response.Approved)
                return;
            
            if (!LobbyControl.PluginConfig.JoinQueue.Enabled.Value)
                return;
            
            response.Pending = true;
            ConnectionQueue.Enqueue(response);
            LobbyControl.Log.LogWarning($"Connection request Enqueued! count:{ConnectionQueue.Count}");
        }

        private static readonly ConcurrentQueue<NetworkManager.ConnectionApprovalResponse> ConnectionQueue = new ConcurrentQueue<NetworkManager.ConnectionApprovalResponse>();
        private static readonly object _lock = new object();
        private static ulong? _currentConnectingPlayer = null;
        private static ulong _currentConnectingExpiration = 0;
        private static bool[] _currentConnectingPlayerConfirmations = new bool[2];
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.OnClientConnect))]
        private static void OnClientConnect(StartOfRound __instance, ulong clientId)
        {
            lock (_lock)
            {
                if (__instance.IsServer && LobbyControl.PluginConfig.JoinQueue.Enabled.Value)
                {
                    _currentConnectingPlayerConfirmations = new bool[2];
                    _currentConnectingPlayer = clientId;
                    _currentConnectingExpiration = (ulong)Environment.TickCount +
                                                   LobbyControl.PluginConfig.JoinQueue.ConnectionTimeout.Value;
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.Singleton_OnClientDisconnectCallback))]
        private static void OnClientDisconnect(GameNetworkManager __instance, ulong clientId)
        {
            if (!__instance.isHostingGame)
                return;
            
            LobbyControl.Log.LogInfo($"{clientId} disconnected");
            lock (_lock)
            {
                if (_currentConnectingPlayer == clientId)
                    _currentConnectingPlayer = null;
            }
        }

        //SyncAlreadyHeldObjectsServerRpc
        [HarmonyPostfix]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.__rpc_handler_682230258))]
        private static void ClientConnectionCompleted1(__RpcParams rpcParams)
        {
            if (!StartOfRound.Instance.IsServer)
                return;
            
            var clientId = rpcParams.Server.Receive.SenderClientId;

            lock (_lock)
            {
                if (_currentConnectingPlayer != clientId) 
                    return;
            
                _currentConnectingPlayerConfirmations[0] = true;

                if (_currentConnectingPlayerConfirmations[1] || GameNetworkManager.Instance.disableSteam)
                {
                    LobbyControl.Log.LogWarning($"{clientId} completed the connection");
                    _currentConnectingPlayer = null;
                }
                else
                {
                    LobbyControl.Log.LogWarning($"{clientId} is waiting to synchronize the name");
                }
            }
        }
        
        //SendNewPlayerValuesServerRpc
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.__rpc_handler_2504133785))]
        private static void ClientConnectionCompleted2(__RpcParams rpcParams)
        {
            if (!StartOfRound.Instance.IsServer)
                return;
            
            var clientId = rpcParams.Server.Receive.SenderClientId;

            lock (_lock)
            {
                if ( _currentConnectingPlayer != clientId) 
                    return;
            
                _currentConnectingPlayerConfirmations[1] = true;

                if (_currentConnectingPlayerConfirmations[0])
                {
                    LobbyControl.Log.LogWarning($"{clientId} completed the connection");
                    _currentConnectingPlayer = null;
                }
                else
                {
                    LobbyControl.Log.LogWarning($"{clientId} is waiting to synchronize the held items");
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.LateUpdate))]
        private static void ProcessConnectionQueue()
        {
            if (!StartOfRound.Instance.IsServer)
                return;
            
            if(!Monitor.TryEnter(_lock))
                return;

            try
            {
                if (_currentConnectingPlayer.HasValue)
                {
                    if ((ulong)Environment.TickCount < _currentConnectingExpiration)
                        return;

                    if (_currentConnectingPlayer.Value != 0L)
                    {
                        LobbyControl.Log.LogWarning(
                            $"Connection to {_currentConnectingPlayer.Value} expired, Disconnecting!");
                        try
                        {
                            NetworkManager.Singleton.DisconnectClient(_currentConnectingPlayer.Value);
                        }
                        catch (Exception ex)
                        {
                            LobbyControl.Log.LogError(ex);
                        }
                    }

                    _currentConnectingPlayer = null;
                }
                else
                {
                    if (ConnectionQueue.TryDequeue(out var response))
                    {
                        LobbyControl.Log.LogWarning($"Connection request Resumed! remaining: {ConnectionQueue.Count}");
                        response.Pending = false;
                        _currentConnectingPlayerConfirmations = new bool[2];
                        _currentConnectingPlayer = 0L;
                        _currentConnectingExpiration = (ulong)Environment.TickCount + 5000UL;
                    }
                }
            }
            catch (Exception ex)
            {
                LobbyControl.Log.LogError(ex);
            }
            finally
            {
                Monitor.Exit(_lock);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.OnLocalDisconnect))]
        private static void FlushConnectionQueue()
        {
            lock (_lock)
            {
                _currentConnectingPlayerConfirmations = new bool[2];
                _currentConnectingPlayer = null;
                _currentConnectingExpiration = 0UL;
                if (ConnectionQueue.Count > 0)
                {
                    LobbyControl.Log.LogWarning($"Disconnecting with {ConnectionQueue.Count} pending connection, Flushing!");
                }

                while (ConnectionQueue.TryDequeue(out var response))
                {
                    response.Reason = "Host has disconnected!";
                    response.Approved = false;
                    response.Pending = false;
                }
            }
        }
    }
}