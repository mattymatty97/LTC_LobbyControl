using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx;
using GameNetcodeStuff;
using LethalAPI.LibTerminal.Attributes;
using LethalAPI.LibTerminal.Models;
using LethalAPI.LibTerminal.Models.Enums;
using LobbyControl.Patches;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable MemberCanBeMadeStatic.Global

namespace LobbyControl
{
    public class LobbyCommands
    {
        private const string DefaultText = @"
- status        : prints the current lobby status

Steam:
- open          : open the lobby
- close         : close the lobby
- private       : set lobby to Invite Only
- friend        : set lobby to Friends Only
- public        : set lobby to Public
- rename [name] : change the name of the lobby

Saving:
- autosave      : toggle the autosave state
- save (name)   : forcefully save the lobby
- load (name)   : reload the lobby

Extra:
- namefix       : refresh player names for the entire lobby
- dropall       : force all players to drop their inventory
";

        [TerminalCommand("Lobby")]
        [AllowedCaller(AllowedCaller.Host)]
        public TerminalNode Lobby()
        {
            var node = ScriptableObject.CreateInstance<TerminalNode>();
            node.displayText = "Invalid command:" + DefaultText;
            node.clearPreviousText = true;
            node.maxCharactersToType = node.displayText.Length + 2;
            return node;
        }

