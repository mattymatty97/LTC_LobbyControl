using LobbyControl.API;
using Steamworks;
using Unity.Networking.Transport;

namespace LobbyControl.ConnectionQueue;

/// <summary>
/// Represents a Steam-based lobby connection identified by a <see cref="SteamId"/>.
/// </summary>
/// <param name="steamId">The Steam ID associated with this connection.</param>
public class SteamConnection(SteamId steamId) : ILobbyConnection
{
    /// <inheritdoc />
    public string Name => SteamId!.Value.IsValid ? new Friend(SteamId!.Value).Name : $"[{SteamId.Value}]";

    /// <inheritdoc />
    public SteamId? SteamId { get; } = steamId;

    /// <inheritdoc />
    public NetworkEndPoint? EndPoint => null;

    /// <inheritdoc />
    public void AcceptConnection() { }

    /// <inheritdoc />
    public void RefuseConnection(string reason = null) { }

    /// <inheritdoc />
    public void DropConnection() { }

    /// <inheritdoc />
    public void Dispose() { }
}
