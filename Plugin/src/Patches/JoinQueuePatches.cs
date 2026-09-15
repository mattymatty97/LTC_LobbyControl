using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using GameNetcodeStuff;
using HarmonyLib;
using JetBrains.Annotations;
using LobbyControl.API;
using LobbyControl.Networking;
using LobbyControl.Utils;
using LobbyControl.Utils.IL;
using MonoMod.RuntimeDetour;
using Netcode.Transports.Facepunch;
using Steamworks;
using Steamworks.Data;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport;
using Object = UnityEngine.Object;

namespace LobbyControl.Patches;

[HarmonyPatch]
internal class JoinQueuePatches
{
    internal static uint MaxPlayerCount
    {
        get
        {
            var startOfRound = StartOfRound.Instance;
            if(!startOfRound)
                return 0;
            return (uint)startOfRound.allPlayerObjects.Length;
        }
    }

    internal static void Init()
    {
        var methodInfo =
            AccessTools.Method(typeof(StartOfRound), nameof(StartOfRound.SyncAlreadyHeldObjectsServerRpc));

        if (RPCUtils.TryGetRpcID(methodInfo, out var id))
        {
            var harmonyTarget = AccessTools.Method(typeof(StartOfRound), $"__rpc_handler_{id}");
            var harmonyFinalizer =
                AccessTools.Method(typeof(JoinQueuePatches), nameof(OnSyncAlreadyHeldObjectsServerRpc));
            LobbyControl.Harmony.Patch(harmonyTarget, null, null, null, new HarmonyMethod(harmonyFinalizer), null);
        }
        else
        {
            LobbyControl.Log.LogFatal("Could not find RPC id for SyncAlreadyHeldObjectsServerRpc");
        }

        methodInfo =
            AccessTools.Method(typeof(PlayerControllerB), nameof(PlayerControllerB.SendNewPlayerValuesServerRpc));

        if (RPCUtils.TryGetRpcID(methodInfo, out id))
        {
            var harmonyTarget = AccessTools.Method(typeof(PlayerControllerB), $"__rpc_handler_{id}");
            var harmonyFinalizer = AccessTools.Method(typeof(JoinQueuePatches), nameof(OnSendNewPlayerValuesServerRpc));
            LobbyControl.Harmony.Patch(harmonyTarget, null, null, null, new HarmonyMethod(harmonyFinalizer), null);
        }
        else
        {
            LobbyControl.Log.LogFatal("Could not find RPC id for SendNewPlayerValuesServerRpc");
        }

        methodInfo =
            AccessTools.Method(typeof(HUDManager), nameof(HUDManager.SyncAllPlayerLevelsServerRpc),
                [typeof(int), typeof(int)]);

        if (RPCUtils.TryGetRpcID(methodInfo, out id))
        {
            var harmonyTarget = AccessTools.Method(typeof(HUDManager), $"__rpc_handler_{id}");
            var harmonyFinalizer = AccessTools.Method(typeof(JoinQueuePatches), nameof(OnSyncAllPlayerLevelsServerRpc));
            LobbyControl.Harmony.Patch(harmonyTarget, null, null, null, new HarmonyMethod(harmonyFinalizer), null);
        }
        else
        {
            LobbyControl.Log.LogFatal("Could not find RPC id for SyncAllPlayerLevelsServerRpc");
        }

        //Connection completed callback

        ConnectionEvents.OnConnectionCompletedServer += OnConnectionCompletedServer;

        //StartOfMatchLever

        var monoModTarget = AccessTools.Method(typeof(StartOfRound), nameof(StartOfRound.StartGame));

        if (monoModTarget != null)
            LobbyControl.Hooks.Add(new Hook(monoModTarget, CheckValidStart, new HookConfig { Priority = 999 }));
        else
            LobbyControl.Log.LogFatal("Cannot apply patch to StartGame");
    }

    private static bool _checkpointsInitialized;
    private static ConnectionCheckpoint _syncAlreadyHeldObjectsCheckpoint;
    private static ConnectionCheckpoint _sendNewPlayerValuesCheckpoint;
    private static ConnectionCheckpoint _syncAllPlayerLevelsCheckpoint;

    private static ConnectionQueueEntry _connectingClient;

    internal static readonly ConcurrentQueue<ConnectionQueueEntry> ConnectionQueue = new();

