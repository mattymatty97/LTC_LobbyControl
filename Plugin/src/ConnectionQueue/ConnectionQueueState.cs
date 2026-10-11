using System.Collections.Concurrent;
using JetBrains.Annotations;
using LobbyControl.API;
using LobbyControl.Utils;

namespace LobbyControl.ConnectionQueue;

internal static class ConnectionQueueState
{
    internal static readonly ConcurrentQueue<ConnectionQueueEntry> ConnectionQueue = new();
    public static ConnectionQueueEntry _connectingClient;

    [CanBeNull]
    internal static ConnectionQueueEntry ConnectingClient
    {
        get => _connectingClient;
        set
        {
            _connectingClient = value;
            ConnectionEvents.ResetCheckpoints();
        }
    }

    internal static int QueuedClients => ConnectionQueue.Count + (ConnectingClient is not null ? 1 : 0);

    internal static readonly FreeRunningTimer ConnectionTimer = new();
}
