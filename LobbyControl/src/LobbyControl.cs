using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using LobbyControl.PopUp;
using LobbyControl.TerminalCommands;
using PluginInfo = BepInEx.PluginInfo;

namespace LobbyControl
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency("com.github.tinyhoot.ShipLobby", Flags:BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("twig.latecompany", Flags:BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.potatoepet.AdvancedCompany", Flags:BepInDependency.DependencyFlags.SoftDependency)]
    internal class LobbyControl : BaseUnityPlugin
    {
        public const string GUID = "mattymatty.LobbyControl";
        public const string NAME = "LobbyControl";
        public const string VERSION = "2.3.0";

        internal static ManualLogSource Log;

        public static bool CanModifyLobby = true;

        public static bool CanSave = true;
        public static bool AutoSaveEnabled = true;


        private static readonly string[] IncompatibleGUIDs = new string[]
        {
            "com.github.tinyhoot.ShipLobby",
            "twig.latecompany",
            "com.potatoepet.AdvancedCompany"
        };

        internal static readonly List<PluginInfo> FoundIncompatibilities = new List<PluginInfo>();
            
        private void Awake()
        {
            Log = Logger;
            try
            {
                PluginInfo[] incompatibleMods = Chainloader.PluginInfos.Values.Where(p => IncompatibleGUIDs.Contains(p.Metadata.GUID)).ToArray();
                if (incompatibleMods.Length > 0)
                {    
                    FoundIncompatibilities.AddRange(incompatibleMods);
                    foreach (var mod in incompatibleMods)
                    {
                        Log.LogWarning($"{mod.Metadata.Name} is incompatible!");   
                    }
                    Log.LogError($"{incompatibleMods.Length} incompatible mods found! Disabling!");
                    var harmony = new Harmony(GUID);
                    harmony.PatchAll(typeof(OnDisablePatch));
                }
                else
                {
                    Log.LogInfo("Initializing Configs");

                    PluginConfig.Init(this);
                    
                    CommandManager.Initialize();
                    
                    Log.LogInfo("Patching Methods");
                    var harmony = new Harmony(GUID);
                    harmony.PatchAll(Assembly.GetExecutingAssembly());
                    
                    Log.LogInfo(NAME + " v" + VERSION + " Loaded!");
                }
            }
            catch (Exception ex)
            {
                Log.LogError("Exception while initializing: \n" + ex);
            }
        }


        internal static class PluginConfig
        {
            internal static void Init(BaseUnityPlugin plugin)
            {
                var config = plugin.Config;
                //Initialize Configs
                //ItemSync
                ItemSync.GhostItems = config.Bind("ItemSync","ghost_items",true
                    ,"prevent the creation of non-grabbable items in case of inventory desync");
                ItemSync.ForceDrop = config.Bind("ItemSync","force_drop",true
                    ,"forcefully drop all items of the player causing the desync");
                ItemSync.ShotGunReload = config.Bind("ItemSync","shotgun_reload",true
                    ,"prevent the shotgun disappearing when reloading it");
                ItemSync.SyncOnUse = config.Bind("ItemSync","sync_on_use",false
                    ,"sync held object upon usage");
                ItemSync.SyncOnInteract = config.Bind("ItemSync","sync_on_interact",false
                    ,"sync held object upon interaction");
                ItemSync.SyncIgnoreBattery = config.Bind("ItemSync","sync_ignore_battery",true
                    ,"ignore battery powered items (compatibility with flashlight toggle mods)");
                //SaveLimit
                SaveLimit.Enabled = config.Bind("SaveLimit","enabled",true
                    ,"remove the limit to the amount of items that can be saved");
                //InvisiblePlayer
                InvisiblePlayer.Enabled = config.Bind("InvisiblePlayer","enabled",true
                    ,"attempts to fix late joining players appearing invisible to the rest of the lobby");
                //SteamLobby
                SteamLobby.AutoLobby = config.Bind("SteamLobby","auto_lobby",false
                    ,"automatically reopen the lobby as soon as you reach orbit");
                //LogSpam
                LogSpam.Enabled = config.Bind("LogSpam","enabled",true
                    ,"prevent some annoying log spam");
                LogSpam.CalculatePolygonPath = config.Bind("LogSpam","CalculatePolygonPath",true
                    ,"stop pathfinding for dead Enemies");
                LogSpam.MoreCompany = config.Bind("LogSpam","more_company",true
                    ,"Remove some leftover Exceptions caused by MoreCompany");
                //JoinQueue
                JoinQueue.Enabled = config.Bind("JoinQueue","enabled",true
                    ,"handle joining players as a queue instead of at the same time");
                JoinQueue.ConnectionTimeout = config.Bind("JoinQueue","connection_timeout_ms",3000L
                    ,"After how much time discard a hanging connection");
                JoinQueue.ConnectionDelay = config.Bind("JoinQueue","connection_delay_ms",500L
                    ,"Delay between each successful connection");

                //remove unused options
                PropertyInfo orphanedEntriesProp = config.GetType().GetProperty("OrphanedEntries", BindingFlags.NonPublic | BindingFlags.Instance);

                var orphanedEntries = (Dictionary<ConfigDefinition, string>)orphanedEntriesProp!.GetValue(config, null);

                orphanedEntries.Clear(); // Clear orphaned entries (Unbinded/Abandoned entries)
                config.Save(); // Save the config file
            }
            
            internal static class SteamLobby
            {
                internal static ConfigEntry<bool> AutoLobby;
            }

            internal static class ItemSync
            {
                internal static ConfigEntry<bool> GhostItems;
                internal static ConfigEntry<bool> ForceDrop;
                internal static ConfigEntry<bool> ShotGunReload;
                internal static ConfigEntry<bool> SyncOnUse;
                internal static ConfigEntry<bool> SyncOnInteract;
                internal static ConfigEntry<bool> SyncIgnoreBattery;
            }
            
            internal static class SaveLimit
            {
                internal static ConfigEntry<bool> Enabled;
            }
            
            internal static class InvisiblePlayer
            {
                internal static ConfigEntry<bool> Enabled;
            }
            
            internal static class LogSpam
            {
                internal static ConfigEntry<bool> Enabled;
                internal static ConfigEntry<bool> CalculatePolygonPath;
                internal static ConfigEntry<bool> MoreCompany;
            }
            
            internal static class JoinQueue
            {
                internal static ConfigEntry<bool> Enabled;
                internal static ConfigEntry<long> ConnectionTimeout;
                internal static ConfigEntry<long> ConnectionDelay;
            }

        }

    }
}