    [CanBeNull]
    internal static ConnectionQueueEntry ConnectingClient
    {
        get => _connectingClient;
        private set
        {
            _connectingClient = value;
            ConnectionEvents.ResetCheckpoints();
        }
    }

    internal static int QueuedClients => ConnectionQueue.Count + (ConnectingClient is not null ? 1 : 0);

    internal static readonly FreeRunningTimer ConnectionTimer = new();

    private static void ForcefullyDisconnectClient(ulong clientId)
    {
        if (clientId == NetworkManager.Singleton.LocalClientId)
            return;

        try
        {
            //Forcefully close the connection
            var nm = NetworkManager.Singleton;
            var transportID = nm.ConnectionManager.ClientIdToTransportId(clientId);
            NetworkManager.Singleton.NetworkConfig.NetworkTransport.DisconnectRemoteClient(transportID);
        }
        catch (Exception ex)
        {
            LobbyControl.Log.LogError(ex);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.Awake))]
    private static void OnStartup(GameNetworkManager __instance)
    {
        if (_checkpointsInitialized)
            return;

        _checkpointsInitialized = true;

        _syncAlreadyHeldObjectsCheckpoint =
            ConnectionCheckpoint.RegisterCheckpoint(LobbyControl.Instance, "SyncAlreadyHeldObjects");

        _syncAllPlayerLevelsCheckpoint =
            ConnectionCheckpoint.RegisterCheckpoint(LobbyControl.Instance, "SyncAllPlayerLevels");

        if (__instance.disableSteam)
            return;

        _sendNewPlayerValuesCheckpoint =
            ConnectionCheckpoint.RegisterCheckpoint(LobbyControl.Instance, "SendNewPlayerValues");
    }

    [HarmonyFinalizer]
    [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.ConnectionApproval))]
    [HarmonyPriority(10)]
    private static Exception ThrottleApprovals(
        GameNetworkManager __instance,
        NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response,
        Exception __exception)
    {
        ConnectionQueueEntry entry;

        if (__exception != null)
            return __exception;

        if (!PluginConfig.JoinQueue.Enabled.Value)
            return null;

        if (!response.Approved)
            return null;

        try
        {
            LobbyControl.Log.LogDebug($"Connection request from {request.ClientNetworkId}. Current queue size: {QueuedClients}");

            var maxQueueSize = PluginConfig.JoinQueue.MaxSize.Value;
            if (QueuedClients >= maxQueueSize)
            {
                LobbyControl.Log.LogWarning($"Connection refused, Queue full! count:{QueuedClients}");
                if (PluginConfig.JoinQueue.ConnectionPopup.Value)
                {
                    HUDManager.Instance.StartCoroutine(HudUtils.ShowMessageAfterDelay("Connection refused",
                        $"Client {request.ClientNetworkId} requested a connection but queue was full!"));
                }

                var message = new StringBuilder();

                if (maxQueueSize == 1)
                {
                    message.Append("Another player is connecting\n");
                }
                else
                {
                    message.Append("Join Queue is Full !\n");
                    message.AppendFormat("Queued connections: {0}\n", QueuedClients);
                }

                message.Append("Please Wait a bit before retrying");

                response.Approved = false;
                response.Reason = message.ToString();
                return null;
            }

            var nm = NetworkManager.Singleton;

            response.Pending = true;

            switch (nm.NetworkConfig.NetworkTransport)
            {
                case FacepunchTransport:
                {
                    if (!FacepunchTransportPatches.TryGetConnection(request.ClientNetworkId, out var connectionPair) || !connectionPair.connectionInfo.identity.SteamId.IsValid)
                    {
                        LobbyControl.Log.LogWarning($"Connection refused, Failed to get SteamID for {request.ClientNetworkId}");

                        response.Approved = false;
                        response.Reason = "Failed to get SteamID";
                        response.Pending = false;
                        return null;
                    }

                    var steamID = connectionPair.connectionInfo.identity.SteamId;

                    LobbyControl.Log.LogDebug($"Request was from SteamID {steamID}");

                    if (StartOfRound.Instance.KickedClientIds.Contains(steamID.Value))
                    {
                        response.Approved = false;
                        response.Reason = "You tried to bypass the kick!";
                        response.Pending = false;
                        return null;
                    }

                    Task.Run(() => SteamFriends.RequestUserInformation(steamID));

                    // if this is the currently connecting client,
                    // populate missing properties and resume the connection immediately
                    if (ConnectingClient?.SteamId == steamID)
                    {
                        entry = ConnectingClient;
                        entry.UnityConnection = (request, response);
                        response.Pending = false;
                    }
                    else //prepare queue element
                    {
                        entry = new ConnectionQueueEntry(request, response, steamID);
                    }

                    if (PluginConfig.JoinQueue.ConnectionPopup.Value)
                    {
                        HUDManager.Instance.StartCoroutine(HudUtils.ShowMessageAfterDelay("Connection request",
                            $"Player {entry} requested a connection"));
                    }

                    break;
                }
                case UnityTransport unityTransport:
                {
                    var endpoint       = unityTransport!.GetEndpoint(request.ClientNetworkId);

                    LobbyControl.Log.LogDebug($"Request was from IP {endpoint.Address}");

                    entry = new ConnectionQueueEntry(request, response, endpoint);

                    if (PluginConfig.JoinQueue.ConnectionPopup.Value)
                    {
                        HUDManager.Instance.StartCoroutine(HudUtils.ShowMessageAfterDelay("Connection request",
                            $"Client {request.ClientNetworkId} requested a connection"));
                    }
                    break;
                }
                default:
                {
                    response.Approved = true;
                    response.Pending = false;
                    return null;
                }
            }

            if(entry.UnityConnection!.Value.response.Pending)
                ConnectionQueue.Enqueue(entry);

            LobbyControl.Log.LogWarning($"Connection request Enqueued! count:{QueuedClients}");
            return null;
        }
        catch (Exception e)
        {
            LobbyControl.Log.LogError($"Exception while processing connection request:\n{e}");

            response.Approved = true;
            response.Pending = false;
            return null;
        }
    }


    private static void OnConnectionCompletedServer(ulong clientId)
    {
        if (!PluginConfig.JoinQueue.Enabled.Value)
            return;

        //notify other players to reset the object variables
        var playerIndex = StartOfRound.Instance.ClientPlayerList[clientId];
        var targets = NetworkManager.Singleton.ConnectedClientsIds.ToList();
        //skip the connecting client as he's guaranteed to have all the values correct
        targets.Remove(clientId);
        NamedMessages.ResetPlayerValuesClientRpc(playerIndex, targets.ToArray());
        //re-sort the radar map so all clients are aligned
        NamedMessages.ReorderRadarClientRpc();
    }


    [HarmonyPrefix]
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.OnClientConnect))]
    private static void OnClientConnect(StartOfRound __instance, ulong clientId)
    {
        if (!__instance.IsServer)
            return;

        if (!PluginConfig.JoinQueue.Enabled.Value)
            return;

        if (ConnectingClient is not null && ConnectingClient.ClientId != clientId)
            LobbyControl.Log.LogError(
                $"client {clientId} connected while '{ConnectingClient.Name}' was still being processed!");
        else
        {
            //reset timeout to be a bit more lenient
            ConnectionTimer.Stop();
            ConnectionTimer.Start(TimeSpan.FromMilliseconds(PluginConfig.JoinQueue.ConnectionTimeout.Value));
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.OnPlayerConnectedClientRpc))]
    private static void OnClientConnect2(StartOfRound __instance, ulong clientId)
    {
        //run only if we're actually executing the Rpc code
        var networkManager = __instance.NetworkManager;
        if (networkManager == null || !networkManager.IsListening)
            return;
        if (__instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Client ||
            !networkManager.IsClient && !networkManager.IsHost)
            return;

        if (!__instance.IsServer)
            return;

        if (PluginConfig.JoinQueue.Enabled.Value)
            return;

        //fallback event to when vanilla adds the playerIndex to StartOfRound.Instance.ClientPlayerList
        ConnectionEvents.RaiseConnectionCompleteServerEvent(clientId);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.Singleton_OnClientDisconnectCallback))]
    private static void OnClientDisconnect(GameNetworkManager __instance, ulong clientId)
    {
        if (!__instance.isHostingGame)
            return;

        LobbyControl.Log.LogInfo($"{clientId} disconnected");

        if (ConnectingClient?.ClientId == clientId)
        {
            ConnectingClient = null;
            ConnectionTimer.Stop();
        }
        else
        {
            //if we disconnected while in queue mark the response as Approved to skip it
            foreach (var entry in ConnectionQueue.Where(e => e.ClientId == clientId))
            {
                entry.RefuseConnection("You disconnected!");
            }
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.Singleton_OnClientConnectedCallback))]
    private static void OnClientConnect(GameNetworkManager __instance, ulong clientId)
    {
        if (!__instance.isHostingGame)
            return;

        LobbyControl.Log.LogInfo($"{clientId} connected");
    }

    private static void OnSyncAlreadyHeldObjectsServerRpc(
        NetworkBehaviour target,
        __RpcParams rpcParams)
    {
        if (!target.IsServer)
            return;

        if (ConnectingClient is null)
            return;

        var clientId = rpcParams.Server.Receive.SenderClientId;

        _syncAlreadyHeldObjectsCheckpoint.Set(clientId);
    }

    private static void OnSendNewPlayerValuesServerRpc(
        NetworkBehaviour target, __RpcParams rpcParams)
    {
        if (!target.IsServer)
            return;

        if (ConnectingClient is null)
            return;

        var clientId = rpcParams.Server.Receive.SenderClientId;

        _sendNewPlayerValuesCheckpoint.Set(clientId);
    }

    private static void OnSyncAllPlayerLevelsServerRpc(
        NetworkBehaviour target, __RpcParams rpcParams)
    {
        if (!target.IsServer)
            return;

        if (ConnectingClient is null)
            return;

        var clientId = rpcParams.Server.Receive.SenderClientId;

        _syncAllPlayerLevelsCheckpoint.Set(clientId);
    }


    [HarmonyPostfix]
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.LateUpdate))]
    private static void ProcessConnectionQueue(StartOfRound __instance)
    {
        if (!__instance.IsServer)
            return;

        try
        {
            //check if the current connection reached all checkpoints!
            if (ConnectionEvents.HasCompletedAllCheckpoints && ConnectingClient is not null)
            {
                var client = ConnectingClient;
                ConnectingClient = null;

                LobbyControl.Log.LogDebug($"{client.Name} completed all the checkpoints");

                ConnectionTimer.Stop();

                LobbyControl.Log.LogWarning($"{client.Name} completed the connection");

                ConnectionEvents.RaiseConnectionCompleteServerEvent(client.ClientId!.Value);
            }

            //if we are still waiting for a connection to complete
            if (ConnectingClient is not null)
            {
                //wait till the timeout expires
                if (!ConnectionTimer.TimedOut)
                    return;

                //if there was an actual client
                if (ConnectingClient is not null)
                {
                    var missing = ConnectionEvents.MissingCheckpoints;

                    LobbyControl.Log.LogWarning(
                        $"missing checkpoints for {ConnectingClient.Name}: [{string.Join<ConnectionCheckpoint>(",", missing)}]");

                    if (PluginConfig.JoinQueue.TimeoutPopup.Value)
                        HUDManager.Instance.StartCoroutine(HudUtils.ShowMessageAfterDelay("Connection Timeout",
                                $"Player {ConnectingClient.Name}\n has been disconnected"));

                    HUDManager.Instance.StartCoroutine(HudUtils.ShowTipAfterDelay("Connection Timeout",
                        "If clients frequently fail to connect maybe consider increasing \"connection_timeout_ms\" in LobbyControl config",
                        5, "LCTip_LCTimeout"));

                    LobbyControl.Log.LogError($"Connection to {ConnectingClient.Name} expired, Disconnecting!");

                    ConnectingClient.DropConnection();
                }

                //allow the connection of the next client
                ConnectingClient = null;
            }

            var lobbyFull = (StartOfRound.Instance.connectedPlayersAmount + 1) >= MaxPlayerCount;

            //if we can let new connections in
            if (LateJoinPatches._allowNewConnection && !lobbyFull)
            {
                //wait until the delay between connections
                if (ConnectingClient is not null)
                    return;

                if (!ConnectionQueue.TryDequeue(out var entry))
                    return;

                if (entry.UnityConnection?.response.Approved is not true)
                {
                    LobbyControl.Log.LogWarning(
                        $"Connection request Skipped! remaining: {ConnectionQueue.Count}");
                    return;
                }

                LobbyControl.Log.LogWarning(
                    $"Connection request Resumed! remaining: {ConnectionQueue.Count}");

                entry.AcceptConnection();

                //if the queue has been disabled, approve all the connections w/o waiting
                if (!PluginConfig.JoinQueue.Enabled.Value)
                    return;

                ConnectingClient = entry;
                ConnectionTimer.Start(TimeSpan.FromMilliseconds(PluginConfig.JoinQueue.ConnectionTimeout.Value));

                if (!PluginConfig.JoinQueue.ConnectionPopup.Value)
                    return;

                if (entry.SteamId.IsValid)
                    HUDManager.Instance.StartCoroutine(HudUtils.ShowMessageAfterDelay("Connection resumed",
                        $"Player {entry.Name} is now connecting"));

                return;
            }

            while (ConnectionQueue.TryDequeue(out var entry))
            {
                entry.RefuseConnection(lobbyFull ? "Lobby is full!" : "Ship has landed!");
            }
        }
        catch (Exception ex)
        {
            LobbyControl.Log.LogError(ex);
        }
    }


    [HarmonyFinalizer]
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.OnLocalDisconnect))]
    private static void FlushConnectionQueue()
    {
        ConnectingClient = null;
        ConnectionTimer.Stop();

        if (ConnectionQueue.Count > 0)
        {
            LobbyControl.Log.LogWarning(
                $"Disconnecting with {ConnectionQueue.Count} pending connection, Flushing!");
        }

        while (ConnectionQueue.TryDequeue(out var entry))
        {
            entry.RefuseConnection("Host has disconnected!");
        }
    }

    //-----------HOST TIMEOUT DETECTION--------------

    private static readonly Stopwatch ConnectionStopwatch = new();

    [HarmonyPrefix]
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Awake))]
    private static void OnHostStartLoad()
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        if (!PluginConfig.JoinQueue.Enabled.Value)
            return;

        ConnectionStopwatch.Restart();
    }

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.StartSpatialVoiceChat), MethodType.Enumerator)]
    private static IEnumerable<CodeInstruction> PatchStartSpatialVoiceChat(IEnumerable<CodeInstruction> instructions,
        ILGenerator ilGenerator)
    {
        var codes = instructions.ToList();

        var injector = new ILInjector(codes, ilGenerator);

        injector.Find([
            ILMatcher.Callvirt(typeof(HUDManager).GetMethod(nameof(HUDManager.SyncAllPlayerLevelsServerRpc),
                [typeof(int), typeof(int)]))
        ]);

        if (!injector.IsValid)
        {
            LobbyControl.Log.LogFatal(
                "Failed to find HUDManager.SyncPlayerLevelServerRpc in StartOfRound.StartSpatialVoiceChat!");
            return codes;
        }

        injector
            .GoToMatchEnd()
            .Insert(new CodeInstruction(OpCodes.Call,
                typeof(JoinQueuePatches).GetMethod(nameof(OnHostLoaded),
                    BindingFlags.Static | BindingFlags.NonPublic)));

        return injector.ReleaseInstructions();
    }

    private static void OnHostLoaded()
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        if (!PluginConfig.JoinQueue.Enabled.Value)
            return;

        ConnectionStopwatch.Stop();

        var elapsed = ConnectionStopwatch.ElapsedMilliseconds;
        var currentTimeout = PluginConfig.JoinQueue.ConnectionTimeout.Value;

        LobbyControl.Log.LogDebug($"Lobby took {elapsed}ms to load");

        if (currentTimeout >= elapsed)
            return;

        LobbyControl.Log.LogWarning(
            $"Lobby took {elapsed}ms to load but the configured connectionTimeout is only {currentTimeout}ms !");

        HUDManager.Instance.StartCoroutine(HudUtils.ShowMessageAfterDelay("Low Connection Timeout!",
            $"Lobby took {elapsed}ms to load but the configured connectionTimeout is only {currentTimeout}ms", 5));
    }


    //--------------HANDLE SHIP LEVER----------------

    private static bool CanStartGame()
    {
        return ConnectingClient is null && ConnectionQueue.IsEmpty;
    }

    private static void CheckValidStart(Action<StartOfRound> orig, StartOfRound @this)
    {
        if (!@this.IsServer || !@this.inShipPhase)
        {
            orig(@this);
            return;
        }

        if (!CanStartGame())
        {
            var count = ConnectionQueue.Count + (ConnectingClient is not null ? 1 : 0);

            var leverScript = Object.FindAnyObjectByType<StartMatchLever>();

            leverScript.CancelStartGame();
            leverScript.CancelStartGameClientRpc();

            HUDManager.Instance.StartCoroutine(
                HudUtils.ShowMessageAfterDelay(
                    "GAME START CANCELLED",
                    $"{count} Players Connecting!!",
                    0,
                    true
                ));

            HUDManager.Instance.AddTextMessageServerRpc(
                $"there are still {count} Players connecting!!\n");
            return;
        }

        LateJoinPatches._allowNewConnection = false;

        orig(@this);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(StartMatchLever), nameof(StartMatchLever.CancelStartGame))]
    private static void FixUnusableLever(StartMatchLever __instance)
    {
        __instance.triggerScript.interactable = true;
    }

    [HarmonyFinalizer]
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SetShipReadyToLand))]
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Start))]
    private static void OnReadyToLand()
    {
        LateJoinPatches._allowNewConnection = true;
    }

    //--------------Other Classes----------------

    public class ConnectionQueueEntry
    {
        private ConnectionType _type;
        public SteamId SteamId { get; }
        public ulong? ClientId => UnityConnection?.request.ClientNetworkId;

        public string Name =>
            _type switch
            {
                ConnectionType.Steam => (SteamId.IsValid ? new Friend(SteamId.Value).Name : "????") + $"({ClientId})",
                ConnectionType.IP => $"{EndPoint.Address}({ClientId})",
                _ => throw new ArgumentOutOfRangeException()
            };

        public ushort QueuePosition {
            get {
                if (_connectingClient == this)
                    return 1;
                var idx = Array.IndexOf(ConnectionQueue.ToArray(), this);
                return (ushort)(idx < 0 ? 0 : idx + 1);
            }
        }

        public NetworkEndPoint EndPoint { get; }

        public (NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)? UnityConnection { get; internal set; }

        public Connection? PreLobbyConnection { get; private set; }

        internal ConnectionQueueEntry(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response, SteamId identity)
        {
            _type           = ConnectionType.Steam;
            SteamId         = identity;
            UnityConnection = (request, response);
        }

        internal ConnectionQueueEntry(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response, NetworkEndPoint identity)
        {
            _type           = ConnectionType.IP;
            EndPoint        = identity;
            UnityConnection = (request, response);
        }

        internal ConnectionQueueEntry(Connection preLobbyConnection, SteamId identity)
        {
            _type              = ConnectionType.Steam;
            SteamId            = identity;
            PreLobbyConnection = preLobbyConnection;
        }

        internal void DropConnection()
        {
            if (UnityConnection.HasValue)
            {
                ForcefullyDisconnectClient(ClientId!.Value);
            }

            if (PreLobbyConnection.HasValue)
            {
                PreLobbyConnection.Value.Close();
                PreLobbyConnection = null;
            }
        }

        internal void RefuseConnection([NotNull] string reason)
        {
            if (UnityConnection.HasValue)
            {
                UnityConnection.Value.response.Approved = false;
                UnityConnection.Value.response.Reason   = reason;
                UnityConnection.Value.response.Pending  = false;
            }
            else
            {
                DropConnection();
            }
        }

        internal void AcceptConnection()
        {
            if (UnityConnection.HasValue)
            {
                UnityConnection.Value.response.Approved = true;
                UnityConnection.Value.response.Pending  = false;
            }
        }

        public override string ToString()
        {
            var builder = new StringBuilder("{Name: ");

            switch (_type)
            {
                case ConnectionType.Steam:
                    if (SteamId.IsValid)
                    {
                        builder.Append(new Friend(SteamId.Value).Name);
                        builder.Append(", SteamID:").Append(SteamId.Value);
                    }
                    else
                        builder.Append("-");
                    break;
                case ConnectionType.IP:
                    builder.Append(", Endpoint: ").Append(EndPoint.Address);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            builder.Append(", ClientId: ").Append(ClientId);
            builder.Append("}");

            return builder.ToString();
        }
    }

    public enum ConnectionType
    {
        Steam,
        IP,
    }
}
