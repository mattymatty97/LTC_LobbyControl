using System;
using System.Collections.Generic;
using BepInEx;

namespace LobbyControl.API;

public partial class ConnectionCheckpoint : IDisposable
{
    public static partial ConnectionCheckpoint RegisterCheckpoint(BaseUnityPlugin source, string name)
    {
        return RegisterCheckpoint(source.Info, name);
    }

    public static partial ConnectionCheckpoint RegisterCheckpoint(PluginInfo source, string name)
    {
        return RegisterCheckpoint(source.Metadata, name);
    }

    public static partial ConnectionCheckpoint RegisterCheckpoint(BepInPlugin source, string name)
    {
        var dictionaryKey = (source, name);

        if (RegisteredCheckpoints.TryGetValue(dictionaryKey, out var reference) &&
            reference.TryGetTarget(out var checkpoint) && !checkpoint.IsDisposed)
            return checkpoint;

        checkpoint = new ConnectionCheckpoint(source, name);

        RegisteredCheckpoints[dictionaryKey] = new WeakReference<ConnectionCheckpoint>(checkpoint);
        return checkpoint;
    }

    public partial bool IsDisposed => Mask == 0L;

    public partial bool Set(ulong clientId)
    {
        if (IsDisposed)
            throw new InvalidOperationException($"This {nameof(ConnectionCheckpoint)} has already been disposed");

        if (ConnectionEvents.ConnectingClientId is null && !PluginConfig.JoinQueue.Enabled.Value)
            return true;

        if (ConnectionEvents.ConnectingClientId != clientId)
        {
            LobbyControl.Log.LogWarning(
                $"CompleteCheckpoint ('{Name}' from '{Plugin.Name}') was called for client '{clientId}' but '{ConnectionEvents.ConnectingClientId}' was expected");
            return false;
        }

        ConnectionEvents.CurrentCheckpoints |= Mask;
        LobbyControl.Log.LogDebug($"client {ConnectionEvents.ConnectingName} completed checkpoint '{Name}' from '{Plugin.Name}'");

        ConnectionEvents.RaiseConnectionCheckpointServerEvent(clientId, this);

        return true;
    }

    public partial bool Reset(ulong clientId)
    {
        if (IsDisposed)
            throw new InvalidOperationException($"This {nameof(ConnectionCheckpoint)} has already been disposed");

        if (ConnectionEvents.ConnectingClientId is null && !PluginConfig.JoinQueue.Enabled.Value)
            return true;

        if (ConnectionEvents.ConnectingClientId != clientId)
        {
            LobbyControl.Log.LogWarning(
                $"ResetCheckpoint ('{Name}' from '{Plugin.Name}') was called for client '{clientId}' but '{ConnectionEvents.ConnectingClientId}' was expected");
            return false;
        }

        LobbyControl.Log.LogDebug($"'{Plugin.Name}' reset checkpoint '{Name}' for client {ConnectionEvents.ConnectingName}");
        ConnectionEvents.CurrentCheckpoints &= ~Mask;

        return true;
    }

    public partial void Dispose()
    {
        if (IsDisposed)
            throw new InvalidOperationException($"This {nameof(ConnectionCheckpoint)} has already been disposed");

        CheckpointMask &= ~Mask;
        ConnectionEvents.CurrentCheckpoints &= ~Mask;

        Mask = 0;
    }

    ///<inheritdoc />
    ~ConnectionCheckpoint()
    {
        if (!IsDisposed)
            Dispose();
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{{{Name} from {Plugin.Name}}}";
    }

    // internal stuff

    private ConnectionCheckpoint(BepInPlugin source, string name)
    {
        Name = name;
        Plugin = source;
        if (!GetNewMask(out var mask))
            throw new IndexOutOfRangeException("Too many checkpoints");

        LobbyControl.Log.LogDebug(
            $"Checkpoint '{name}' from '{source.Name}' created with mask '{Convert.ToString(mask, 2)}'");
        Mask = mask;
        CheckpointMask |= Mask;
    }

    private static bool GetNewMask(out Int64 mask)
    {
        uint index = 0;
        mask = 0b1;

        LobbyControl.Log.LogDebug(
            $"{Convert.ToString(CheckpointMask, 2)} & {Convert.ToString(mask, 2)} = {Convert.ToString(CheckpointMask & mask, 2)}");
        while ((CheckpointMask & mask) != 0)
        {
            index++;

            if (index >= 64)
                return false;

            mask <<= 1;
            LobbyControl.Log.LogDebug(
                $"{Convert.ToString(CheckpointMask, 2)} & {Convert.ToString(mask, 2)} = {Convert.ToString(CheckpointMask & mask, 2)}b");
        }

        return true;
    }

    internal static readonly Dictionary<(BepInPlugin plugin, string name), WeakReference<ConnectionCheckpoint>>
        RegisteredCheckpoints = [];

    internal static Int64 CheckpointMask { get; private set; }
}
