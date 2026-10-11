using System;
using JetBrains.Annotations;
using Steamworks;
using Unity.Networking.Transport;

namespace LobbyControl.API;

/// <summary>
/// Represents an incoming or active lobby connection, providing metadata and control methods to manage the connection lifecycle.
/// </summary>
public interface ILobbyConnection : IDisposable
{
    /// <summary>
    /// Gets the display name or identifier representing this connection.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the <see cref="Steamworks.SteamId"/> associated with this connection, or <c>null</c> if this is not a Steam connection.
    /// </summary>
    [CanBeNull] public SteamId? SteamId { get; }

    /// <summary>
    /// Gets the <see cref="NetworkEndPoint"/> associated with this connection, or <c>null</c> if this is not a direct network or LAN connection.
    /// </summary>
    [CanBeNull] public NetworkEndPoint? EndPoint { get; }

    /// <summary>
    /// Accepts the pending lobby connection, allowing the client to proceed with joining.
    /// </summary>
    public void AcceptConnection();

    /// <summary>
    /// Refuses the pending lobby connection, optionally specifying a reason for rejection.
    /// </summary>
    /// <param name="reason">An optional explanation for why the connection was refused.</param>
    public void RefuseConnection([CanBeNull] string reason = null);

    /// <summary>
    /// Forcibly drops and disconnects the connection.
    /// </summary>
    public void DropConnection();
}
