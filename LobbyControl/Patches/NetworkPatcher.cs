using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Mono.Cecil.Cil;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;
using OpCodes = System.Reflection.Emit.OpCodes;

namespace LobbyControl.Patches
{
    [HarmonyPatch]
    internal class NetworkPatcher
    {
        
        /// <summary>
        ///     Do not check for gameHasStarted.
        /// </summary>
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.ConnectionApproval))]
        private static IEnumerable<CodeInstruction> FixConnectionApprovalPrefix(IEnumerable<CodeInstruction> instructions)
        {
            FieldInfo gameStartedField = typeof(GameNetworkManager).GetField(nameof(GameNetworkManager.gameHasStarted));
            List<CodeInstruction> code = instructions.ToList();

            for (var index = 0; index < code.Count; index++)
            {
                var curr = code[index];
                if (curr.LoadsField(gameStartedField))
                {
                    var next = code[index + 1];
                    var prec = code[index - 1];
                    if (next.Branches(out var dest))
                    {
                        code[index - 1] = new CodeInstruction(OpCodes.Nop)
                        {
                            labels = prec.labels,
                            blocks = prec.blocks
                        };
                        code[index] = new CodeInstruction(OpCodes.Nop)
                        {
                            labels = curr.labels,
                            blocks = curr.blocks
                        };
                        code[index + 1] = new CodeInstruction(OpCodes.Br, dest)
                        {
                            labels = next.labels,
                            blocks = next.blocks
                        };
                        break;
                    }
                }
            }

            return code;
        }
        
/*
        /// <summary>
        ///     Ensure that any incoming connections are properly accepted.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.ConnectionApproval))]
        [HarmonyPriority(Priority.First)]
        private static void FixConnectionApprovalPrefix(GameNetworkManager __instance, bool __runOriginal, ref object[] __state)
        {
            __state = new object[]{GameNetworkManager.Instance.gameHasStarted};
            
            if (!__runOriginal)
                return;

            // make the function believe we haven't started the lobby yet.
            GameNetworkManager.Instance.gameHasStarted = false;
        }
    */  

        /// <summary>
        ///     Ensure that any incoming connections are properly accepted.
        /// </summary>
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameNetworkManager), nameof(GameNetworkManager.ConnectionApproval))]
        [HarmonyPriority(0)]
        private static void FixConnectionApprovalPostFix(GameNetworkManager __instance, bool __runOriginal, 
            NetworkManager.ConnectionApprovalResponse response/*, ref object[] __state*/)
        {
            /*
            // reset the started status.
            GameNetworkManager.Instance.gameHasStarted = (bool)__state[0];
            */
            
            if (!__runOriginal)
                return;
            
            //if we're allowing late connections log it
            if (__instance.gameHasStarted && response.Approved && __instance.currentLobby.HasValue && LobbyPatcher.IsOpen(__instance.currentLobby.Value))
            {
                LobbyControl.Log.LogDebug("Approving incoming late connection.");
            }
            //if not give them a valid reason
            else if (!LobbyControl.CanModifyLobby)
            {
                LobbyControl.Log.LogDebug("Late connection refused ( ship was landed ).");
                response.Reason = "Ship has already landed!";
                response.Approved = false;
            }
            else if (!__instance.currentLobby.HasValue || !LobbyPatcher.IsOpen(__instance.currentLobby.Value))
            {
                LobbyControl.Log.LogDebug("Late connection refused ( lobby was closed ).");
                response.Reason = "Lobby has been closed!";
                response.Approved = false;
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
            if (GameNetworkManager.Instance.gameHasStarted && manager.currentLobby.HasValue && LobbyPatcher.IsOpen(manager.currentLobby.Value))
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
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.EndOfGame))]
        [HarmonyPriority(0)]
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


            LobbyControl.Log.LogDebug("Lobby can be re-opened");

            LobbyControl.CanModifyLobby = true;

            if (LobbyControl.PluginConfig.SteamLobby.AutoLobby.Value)
            {
                var manager = GameNetworkManager.Instance;

                if (!manager.currentLobby.HasValue)
                    yield break;

                manager.SetLobbyJoinable(true);

                // Restore the friend invite button in the ESC menu.
                Object.FindObjectOfType<QuickMenuManager>().inviteFriendsTextAlpha.alpha = 1f;
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(StartOfRound),nameof(StartOfRound.OnClientConnect))]
        [HarmonyPriority(Priority.High)]
        private static bool PreventExtraClients(StartOfRound __instance, bool __runOriginal, ulong clientId)
        {
            if (!__runOriginal)
                return false;
            
            if (!__instance.IsServer)
                return true;

            if (__instance.connectedPlayersAmount < __instance.allPlayerObjects.Length - 1) 
                return true;
            
            LobbyControl.Log.LogWarning($"Player with ID {clientId} attempted to join a full lobby!");
            NetworkManager.Singleton.DisconnectClient(clientId);
            return false;

        }
    }
}