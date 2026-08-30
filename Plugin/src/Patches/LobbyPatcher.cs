using System.Collections.Generic;
using HarmonyLib;
using LobbyControl.API;
using Steamworks;
using Steamworks.Data;

namespace LobbyControl.Patches;

[HarmonyPatch]
internal class LobbyPatcher
{
    private const string LobbyOwnerIdStringDataKey = LobbyControl.GUID + ".LobbyOwnerIdString";
    private static readonly Dictionary<Lobby, LobbyType> Visibility = [];
    private static readonly Dictionary<Lobby, bool> Open = [];

    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameNetworkManager), "SteamMatchmaking_OnLobbyCreated")]
    private static void SteamMatchmaking_OnLobbyCreated(Result result, Lobby lobby)
    {
        if (result != Result.OK)
            return;

        lobby.SetData(LobbyOwnerIdStringDataKey, lobby.Owner.Id.ToString());
        ConnectionEvents.HostHasLobbyControl = true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.StartClient))]
    private static void StartClient(GameNetworkManager __instance, SteamId id)
    {
        ConnectionEvents.HostHasLobbyControl =
            __instance.currentLobby.HasValue &&
            __instance.currentLobby.Value.GetData(LobbyOwnerIdStringDataKey) == id.ToString();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Lobby), nameof(Lobby.SetJoinable))]
    private static void TrackOpenStatus(Lobby __instance, object[] __args, bool __runOriginal)
    {
        if (!__runOriginal)
            return;
        Open[__instance] = (bool)__args[0];
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Lobby), nameof(Lobby.SetPublic))]
    private static void TrackPublicStatus(Lobby __instance, bool __runOriginal)
    {
        if (!__runOriginal)
            return;
        Visibility[__instance] = LobbyType.Public;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Lobby), nameof(Lobby.SetPrivate))]
    private static void TrackPrivateStatus(Lobby __instance, bool __runOriginal)
    {
        if (!__runOriginal)
            return;
        Visibility[__instance] = LobbyType.Private;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Lobby), nameof(Lobby.SetFriendsOnly))]
    private static void trackFriendsOnly(Lobby __instance, bool __runOriginal)
    {
        if (!__runOriginal)
            return;
        Visibility[__instance] = LobbyType.FriendsOnly;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Lobby), nameof(Lobby.SetData))]
    private static void trackData(Lobby __instance, bool __runOriginal, string key, string value)
    {
        if (!__runOriginal)
            return;
    }

    public static LobbyType GetVisibility(Lobby lobby)
    {
        return Visibility.ContainsKey(lobby) ? Visibility[lobby] : LobbyType.Private;
    }

    public static bool IsOpen(Lobby lobby)
    {
        return !Open.ContainsKey(lobby) || Open.GetValueSafe(lobby);
    }
}