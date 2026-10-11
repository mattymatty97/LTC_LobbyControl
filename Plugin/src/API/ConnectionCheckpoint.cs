using System;
using BepInEx;

// ReSharper disable MemberCanBePrivate.Global

namespace LobbyControl.API;

/// <summary>
/// Represents a synchronization checkpoint used during client connection and join queue processing.
/// Checkpoints allow plugins to coordinate connection steps before a client fully completes joining.
/// </summary>
public partial class ConnectionCheckpoint : IDisposable
{
    /// <summary>
    /// Registers a new connection checkpoint or retrieves an existing active checkpoint for the specified plugin instance.
    /// </summary>
    /// <param name="source">The <see cref="BaseUnityPlugin"/> instance creating or owning the checkpoint.</param>
    /// <param name="name">The unique identifier name for the checkpoint under the given plugin.</param>
    /// <returns>The registered <see cref="ConnectionCheckpoint"/> instance.</returns>
    /// <exception cref="IndexOutOfRangeException">Thrown if the maximum number of registered checkpoints (64) is exceeded.</exception>
    public static partial ConnectionCheckpoint RegisterCheckpoint(BaseUnityPlugin source, string name);

    /// <summary>
    /// Registers a new connection checkpoint or retrieves an existing active checkpoint for the specified plugin metadata info.
    /// </summary>
    /// <param name="source">The <see cref="PluginInfo"/> containing plugin metadata.</param>
    /// <param name="name">The unique identifier name for the checkpoint under the given plugin.</param>
    /// <returns>The registered <see cref="ConnectionCheckpoint"/> instance.</returns>
    /// <exception cref="IndexOutOfRangeException">Thrown if the maximum number of registered checkpoints (64) is exceeded.</exception>
    public static partial ConnectionCheckpoint RegisterCheckpoint(PluginInfo source, string name);

    /// <summary>
    /// Registers a new connection checkpoint or retrieves an existing active checkpoint for the specified BepInEx plugin definition.
    /// </summary>
    /// <param name="source">The <see cref="BepInPlugin"/> metadata identifying the owner plugin.</param>
    /// <param name="name">The unique identifier name for the checkpoint under the given plugin.</param>
    /// <returns>The registered <see cref="ConnectionCheckpoint"/> instance.</returns>
    /// <exception cref="IndexOutOfRangeException">Thrown if the maximum number of registered checkpoints (64) is exceeded.</exception>
    public static partial ConnectionCheckpoint RegisterCheckpoint(BepInPlugin source, string name);

    /// <summary>
    /// Gets the unique name of this checkpoint.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the <see cref="BepInPlugin"/> metadata of the plugin that owns this checkpoint.
    /// </summary>
    public BepInPlugin Plugin { get; }

    /// <summary>
    /// Gets the unique bitmask assigned to this checkpoint for tracking completion status.
    /// </summary>
    public Int64 Mask { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this checkpoint instance has been disposed.
    /// </summary>
    public partial bool IsDisposed { get; }

    /// <summary>
    /// Marks this checkpoint as completed for the specified connecting client.
    /// </summary>
    /// <param name="clientId">The network client ID of the connecting player.</param>
    /// <returns>
    /// <c>true</c> if the checkpoint was successfully marked as completed or if the join queue is disabled;
    /// <c>false</c> if the provided <paramref name="clientId"/> does not match the currently connecting client.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown if this checkpoint has already been disposed.</exception>
    public partial bool Set(ulong clientId);

    /// <summary>
    /// Resets the completion status of this checkpoint for the specified connecting client.
    /// </summary>
    /// <param name="clientId">The network client ID of the connecting player.</param>
    /// <returns>
    /// <c>true</c> if the checkpoint status was successfully reset or if the join queue is disabled;
    /// <c>false</c> if the provided <paramref name="clientId"/> does not match the currently connecting client.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown if this checkpoint has already been disposed.</exception>
    public partial bool Reset(ulong clientId);

    /// <summary>
    /// Disposes this checkpoint, freeing its assigned bitmask slot and removing it from active tracking.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if this checkpoint has already been disposed.</exception>
    public partial void Dispose();
}
