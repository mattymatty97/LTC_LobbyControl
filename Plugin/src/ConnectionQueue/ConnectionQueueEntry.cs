using System;
using System.Text;
using JetBrains.Annotations;
using LobbyControl.API;
using Steamworks;
using Unity.Netcode;
using Unity.Networking.Transport;

namespace LobbyControl.ConnectionQueue;

internal class ConnectionQueueEntry
{
    internal ILobbyConnection Connection { get; }

    public SteamId? SteamId => Connection.SteamId;

    public NetworkEndPoint? EndPoint => Connection.EndPoint;

    public ulong? ClientId => UnityConnection?.request.ClientNetworkId;

    public string Name => Connection.Name + $"({ClientId})";

    public ushort? QueuePosition => ConnectionQueueState.QueuePosition(Connection);

    public (NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)? UnityConnection { get; private set; }

    internal ConnectionQueueEntry(ILobbyConnection Connection)
    {
        this.Connection  = Connection;
    }

    internal bool AttachUnityConnection(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        if (UnityConnection.HasValue)
            return false;

        UnityConnection = (request, response);
        return true;
    }

    internal void DropConnection()
    {
        Connection.DropConnection();

        if (ClientId.HasValue)
        {
            ForcefullyDisconnectUnityClient(ClientId.Value);
        }
    }

    internal void RefuseConnection([NotNull] string reason)
    {
        Connection.RefuseConnection(reason);

        if (UnityConnection.HasValue && UnityConnection.Value.response.Pending)
        {
            UnityConnection.Value.response.Approved = false;
            UnityConnection.Value.response.Reason   = reason;
            UnityConnection.Value.response.Pending  = false;
        }
        else if (ClientId.HasValue)
        {
            ForcefullyDisconnectUnityClient(ClientId.Value);
        }
    }

    internal void AcceptConnection()
    {
        Connection.AcceptConnection();

        if (UnityConnection.HasValue)
        {
            UnityConnection.Value.response.Approved = true;
            UnityConnection.Value.response.Pending  = false;
        }
    }

    public override string ToString()
    {
        var builder = new StringBuilder("{");

        if (SteamId is { IsValid: true })
        {
            builder.Append("Name: ").Append(new Friend(SteamId.Value).Name);
            builder.Append(", SteamID:").Append(SteamId.Value);
            builder.Append(",");
        }

        if (EndPoint is { IsValid: true })
        {
            builder.Append("Endpoint: ").Append(EndPoint.Value.Address);
            builder.Append(",");
        }

        builder.Append("ClientId: ").Append(ClientId);
        builder.Append("}");

        return builder.ToString();
    }

    private static void ForcefullyDisconnectUnityClient(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
            return;

        try
        {
            //Forcefully close the connection
            var nm = NetworkManager.Singleton;
            nm.DisconnectClient(clientId);
            var transportID = nm.ConnectionManager.ClientIdToTransportId(clientId);
            NetworkManager.Singleton.NetworkConfig.NetworkTransport.DisconnectRemoteClient(transportID);
        }
        catch (Exception ex)
        {
            LobbyControl.Log.LogError(ex);
        }
    }

    ~ConnectionQueueEntry()
    {
        Connection.Dispose();
    }
}
