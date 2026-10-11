using LobbyControl.API;
using Steamworks;
using Unity.Networking.Transport;

namespace LobbyControl.ConnectionQueue;

/// <summary>
/// Represents a LAN-based lobby connection identified by a <see cref="NetworkEndPoint"/>.
/// </summary>
/// <param name="endPoint">The network endpoint associated with this connection.</param>
public class LanConnection(NetworkEndPoint endPoint) : ILobbyConnection
{
    /// <inheritdoc />
    public string Name => EndPoint!.Value.Address;

    /// <inheritdoc />
    public SteamId? SteamId => null;

    /// <inheritdoc />
    public NetworkEndPoint? EndPoint { get; } = endPoint;

    /// <inheritdoc />
    public void AcceptConnection() { }

    /// <inheritdoc />
    public void RefuseConnection(string reason = null) { }

    /// <inheritdoc />
    public void DropConnection() { }

    /// <inheritdoc />
    public void Dispose() { }
}
