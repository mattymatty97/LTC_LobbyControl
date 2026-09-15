using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using LobbyControl.Utils;
using LobbyControl.Utils.IL;
using Netcode.Transports.Facepunch;
using Steamworks;
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
    ///     Prevent leaving the lobby on starting the first game.
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.LeaveLobbyAtGameStart))]
    private static bool PreventSteamLobbyLeaving(GameNetworkManager __instance)
    {
        LobbyControl.Log.LogDebug("Preventing the closing of Steam lobby.");
        // Do not run the method that would usually close down the lobby.
        return false;
    }

    /// <summary>
    ///     Automatically leave the Steam lobby when the host leaves.
    ///     This ensures that clients don't remain in a lobby when the game host has disconnected.
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.SteamMatchmaking_OnLobbyMemberLeave))]
    private static void LeaveLobbyIfHostLeaves(GameNetworkManager __instance, Friend friend)
    {
        if (NetworkManager.Singleton.NetworkConfig.NetworkTransport is not FacepunchTransport transport)
            return;

        if (friend.Id != transport.targetSteamId)
            return;

        LobbyControl.Log.LogDebug("Host left the lobby, leaving automatically too.");
        __instance.LeaveCurrentSteamLobby();
    }

    /// <summary>
    ///     Temporarily close the lobby while a game is ongoing. This prevents people from trying to join mid-game.
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPriority(Priority.Last)]
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.StartGame))]
    private static void CloseSteamLobby(StartOfRound __instance, bool __runOriginal)
    {
        if (!__runOriginal)
            return;

        if (!__instance.IsServer)
            return;

        if (!__instance.inShipPhase)
            return;

        LobbyControl.Log.LogDebug("Setting lobby to not joinable.");
        LobbyControl.CanModifyLobby = false;
        GameNetworkManager.Instance.SetLobbyJoinable(false);
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

        LobbyControl.CanModifyLobby = __instance.IsServer;
    }

    /// <summary>
    ///     Allow reopening the steam lobby after a game has ended.
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SetShipReadyToLand))]
    [HarmonyPriority(0)]
    private static void ReopenSteamLobby(StartOfRound __instance, bool __runOriginal)
    {
        if (!__runOriginal)
            return;

        if (!__instance.IsServer)
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


    /// <summary>
    ///     Manages the visibility of the friend invite button based on the current lobby state.
    ///     Shows the invite button when the lobby is open and joinable, hides it otherwise.
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPatch(typeof(QuickMenuManager), nameof(QuickMenuManager.OpenQuickMenu))]
    private static void ManageFriendInviteButtonVisibility(QuickMenuManager __instance, bool __runOriginal)
    {
        if (!__runOriginal)
            return;

        var currentLobby = GameNetworkManager.Instance.currentLobby;

        if (currentLobby.HasValue && LobbyPatcher.IsOpen(currentLobby.Value))
        {
            __instance.inviteFriendsTextAlpha.alpha = 1f;
        }
        else
        {
            __instance.DisableInviteFriendsButton();
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

    [HarmonyTranspiler]
    [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.OnPlayerConnectedClientRpc))]
    private static IEnumerable<CodeInstruction> FixAlivePlayerLoop(IEnumerable<CodeInstruction> instructions, ILGenerator ilGenerator)
    {
        var codes = instructions.ToList();

        var connectedPlayersAmountFieldInfo = typeof(StartOfRound).GetField(nameof(StartOfRound.connectedPlayersAmount), AccessTools.all);
        var allPlayerScriptsFieldInfo = typeof(StartOfRound).GetField(nameof(StartOfRound.allPlayerScripts), AccessTools.all);
        var arrayLengthPropertyInfo = typeof(Array).GetProperty(nameof(Array.Length), AccessTools.all);

        // - for (int index = 0; index < this.connectedPlayersAmount + 1; ++index)
        // + for (int index = 0; index < this.allPlayerScripts.Length; ++index)
        // = {
        // =     if (index == 0 || !this.allPlayerScripts[index].IsOwnedByServer)
        // =         this.allPlayerScripts[index].isPlayerControlled = true;
        // = }
        var injector = new ILInjector(codes)
            .Find([
                ILMatcher.Ldloc().CaptureAs(out var indexInstruction),
                ILMatcher.Ldarg(0),
                ILMatcher.Ldfld(connectedPlayersAmountFieldInfo),
                ILMatcher.Ldc(1),
                ILMatcher.Opcode(OpCodes.Add),
                ILMatcher.Branch().CaptureAs(out var exitInstruction)
            ]);

        if (!injector.IsValid)
        {
            // print error
            LobbyControl.Log.LogWarning("OnPlayerConnectedClientRpc patch failed!!");
            LobbyControl.Log.LogDebug(string.Join("\n", injector.ReleaseInstructions()));
            return codes;
        }

        return injector
            .RemoveLastMatch()
            .Insert([
                indexInstruction,
                InstructionUtilities.MakeLdarg(0),
                new CodeInstruction(OpCodes.Ldfld, allPlayerScriptsFieldInfo),
                new CodeInstruction(OpCodes.Call, arrayLengthPropertyInfo!.GetMethod),
                exitInstruction
            ])
            .ReleaseInstructions();
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
