using System;
using System.Text;
using BepInEx;
using LethalAPI.TerminalCommands.Attributes;
using LethalAPI.TerminalCommands.Models;
using ShipLobby.Patches;
using Steamworks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ShipLobby
{
    public class LobbyCommands
    {
        private const string DefaultText = @"
- status        : prints the current lobby status
- open          : open the lobby
- close         : close the lobby
- private       : set lobby to Invite Only
- friend        : set lobby to Friends Only
- public        : set lobby to Public
- autosave      : toggle the autosave state
- save (name)   : forcefully save the lobby
- rename [name] : change the name of the lobby
";

        private static QuickMenuManager _quickMenuManager;

        [TerminalCommand("Lobby"), AllowedCaller(AllowedCaller.Host)]
        public TerminalNode Lobby()
        {
            TerminalNode node = ScriptableObject.CreateInstance<TerminalNode>();
            node.displayText = "Invalid command:" + DefaultText;
            node.clearPreviousText = true;
            node.maxCharactersToType = node.displayText.Length + 2;
            return node;
        }
        
        [TerminalCommand("Lobby"), CommandInfo("Controls the Lobby status", "[command] (lobby name)"), AllowedCaller(AllowedCaller.Host)]
        public TerminalNode Lobby([RemainingText] string text)
        {
            var node = ScriptableObject.CreateInstance<TerminalNode>();
            try
            {
                if (!StartOfRound.Instance.inShipPhase)
                {
                    node.displayText = "Can only be used while in Orbit";
                    return node;
                }

                if (!GameNetworkManager.Instance.currentLobby.HasValue)
                {
                    node.displayText = "Lobby does not exist";
                    return node;
                }

                if (text.IsNullOrWhiteSpace())
                    return this.Lobby();

                var sub = text.Trim().Split()[0].Trim().ToLower();
                var remaining = text.Substring(sub.Length).Trim();

                ShipLobby.Log.LogInfo("Command is " + text + "|" + sub + "|" + remaining);

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
                        var autoSave = ShipLobby.CanAutoSave;

                        StringBuilder builder = new StringBuilder("Lobby Status:");
                        builder.Append("\n- Name is '").Append(name).Append("'");
                        builder.Append("\n- Status is ").Append(status ? "Open" : "Closed");
                        builder.Append("\n- Visibility is ").Append(visibility.ToString());
                        builder.Append("\n- AutoSaving is ").Append(autoSave ? "On" : "Off");
                        node.displayText = builder.ToString();
                        node.maxCharactersToType = node.displayText.Length + 2;
                        break;
                    }
                    case "open":
                    {
                        if (!ShipLobby.CanModifyLobby)
                        {
                            node.displayText = "Lobby cannot be changed at the moment";
                            break;
                        }

                        ShipLobby.Log.LogDebug("Reopening lobby, setting to joinable.");
                        var manager = GameNetworkManager.Instance;
                        if (!manager.currentLobby.HasValue)
                        {
                            node.displayText = "Failed to fetch lobby ( was null )";
                            break;
                        }

                        manager.SetLobbyJoinable(true);

                        var outText = "Lobby is now Open";

                        ShipLobby.Log.LogInfo(outText);

                        // Restore the friend invite button in the ESC menu.
                        if (_quickMenuManager == null)
                            _quickMenuManager = Object.FindObjectOfType<QuickMenuManager>();
                        _quickMenuManager.inviteFriendsTextAlpha.alpha = 1f;

                        node.displayText = outText;
                        node.maxCharactersToType = node.displayText.Length + 2;
                        break;
                    }
                    case "close":
                    {
                        if (!ShipLobby.CanModifyLobby)
                        {
                            node.displayText = "Lobby cannot be changed at the moment";
                            break;
                        }

                        ShipLobby.Log.LogDebug("Closing lobby, setting to not joinable.");
                        var manager = GameNetworkManager.Instance;
                        if (!manager.currentLobby.HasValue)
                        {
                            node.displayText = "Failed to fetch lobby ( was null )";
                            break;
                        }

                        manager.SetLobbyJoinable(false);

                        var outText = "Lobby is now Closed";

                        ShipLobby.Log.LogInfo(outText);

                        // Remove the friend invite button in the ESC menu.
                        if (_quickMenuManager == null)
                            _quickMenuManager = Object.FindObjectOfType<QuickMenuManager>();
                        _quickMenuManager.inviteFriendsTextAlpha.alpha = 0f;
                        node.displayText = outText;
                        node.maxCharactersToType = node.displayText.Length + 2;
                        break;
                    }
                    case "private":
                    {
                        if (!ShipLobby.CanModifyLobby)
                        {
                            node.displayText = "Lobby cannot be changed at the moment";
                            break;
                        }

                        ShipLobby.Log.LogDebug("Locking lobby, setting to Private.");
                        var manager = GameNetworkManager.Instance;
                        if (!manager.currentLobby.HasValue)
                        {
                            node.displayText = "Failed to fetch lobby ( was null )";
                            break;
                        }

                        manager.currentLobby.Value.SetPrivate();

                        var outText = "Lobby is now Private";
                        ShipLobby.Log.LogInfo(outText);
                        node.displayText = outText;
                        node.maxCharactersToType = node.displayText.Length + 2;
                        break;
                    }
                    case "friend":
                    {
                        if (!ShipLobby.CanModifyLobby)
                        {
                            node.displayText = "Lobby cannot be changed at the moment";
                            break;
                        }

                        ShipLobby.Log.LogDebug("Locking lobby, setting to Friends Only.");
                        var manager = GameNetworkManager.Instance;
                        if (!manager.currentLobby.HasValue)
                        {
                            node.displayText = "Failed to fetch lobby ( was null )";
                            break;
                        }

                        manager.currentLobby.Value.SetPrivate();
                        manager.currentLobby.Value.SetFriendsOnly();

                        var outText = "Lobby is now Friends Only";
                        ShipLobby.Log.LogInfo(outText);
                        node.displayText = outText;
                        node.maxCharactersToType = node.displayText.Length + 2;
                        break;
                    }
                    case "public":
                    {
                        if (!ShipLobby.CanModifyLobby)
                        {
                            node.displayText = "Lobby cannot be changed at the moment";
                            break;
                        }

                        ShipLobby.Log.LogDebug("Unlocking lobby, setting to Public.");
                        var manager = GameNetworkManager.Instance;
                        if (!manager.currentLobby.HasValue)
                        {
                            node.displayText = "Failed to fetch lobby ( was null )";
                            break;
                        }

                        manager.currentLobby.Value.SetPublic();
                        var outText = "Lobby is now Public";
                        ShipLobby.Log.LogInfo(outText);

                        node.displayText = outText;
                        node.maxCharactersToType = node.displayText.Length + 2;
                        break;
                    }
                    case "autosave":
                    {

                        ShipLobby.Log.LogDebug("Toggling AutoSave");

                        ShipLobby.CanAutoSave = !ShipLobby.CanAutoSave;

                        var outText = "AutoSaving is now " + (ShipLobby.CanAutoSave ? "On" : "Off");

                        ShipLobby.Log.LogInfo(outText);

                        node.displayText = outText;
                        node.maxCharactersToType = node.displayText.Length + 2;
                        break;
                    }
                    case "save":
                    {
                        if (!ShipLobby.CanModifyLobby)
                        {
                            node.displayText = "Lobby cannot be saved at the moment";
                            break;
                        }

                        ShipLobby.Log.LogDebug("Saving Lobby");
                        var manager = GameNetworkManager.Instance;
                        if (!manager.currentLobby.HasValue)
                        {
                            node.displayText = "Failed to fetch lobby ( was null )";
                            break;
                        }

                        var oldSaveFileName = manager.currentSaveFileName;
                        if (!remaining.IsNullOrWhiteSpace())
                        {
                            if (remaining.Length > 20)
                                remaining = remaining.Substring(0, 20);
                            manager.currentSaveFileName = remaining;
                        }

                        HUDManager.Instance.saveDataIconAnimatorB.SetTrigger("save");
                        manager.SaveGame();
                        var outText = "Lobby Saved to " + manager.currentSaveFileName;

                        manager.currentSaveFileName = oldSaveFileName;

                        ShipLobby.Log.LogInfo(outText);

                        node.displayText = outText;
                        node.maxCharactersToType = node.displayText.Length + 2;
                        break;
                    }
                    case "rename":
                    {
                        if (remaining.IsNullOrWhiteSpace())
                        {
                            node.displayText = "Lobby name cannot be null";
                            break;
                        }

                        if (!ShipLobby.CanModifyLobby)
                        {
                            node.displayText = "Lobby cannot be changed at the moment";
                            break;
                        }

                        if (remaining.Length > 40)
                            remaining = remaining.Substring(0, 40);

                        ShipLobby.Log.LogDebug("Renaming lobby: \"" + remaining + "\"");
                        var manager = GameNetworkManager.Instance;
                        if (!manager.currentLobby.HasValue)
                        {
                            node.displayText = "Failed to fetch lobby ( was null )";
                            break;
                        }

                        manager.lobbyHostSettings.lobbyName = remaining;
                        manager.currentLobby.Value.SetData("name", manager.lobbyHostSettings.lobbyName.ToString());
                        manager.steamLobbyName = manager.currentLobby.Value.GetData("name");
                        
                        ES3.Save<string>("HostSettings_Name", manager.steamLobbyName,"LCGeneralSaveData");

                        var outText = "Lobby renamed to \"" + remaining + "\"";
                        ShipLobby.Log.LogInfo(outText);

                        // Remove the friend invite button in the ESC menu.
                        if (_quickMenuManager == null)
                            _quickMenuManager = Object.FindObjectOfType<QuickMenuManager>();
                        _quickMenuManager.inviteFriendsTextAlpha.alpha = 0f;
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
                        node = this.Lobby();
                        break;
                }
            }
            catch (Exception ex)
            {
                ShipLobby.Log.LogError("Exception:\n" + ex.ToString());
                node.displayText = "Exception!";
            }
            return node;
        }
    }
}