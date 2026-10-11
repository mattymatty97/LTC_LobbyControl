using System;
using Steamworks;
using Unity.Networking.Transport;

// ReSharper disable MemberCanBePrivate.Global

namespace LobbyControl.API;

/// <summary>
/// Provides access to connection events, connection queue state, and checkpoint synchronization
/// for players connecting to the lobby.
/// </summary>
public static partial class ConnectionEvents
{
    /// <summary>
    /// Gets the network client ID of the player currently undergoing connection processing,
    /// or <c>null</c> if no client is actively connecting.
    /// </summary>
    public static partial ulong? ConnectingClientId { get; }

    /// <summary>
    /// Gets the <see cref="SteamId"/> of the player currently undergoing connection processing,
    /// or <c>null</c> if unavailable or no client is actively connecting.
    /// </summary>
    public static partial SteamId? ConnectingSteamId { get; }

    /// <summary>
    /// Gets the network transport endpoint of the player currently undergoing connection processing,
    /// or <c>null</c> if unavailable or no client is actively connecting.
    /// </summary>
    public static partial NetworkEndPoint? ConnectingEndpoint { get; }

    /// <summary>
    /// Gets the player display name of the player currently undergoing connection processing,
    /// or <c>null</c> if unavailable or no client is actively connecting.
    /// </summary>
    public static partial string ConnectingName { get; }

    /// <summary>
    /// Gets an array of registered active <see cref="ConnectionCheckpoint"/> instances that have not yet been completed
    /// for the currently connecting client.
    /// </summary>
    public static partial ConnectionCheckpoint[] MissingCheckpoints { get; }

    //---------------------EVENTS------------------------

    /// <summary>
    /// Event triggered on the host or server when a connecting client has satisfied all required checkpoints
    /// and completed the connection handshake.
    /// </summary>
    /// <remarks>
    /// Passes the network client ID of the connected player.
    /// </remarks>
    public static event Action<ulong> OnConnectionCompletedServer;

    /// <summary>
    /// Event triggered on clients when a notification is received from the server indicating that a player
    /// has completed connection processing.
    /// </summary>
    /// <remarks>
    /// Passes the network client ID of the connected player.
    /// </remarks>
    public static event Action<ulong> OnClientConnectionCompletedClient;

    /// <summary>
    /// Event triggered on the host or server whenever a specific <see cref="ConnectionCheckpoint"/> is marked
    /// as completed for the currently connecting client.
    /// </summary>
    /// <remarks>
    /// Passes the network client ID of the connecting player and the completed <see cref="ConnectionCheckpoint"/>.
    /// </remarks>
    public static event Action<ulong, ConnectionCheckpoint> OnConnectionCheckpointServer;
}
