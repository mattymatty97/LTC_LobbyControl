using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using LobbyControl.Networking;
using Steamworks;
using Steamworks.Data;
using Unity.Netcode;

namespace LobbyControl.Patches;

[HarmonyPatch]
internal class LobbyPatcher
{
    internal static readonly Dictionary<Lobby, LobbyType> Visibility = [];
    internal static readonly Dictionary<Lobby, bool> Open = [];

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Lobby), nameof(Lobby.SetJoinable))]
    private static void TrackOpenStatus(Lobby __instance, bool b, bool __runOriginal)
    {
        if (!__runOriginal)
            return;

        if (!__instance.IsOwnedBy(SteamClient.SteamId))
            return;

        Open[__instance] = b;

        var targets = NetworkManager.Singleton.ConnectedClientsIds.ToList();
        targets.Remove(NetworkManager.Singleton.LocalClientId);
        NamedMessages.LobbyStatusClientRpc(b, GetVisibility(__instance), targets);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Lobby), nameof(Lobby.SetPublic))]
    private static void TrackPublicStatus(Lobby __instance, bool __runOriginal)
    {
        if (!__runOriginal)
            return;

        if (!__instance.IsOwnedBy(SteamClient.SteamId))
            return;

        Visibility[__instance] = LobbyType.Public;

        var targets = NetworkManager.Singleton.ConnectedClientsIds.ToList();
        targets.Remove(NetworkManager.Singleton.LocalClientId);
        NamedMessages.LobbyStatusClientRpc(IsOpen(__instance), LobbyType.Public, targets);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Lobby), nameof(Lobby.SetPrivate))]
    private static void TrackPrivateStatus(Lobby __instance, bool __runOriginal)
    {
        if (!__runOriginal)
            return;

        if (!__instance.IsOwnedBy(SteamClient.SteamId))
            return;

        Visibility[__instance] = LobbyType.Private;

        var targets = NetworkManager.Singleton.ConnectedClientsIds.ToList();
        targets.Remove(NetworkManager.Singleton.LocalClientId);
        NamedMessages.LobbyStatusClientRpc(IsOpen(__instance), LobbyType.Private, targets);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Lobby), nameof(Lobby.SetFriendsOnly))]
    private static void trackFriendsOnly(Lobby __instance, bool __runOriginal)
    {
        if (!__runOriginal)
            return;

        if (!__instance.IsOwnedBy(SteamClient.SteamId))
            return;

        Visibility[__instance] = LobbyType.FriendsOnly;

        var targets = NetworkManager.Singleton.ConnectedClientsIds.ToList();
        targets.Remove(NetworkManager.Singleton.LocalClientId);
        NamedMessages.LobbyStatusClientRpc(IsOpen(__instance), LobbyType.FriendsOnly, targets);
    }

    public static LobbyType GetVisibility(Lobby lobby)
    {
        return Visibility.GetValueOrDefault(lobby, LobbyType.Private);
    }

    public static bool IsOpen(Lobby lobby)
    {
        return !Open.ContainsKey(lobby) || Open.GetValueSafe(lobby);
    }
}
