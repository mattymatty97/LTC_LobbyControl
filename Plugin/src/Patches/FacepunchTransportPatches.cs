using System.Collections.Generic;
using HarmonyLib;
using Netcode.Transports.Facepunch;
using Steamworks.Data;
using Unity.Netcode;

namespace LobbyControl.Patches;

[HarmonyPatch]
internal static class FacepunchTransportPatches
{
    internal static readonly Dictionary<ulong, (Connection connection, ConnectionInfo connectionInfo)> Connections = [];

    [HarmonyPrefix]
    [HarmonyPatch(typeof(FacepunchTransport), "Steamworks.ISocketManager.OnConnected")]
    private static void TrackNewConnection(FacepunchTransport __instance, Connection connection, ConnectionInfo info)
    {
        Connections[connection.Id] = (connection, info);
        LobbyControl.Log.LogDebug($"(FacepunchTransport) New connection: {connection.Id} from SteamID {info.identity.SteamId}");
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(FacepunchTransport), "Steamworks.ISocketManager.OnDisconnected")]
    private static void TrackDisconnection(FacepunchTransport __instance, Connection connection, ConnectionInfo info)
    {
        Connections.Remove(connection.Id);
        LobbyControl.Log.LogDebug($"(FacepunchTransport) Disconnected: {connection.Id} at SteamID {info.identity.SteamId}");
    }

    internal static bool TryGetConnection(ulong connectionId, out (Connection connection, ConnectionInfo connectionInfo) connection)
    {
        var transportId = NetworkManager.Singleton.ConnectionManager.ClientIdToTransportId(connectionId);

        return Connections.TryGetValue(transportId, out connection);
    }
}
