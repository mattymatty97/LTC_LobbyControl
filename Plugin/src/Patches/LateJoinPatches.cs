using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using LobbyControl.API;
using LobbyControl.Utils;
using LobbyControl.Utils.IL;
using Unity.Netcode;
using Object = UnityEngine.Object;

namespace LobbyControl.Patches;

[HarmonyPatch]
internal class LateJoinPatches
{
    public static bool _allowNewConnection;

    /// <summary>
    /// Do not check for gameHasStarted.
    /// </summary>
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.ConnectionApproval))]
    private static IEnumerable<CodeInstruction> FixConnectionApprovalPrefix(
        IEnumerable<CodeInstruction> instructions)
    {
        var codes = instructions.ToList();
        //   }
        // - else if (GameNetworkManager.Instance.gameHasStarted)
        // - {
        // -     response.Reason = "Game has already started!";
        // -     flag = false;
        // - }
        //   else if (GameNetworkManager.Instance.gameVersionNum.ToString() != strArray[0])
        var injector = new ILInjector(codes)
            .Find([
                ILMatcher.Call(typeof(GameNetworkManager).GetProperty(nameof(GameNetworkManager.Instance))?.GetMethod),
                ILMatcher.Ldfld(typeof(GameNetworkManager).GetField(nameof(GameNetworkManager.gameHasStarted),
                    BindingFlags.Instance | BindingFlags.Public)),
                ILMatcher.Opcode(OpCodes.Brfalse).CaptureOperandAs(out Label gameHasStartedLabel),
            ]);

        if (!injector.IsValid)
        {
            // print error
            LobbyControl.Log.LogWarning("ConnectionApproval patch failed!!");
            LobbyControl.Log.LogDebug(string.Join("\n", injector.ReleaseInstructions()));
            return codes;
        }

        return injector
            .RemoveLastMatch()
            .FindLabel(gameHasStartedLabel)
            .RemoveLastMatch()
            .ReleaseInstructions();
    }

    /// <summary>
    /// Handle late join requests
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.ConnectionApproval))]
    private static void HandleLateJoin(
        GameNetworkManager __instance,
        NetworkManager.ConnectionApprovalRequest request,
        NetworkManager.ConnectionApprovalResponse response)
    {
        if (!response.Approved)
            return;

        //if we're already landing
        if (!_allowNewConnection)
        {
            LobbyControl.Log.LogDebug("connection refused ( ship was landed ).");
            response.Reason = "Ship has already landed!";
            response.Approved = false;
            return;
        }

        //if lobby is closed
        if (!__instance.disableSteam &&
            (!__instance.currentLobby.HasValue || !LobbyPatcher.IsOpen(__instance.currentLobby.Value)))
        {
            LobbyControl.Log.LogDebug("connection refused ( lobby was closed ).");
            response.Reason = "Lobby has been closed!";
            response.Approved = false;
            return;
        }

        //log late joins
        if (__instance.gameHasStarted)
        {
            LobbyControl.Log.LogDebug("Incoming late connection.");
        }
    }

    /// <summary>
    ///     Make the friend invite button work again once we open the lobby.
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(QuickMenuManager), nameof(QuickMenuManager.InviteFriendsButton))]
    private static void FixFriendInviteButton(bool __runOriginal)
    {
        if (!__runOriginal)
            return;
        var manager = GameNetworkManager.Instance;
        // Only do this if the game isn't doing it by itself already.
        if (GameNetworkManager.Instance.gameHasStarted && manager.currentLobby.HasValue &&
            LobbyPatcher.IsOpen(manager.currentLobby.Value))
            GameNetworkManager.Instance.InviteFriendsUI();
    }

    /// <summary>
    ///     Prevent leaving the lobby on starting the first game.
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.LeaveLobbyAtGameStart))]
    private static bool PreventSteamLobbyLeaving(GameNetworkManager __instance)
    {
        if (!ConnectionEvents.HostHasLobbyControl)
            return true;

        LobbyControl.Log.LogDebug("Preventing the closing of Steam lobby.");
        // Do not run the method that would usually close down the lobby.
        return false;
    }

    /// <summary>
    ///     Temporarily close the lobby while a game is ongoing. This prevents people trying to join mid-game.
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPriority(Priority.Last)]
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.StartGame))]
    private static void CloseSteamLobby(StartOfRound __instance, bool __runOriginal)
    {
        if (!__runOriginal)
            return;

        if (__instance.IsServer && __instance.inShipPhase)
        {
            LobbyControl.Log.LogDebug("Setting lobby to not joinable.");
            LobbyControl.CanModifyLobby = false;
            GameNetworkManager.Instance.SetLobbyJoinable(false);

            // Remove the friend invite button in the ESC menu.
            Object.FindObjectOfType<QuickMenuManager>().inviteFriendsTextAlpha.alpha = 0f;
        }
    }

    /// <summary>
    ///     reset the status on a new Lobby
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Start))]
    private static void ResetStatus(StartOfRound __instance, bool __runOriginal)
    {
        if (!__runOriginal)
            return;

        LobbyControl.CanModifyLobby = true;
    }

    /// <summary>
    ///     Allow to reopen the steam lobby after a game has ended.
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SetShipReadyToLand))]
    [HarmonyPriority(0)]
    private static void ReopenSteamLobby(StartOfRound __instance, bool __runOriginal)
    {
        if (!__runOriginal)
            return;

        if (!ConnectionEvents.HostHasLobbyControl)
            return;

        LobbyControl.Log.LogDebug("Lobby can be re-opened");

        LobbyControl.CanModifyLobby = true;

        if (PluginConfig.SteamLobby.AutoLobby.Value)
        {
            // Restore the friend invite button in the ESC menu.
            Object.FindObjectOfType<QuickMenuManager>().inviteFriendsTextAlpha.alpha = 1f;

            var manager = GameNetworkManager.Instance;

            if (!manager.currentLobby.HasValue)
                return;

            manager.SetLobbyJoinable(true);
        }
        else
        {
            HUDManager.Instance.StartCoroutine(HudUtils.ShowTipAfterDelay("Late-Join SYSTEM",
                "To allow new players to join the lobby use \"lobby open\" in Terminal or \"auto_lobby\" in config",
                7, "LCTip_LCAutoLobby"));
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.OnPlayerConnectedClientRpc))]
    private static void ResetDcFlags(StartOfRound __instance, ulong clientId,
        int assignedPlayerObjectId)
    {
        var controllerB = __instance.allPlayerScripts[assignedPlayerObjectId];
        controllerB.disconnectedMidGame = false;
        //re-enable the player model (typically needed for back-filling players)
        controllerB.DisablePlayerModel(controllerB.gameObject, true, true);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(NetworkObject), nameof(NetworkObject.GetCachedParent))]
    public static void FixGetCachedParentNullRef(NetworkObject __instance)
    {
        //unity in their own code uses the null-coalescing operator, but that doesn't work unity lifetime checks
        if (!__instance.m_CachedParent)
        {
            //force the value to actually be null to account for that
            __instance.m_CachedParent = null;
        }
    }
}
