using System.Collections.Concurrent;
using System.Linq;
using JetBrains.Annotations;
using LobbyControl.API;
using LobbyControl.Utils;
using UnityEngine.InputSystem.Utilities;

namespace LobbyControl.ConnectionQueue;

/// <summary>
/// Manages the state and entrypoints for the lobby connection queue.
/// </summary>
public static class ConnectionQueueState
{
    /// <summary>
    /// Gets the total count of clients currently queued or actively connecting.
    /// </summary>
    public static int QueuedClients => ConnectionQueue.Count + (ConnectingClient is not null ? 1 : 0);

    /// <summary>
    /// Enqueues a lobby connection into the join queue if it is not already queued or actively connecting.
    /// </summary>
    /// <param name="connection">The <see cref="ILobbyConnection"/> to enqueue.</param>
    /// <returns>
    /// <c>true</c> if the connection was successfully enqueued; otherwise, <c>false</c>.
    /// </returns>
    public static bool EnqueueConnection(ILobbyConnection connection)
    {
        if (ConnectingClient?.Connection == connection)
            return false;
        if (ConnectionQueue.FirstOrDefault(e => e.Connection == connection) is not null)
            return false;
        ConnectionQueue.Enqueue(new ConnectionQueueEntry(connection));
        return true;
    }

    /// <summary>
    /// Gets the current position of a connection in the join queue, or <c>null</c> if the connection is not queued.
    /// </summary>
    /// <param name="connection">The <see cref="ILobbyConnection"/> to query position for.</param>
    /// <returns>
    /// The queue position (0 for the actively connecting client), or <c>null</c> if not found in the queue.
    /// </returns>
    public static ushort? QueuePosition(ILobbyConnection connection)
    {
        if (ConnectingClient?.Connection == connection)
            return 0;
        var idx = ConnectionQueue.Select(e => e.Connection).IndexOf(connection);
        return idx < 0 ? null : (ushort)(idx + 1);
    }

    internal static readonly ConcurrentQueue<ConnectionQueueEntry> ConnectionQueue = new();

    internal static readonly FreeRunningTimer ConnectionTimer = new();

    [CanBeNull]
    internal static ConnectionQueueEntry ConnectingClient
    {
        get;
        set
        {
            field = value;
            ConnectionEvents.ResetCheckpoints();
        }
    }
}