        [TerminalCommand("Lobby")]
        [CommandInfo("type lobby help for more info", "[command] (lobby name)")]
        [AllowedCaller(AllowedCaller.Host)]
        public TerminalNode Lobby([RemainingText] string text)
        {
            var node = ScriptableObject.CreateInstance<TerminalNode>();
            try
            {
                if (text.IsNullOrWhiteSpace())
                    return Lobby();

                var sub = text.Trim().Split()[0].Trim().ToLower();
                var remaining = text.Substring(sub.Length).Trim();

                LobbyControl.Log.LogInfo("Command is " + text + "|" + sub + "|" + remaining);

                switch (sub)
                {
                    case "status":
                    {
                        var manager = GameNetworkManager.Instance;
                        if (!manager.currentLobby.HasValue)
                        {
                            node.displayText = "Failed to fetch lobby ( was null )";
                            break;
                        }

                        var lobby = manager.currentLobby.Value;
                        var status = LobbyPatcher.IsOpen(lobby);
                        var visibility = LobbyPatcher.GetVisibility(lobby);
                        var name = lobby.GetData("name");
                        var autoSave = LobbyControl.CanSave;

                        var builder = new StringBuilder("Lobby Status:");
                        builder.Append("\n- File is '").Append(manager.currentSaveFileName).Append("'");
                        builder.Append("\n- Name is '").Append(name).Append("'");
                        builder.Append("\n- Status is ").Append(status ? "Open" : "Closed");
                        builder.Append("\n- Visibility is ").Append(visibility.ToString());
                        builder.Append("\n- Saving is ").Append(autoSave ? "Automatic" : "Manual");
                        node.displayText = builder.ToString();
                        node.maxCharactersToType = node.displayText.Length + 2;
                        break;
                    }
                    case "open":
                    {
                        if (PerformOrbitCheck(node, out var errorText)) 
                            return errorText;
                        
                        if (!LobbyControl.CanModifyLobby)
                        {
                            node.displayText = "Lobby cannot be changed at the moment";
                            break;
                        }

                        LobbyControl.Log.LogDebug("Reopening lobby, setting to joinable.");
                        var manager = GameNetworkManager.Instance;
                        if (!manager.currentLobby.HasValue)
                        {
                            node.displayText = "Failed to fetch lobby ( was null )";
                            break;
                        }

                        manager.SetLobbyJoinable(true);
                        
                        // Restore the friend invite button in the ESC menu.
                        Object.FindObjectOfType<QuickMenuManager>().inviteFriendsTextAlpha.alpha = 1f;

                        var outText = "Lobby is now Open";

                        LobbyControl.Log.LogInfo(outText);

                        node.displayText = outText;
                        node.maxCharactersToType = node.displayText.Length + 2;
                        break;
                    }
                    case "close":
                    {
                        if (PerformOrbitCheck(node, out var errorText)) 
                            return errorText;
                        
                        if (!LobbyControl.CanModifyLobby)
                        {
                            node.displayText = "Lobby cannot be changed at the moment";
                            break;
                        }

                        LobbyControl.Log.LogDebug("Closing lobby, setting to not joinable.");
                        var manager = GameNetworkManager.Instance;
                        if (!manager.currentLobby.HasValue)
                        {
                            node.displayText = "Failed to fetch lobby ( was null )";
                            break;
                        }

                        manager.SetLobbyJoinable(false);
                        
                        // Hide the friend invite button in the ESC menu.
                        Object.FindObjectOfType<QuickMenuManager>().inviteFriendsTextAlpha.alpha = 0f;

                        var outText = "Lobby is now Closed";

                        LobbyControl.Log.LogInfo(outText);

                        node.displayText = outText;
                        node.maxCharactersToType = node.displayText.Length + 2;
                        break;
                    }
                    case "private":
                    {
                        if (PerformOrbitCheck(node, out var errorText)) 
                            return errorText;
                        
                        if (!LobbyControl.CanModifyLobby)
                        {
                            node.displayText = "Lobby cannot be changed at the moment";
                            break;
                        }

                        LobbyControl.Log.LogDebug("Locking lobby, setting to Private.");
                        var manager = GameNetworkManager.Instance;
                        if (!manager.currentLobby.HasValue)
                        {
                            node.displayText = "Failed to fetch lobby ( was null )";
                            break;
                        }

                        manager.currentLobby.Value.SetPrivate();

                        var outText = "Lobby is now Private";
                        LobbyControl.Log.LogInfo(outText);
                        node.displayText = outText;
                        node.maxCharactersToType = node.displayText.Length + 2;
                        break;
                    }
                    case "friend":
                    {
                        if (PerformOrbitCheck(node, out var errorText)) 
                            return errorText;
                        
                        if (!LobbyControl.CanModifyLobby)
                        {
                            node.displayText = "Lobby cannot be changed at the moment";
                            break;
                        }

                        LobbyControl.Log.LogDebug("Locking lobby, setting to Friends Only.");
                        var manager = GameNetworkManager.Instance;
                        if (!manager.currentLobby.HasValue)
                        {
                            node.displayText = "Failed to fetch lobby ( was null )";
                            break;
                        }

                        manager.currentLobby.Value.SetPrivate();
                        manager.currentLobby.Value.SetFriendsOnly();

                        var outText = "Lobby is now Friends Only";
                        LobbyControl.Log.LogInfo(outText);
                        node.displayText = outText;
                        node.maxCharactersToType = node.displayText.Length + 2;
                        break;
                    }
                    case "public":
                    {
                        if (PerformOrbitCheck(node, out var errorText)) 
                            return errorText;
                        
                        if (!LobbyControl.CanModifyLobby)
                        {
                            node.displayText = "Lobby cannot be changed at the moment";
                            break;
                        }

                        LobbyControl.Log.LogDebug("Unlocking lobby, setting to Public.");
                        var manager = GameNetworkManager.Instance;
                        if (!manager.currentLobby.HasValue)
                        {
                            node.displayText = "Failed to fetch lobby ( was null )";
                            break;
                        }

                        manager.currentLobby.Value.SetPublic();
                        var outText = "Lobby is now Public";
                        LobbyControl.Log.LogInfo(outText);

                        node.displayText = outText;
                        node.maxCharactersToType = node.displayText.Length + 2;
                        break;
                    }
                    case "rename":
                    {
                        if (PerformOrbitCheck(node, out var errorText)) 
                            return errorText;
                        
                        if (remaining.IsNullOrWhiteSpace())
                        {
                            node.displayText = "Lobby name cannot be null";
                            break;
                        }

                        if (!LobbyControl.CanModifyLobby)
                        {
                            node.displayText = "Lobby cannot be changed at the moment";
                            break;
                        }

                        if (remaining.Length > 40)
                            remaining = remaining.Substring(0, 40);

                        LobbyControl.Log.LogDebug("Renaming lobby: \"" + remaining + "\"");
                        var manager = GameNetworkManager.Instance;
                        if (!manager.currentLobby.HasValue)
                        {
                            node.displayText = "Failed to fetch lobby ( was null )";
                            break;
                        }

                        manager.lobbyHostSettings.lobbyName = remaining;
                        manager.currentLobby.Value.SetData("name", manager.lobbyHostSettings.lobbyName);
                        manager.steamLobbyName = manager.currentLobby.Value.GetData("name");

                        ES3.Save<string>("HostSettings_Name", manager.steamLobbyName, "LCGeneralSaveData");

                        var outText = "Lobby renamed to \"" + remaining + "\"";
                        LobbyControl.Log.LogInfo(outText);

                        node.displayText = outText;
                        node.maxCharactersToType = node.displayText.Length + 2;
                        break;
                    }
                    case "autosave":
                    {
                        LobbyControl.Log.LogDebug("Toggling AutoSave");

                        LobbyControl.CanSave = LobbyControl.AutoSaveEnabled = !LobbyControl.AutoSaveEnabled;

                        var outText = "AutoSaving is now " + (LobbyControl.CanSave ? "On" : "Off");

                        LobbyControl.Log.LogInfo(outText);

                        node.displayText = outText;
                        node.maxCharactersToType = node.displayText.Length + 2;
                        break;
                    }
                    case "save":
                    {
                        if (PerformOrbitCheck(node, out var errorText)) 
                            return errorText;
                        
                        if (!LobbyControl.CanModifyLobby)
                        {
                            node.displayText = "Lobby cannot be saved at the moment";
                            break;
                        }

                        if (StartOfRound.Instance.isChallengeFile)
                        {
                            node.displayText = "Cannot Edit Challenge Save";
                            return node;
                        }

                        LobbyControl.Log.LogDebug("Saving Lobby");
                        var manager = GameNetworkManager.Instance;

                        var oldSaveFileName = manager.currentSaveFileName;
                        if (!remaining.IsNullOrWhiteSpace())
                        {
                            if (remaining.Length > 20)
                                remaining = remaining.Substring(0, 20);
                            manager.currentSaveFileName = remaining;
                        }

                        if (oldSaveFileName != manager.currentSaveFileName)
                            ES3.CopyFile(oldSaveFileName, manager.currentSaveFileName);

                        LobbyControl.CanSave = true;
                        HUDManager.Instance.saveDataIconAnimatorB.SetTrigger("save");
                        manager.SaveGame();
                        var outText = "Lobby Saved to " + manager.currentSaveFileName;
                        LobbyControl.CanSave = LobbyControl.AutoSaveEnabled;
                        manager.currentSaveFileName = oldSaveFileName;

                        LobbyControl.Log.LogInfo(outText);

                        node.displayText = outText;
                        node.maxCharactersToType = node.displayText.Length + 2;
                        break;
                    }
                    case "load":
                    {
                        if (PerformOrbitCheck(node, out var errorText)) 
                            return errorText;
                        
                        if (!LobbyControl.CanModifyLobby)
                        {
                            node.displayText = "Lobby cannot be modified at the moment";
                            break;
                        }

                        if (StartOfRound.Instance.isChallengeFile)
                        {
                            node.displayText = "Cannot Edit Challenge Save";
                            return node;
                        }

                        LobbyControl.Log.LogDebug("Reloading Lobby");
                        var manager = GameNetworkManager.Instance;

                        var oldSaveFileName = manager.currentSaveFileName;
                        if (!remaining.IsNullOrWhiteSpace())
                        {
                            if (remaining.Length > 20)
                                remaining = remaining.Substring(0, 20);
                            manager.currentSaveFileName = remaining;
                        }

                        var outText = "Lobby \'" + manager.currentSaveFileName + "\' loaded";

                        if (ES3.FileExists(manager.currentSaveFileName))
                        {
                            StartOfRound.Instance.StartCoroutine(LobbyControl.LoadLobbyCoroutine());
                            LobbyControl.AutoSaveEnabled = LobbyControl.CanSave = ES3.Load("LC_SavingMethod",
                                GameNetworkManager.Instance.currentSaveFileName, true);
                            LobbyControl.Log.LogInfo(outText);
                        }
                        else
                        {
                            outText = "Lobby \'" + manager.currentSaveFileName + "\' does not Exist";
                            LobbyControl.Log.LogError(outText);
                            manager.currentSaveFileName = oldSaveFileName;
                        }

                        node.displayText = outText;
                        node.maxCharactersToType = node.displayText.Length + 2;
                        break;
                    }
                    case "namefix":
                    {
                       
                        LobbyControl.Log.LogDebug("Fixing names");

                        var outText = "Player names Reloaded";

                        var localController = StartOfRound.Instance.localPlayerController;
                        var scripts = StartOfRound.Instance.allPlayerScripts;
                        List<ulong> ulongList = new List<ulong>();
                        foreach (var script in scripts)
                        {
                            if (script.IsOwnedByServer && localController != script )
                                ulongList.Add(0L);
                            else
                                ulongList.Add(script.playerSteamId);
                        }
                        localController.SendNewPlayerValuesClientRpc(ulongList.ToArray());

                        node.displayText = outText;
                        node.maxCharactersToType = node.displayText.Length + 2;
                        break;
                    }
                    case "dropall":
                    {
                        
                        if (PerformOrbitCheck(node, out var errorText)) 
                            return errorText;
                        
                        LobbyControl.Log.LogDebug("Dropping all Items");

                        var outText = "All Items Dropped";

                        foreach (var playerScript in StartOfRound.Instance.allPlayerScripts)
                        {
                            playerScript.DropAllHeldItemsAndSync();
                        }

                        node.displayText = outText;
                        node.maxCharactersToType = node.displayText.Length + 2;
                        break;
                    }
                    case "help":
                    {
                        node.displayText = DefaultText;
                        node.maxCharactersToType = node.displayText.Length + 2;
                        node.clearPreviousText = true;
                        break;
                    }
                    default:
                        node = Lobby();
                        break;
                }
            }
            catch (Exception ex)
            {
                LobbyControl.Log.LogError("Exception:\n" + ex);
                node.displayText = "Exception!";
            }

            return node;

            bool PerformOrbitCheck(TerminalNode terminalNode, out TerminalNode errorText)
            {
                if (!StartOfRound.Instance.inShipPhase)
                {
                    terminalNode.displayText = "Can only be used while in Orbit";
                    {
                        errorText = terminalNode;
                        return true;
                    }
                }

                errorText = null;
                return false;
            }
        }
    }
}