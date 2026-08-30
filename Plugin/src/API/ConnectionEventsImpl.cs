using System;
using System.Collections.Generic;
using LobbyControl.Patches;
using Steamworks;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine.Pool;

namespace LobbyControl.API;

public static partial class ConnectionEvents
{
    //---------------------EVENTS------------------------

    public static partial ulong? ConnectingClientId => JoinQueuePatches.ConnectingClient?.ClientId;
    public static partial SteamId? ConnectingSteamId => JoinQueuePatches.ConnectingClient?.SteamId;
    public static partial string ConnectingName => JoinQueuePatches.ConnectingClient?.Name;

    public static partial ConnectionCheckpoint[] MissingCheckpoints
    {
        get
        {
            using (ListPool<ConnectionCheckpoint>.Get(out var list))
            {
                foreach (var references in ConnectionCheckpoint.RegisteredCheckpoints.Values)
                {
                    if (!references.TryGetTarget(out var checkpoint))
                        continue;

                    if (checkpoint.IsDisposed)
                        continue;

                    if ((CurrentCheckpoints & checkpoint.Mask) == 0)
                        list.Add(checkpoint);
                }

                return list.ToArray();
            }
        }
    }

    //--------------------INTERNAL STUFF------------------

    internal static bool HostHasLobbyControl { get; set; }

    internal static void RaiseConnectionCheckpointServerEvent(ulong clientId, ConnectionCheckpoint checkpoint)
    {
        try
        {
            OnConnectionCheckpointServer?.Invoke(clientId, checkpoint);
        }
        catch (Exception ex)
        {
            LobbyControl.Log.LogError($"Exception while processing ConnectionCheckpointServerEvent: {ex}");
        }
    }

    internal static void RaiseConnectionCompleteServerEvent(ulong clientId)
    {
        try
        {
            RaiseConnectionCompleteEventClientRpc(clientId);
            OnConnectionCompletedServer?.Invoke(clientId);
        }
        catch (Exception ex)
        {
            LobbyControl.Log.LogError($"Exception while processing ConnectionCompleteServerEvent: {ex}");
        }
    }

    private static readonly string BaseName = typeof(ConnectionEvents).FullName;

    private static readonly string RaiseConnectionCompleteEventClientRpcMessage =
        $"{BaseName}|RaiseConnectionCompleteEvent";

    internal static void RegisterNamedMessages()
    {
        NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler(
            RaiseConnectionCompleteEventClientRpcMessage,
            OnRaiseConnectionCompleteEventClientRpc);
    }

    internal static void UnregisterNamedMessages()
    {
        NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler(
            RaiseConnectionCompleteEventClientRpcMessage);
    }


    internal static void RaiseConnectionCompleteEventClientRpc(ulong clientId, IReadOnlyList<ulong> targets = null)
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        var buffer = new FastBufferWriter(sizeof(ulong), Allocator.Temp);
        buffer.WriteValue(clientId);

        if (targets == null)
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessageToAll(
                RaiseConnectionCompleteEventClientRpcMessage, buffer);
        else
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage(
                RaiseConnectionCompleteEventClientRpcMessage, targets,
                buffer);
    }

    private static void OnRaiseConnectionCompleteEventClientRpc(ulong senderId, FastBufferReader data)
    {
        if (senderId != NetworkManager.ServerClientId)
            return;

        if (!GameNetworkManager.Instance || !GameNetworkManager.Instance.localPlayerController)
        {
            LobbyControl.Log.LogError(
                $"Received {nameof(RaiseConnectionCompleteEventClientRpc)} while not connected to a lobby!");
            return;
        }

        data.ReadValue(out ulong clientId);

        try
        {
            OnClientConnectionCompletedClient?.Invoke(clientId);
        }
        catch (Exception ex)
        {
            LobbyControl.Log.LogError($"Exception while processing ConnectionCompleteClientEvent: {ex}");
        }
    }

    internal static bool HasCompletedAllCheckpoints => ConnectionCheckpoint.CheckpointMask == CurrentCheckpoints;
    internal static long MissingCheckpointMask => ~CurrentCheckpoints & ConnectionCheckpoint.CheckpointMask;

    internal static long CurrentCheckpoints;

    internal static void ResetCheckpoints()
    {
        CurrentCheckpoints = 0;
    }
}
