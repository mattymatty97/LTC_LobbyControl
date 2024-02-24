using System.Collections;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LobbyControl.Patches
{
    [HarmonyPatch]
    internal class NetworkPatcher
    {
        private static QuickMenuManager _quickMenuManager;

        /// <summary>
        ///     Ensure that any incoming connections are properly accepted.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.ConnectionApproval))]
        private static void FixConnectionApproval(GameNetworkManager __instance,
            NetworkManager.ConnectionApprovalResponse response, bool __runOriginal)
        {
            // Only override refusals that are due to the current game state being set to "has already started".
            if (!__runOriginal || response.Approved || response.Reason != "Game has already started!")
                return;

            if (__instance.gameHasStarted && StartOfRound.Instance.inShipPhase)
            {
                LobbyControl.Log.LogDebug("Approving incoming late connection.");
                response.Reason = "";
                response.Approved = true;
            }
        }

        /// <summary>
        ///     Make the friend invite button work again once we are back in orbit.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(QuickMenuManager), nameof(QuickMenuManager.InviteFriendsButton))]
        private static void FixFriendInviteButton(bool __runOriginal)
        {
            if (!__runOriginal)
                return;
            
            // Only do this if the game isn't doing it by itself already.
            if (GameNetworkManager.Instance.gameHasStarted && StartOfRound.Instance.inShipPhase)
                GameNetworkManager.Instance.InviteFriendsUI();
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
        ///     Temporarily close the lobby while a game is ongoing. This prevents people trying to join mid-game.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.StartGame))]
        private static void CloseSteamLobby(StartOfRound __instance)
        {
            if (__instance.IsServer && __instance.inShipPhase)
            {
                LobbyControl.Log.LogDebug("Setting lobby to not joinable.");
                LobbyControl.CanModifyLobby = false;
                GameNetworkManager.Instance.SetLobbyJoinable(false);

                // Remove the friend invite button in the ESC menu.
                if (_quickMenuManager == null)
                    _quickMenuManager = Object.FindObjectOfType<QuickMenuManager>();
                _quickMenuManager.inviteFriendsTextAlpha.alpha = 0f;
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
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.EndOfGame))]
        private static IEnumerator ReopenSteamLobby(IEnumerator coroutine, StartOfRound __instance, bool __runOriginal)
        {
            if (!__runOriginal)
                yield break;
            
            // The method we're patching here is a coroutine. Fully exhaust it before adding our code.
            while (coroutine.MoveNext())
                yield return coroutine.Current;
            // At this point all players should have been revived and the stats screen should have been shown.

            // Nothing to do at all if this is not the host.
            if (!__instance.IsServer)
                yield break;

            // The "getting fired" cutscene runs in a separate coroutine. Ensure we don't open the lobby until after
            // it is over.
            yield return new WaitForSeconds(0.5f);
            yield return new WaitUntil(() => !__instance.firingPlayersCutsceneRunning);


            LobbyControl.Log.LogDebug("Lobby can be re-openned");

            LobbyControl.CanModifyLobby = true;

            // Restore the friend invite button in the ESC menu.
            if (_quickMenuManager == null)
                _quickMenuManager = Object.FindObjectOfType<QuickMenuManager>();
            _quickMenuManager.inviteFriendsTextAlpha.alpha = 1f;
        }
        
        /// <summary>
        ///     Skip handling the new Connection if it comes from us and we are the Host.
        ///     Should only happen if we're trying to reLoad the lobby
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.OnPlayerConnectedClientRpc))]
        private static bool SkipLocalReconnect(StartOfRound __instance, ulong clientId)
        {
            return !(__instance.IsServer && __instance.__rpc_exec_stage == NetworkBehaviour.__RpcExecStage.Client &&
                     clientId == __instance.localPlayerController.playerClientId);
        }
    }
}