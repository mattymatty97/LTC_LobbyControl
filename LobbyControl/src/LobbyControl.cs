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
                //CupBoard
                CupBoard.Enabled = config.Bind("CupBoard","enabled",true
                    ,"prevent items inside or above the Storage Closet from falling to the ground");
                CupBoard.Tolerance = config.Bind("CupBoard","tolerance",0.05f
                    ,"how loosely \"close\" the items have to be to the top of the closet for them to count X/Z");
                CupBoard.Shift = config.Bind("CupBoard","shift",0.1f
                    ,"how much move the items inside the closet on load ( only if ItemClippingFix disabled )");
                //Radar
                Radar.Enabled = config.Bind("Radar","enabled",true
                    ,"remove orphan radar icons from deleted/collected scrap");
                Radar.RemoveDeleted = config.Bind("Radar","deleted_scrap",true
                    ,"remove orphan radar icons from deleted scrap ( company building )");
                Radar.RemoveOnShip = config.Bind("Radar","ship_loot",true
                    ,"remove orphan radar icons from scrap on the ship in a recently created game");
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
                //ItemClipping
                ItemClipping.Enabled = config.Bind("ItemClipping","enabled",true
                    ,"fix rotation and height of various items when on the Ground");
                ItemClipping.RotateOnSpawn = config.Bind("ItemClipping","rotate_on_spawn",true
                    ,"fix rotation of newly spawned items");
                ItemClipping.VerticalOffset = config.Bind("ItemClipping","vertical_offset",0f
                    ,"additional y offset for items on the ground");
                ItemClipping.ManualOffsets = config.Bind("ItemClipping","manual_offsets","Apparatus:0.25,Pro-flashlight:0.03,Large axle:0.55"
                    ,"y offset for items on the ground");
                //SaveLimit
                SaveLimit.Enabled = config.Bind("SaveLimit","enabled",true
                    ,"remove the limit to the amount of items that can be saved");
                //InvisiblePlayer
                InvisiblePlayer.Enabled = config.Bind("InvisiblePlayer","enabled",true
                    ,"attempts to fix late joining players appearing invisible to the rest of the lobby");
                //SteamLobby
                SteamLobby.AutoLobby = config.Bind("SteamLobby","auto_lobby",false
                    ,"automatically reopen the lobby as soon as you reach orbit");
                //OutOfBounds
                OutOfBounds.Enabled = config.Bind("OutOfBounds","enabled",true
                    ,"prevent items from falling below the ship");
                OutOfBounds.VerticalOffset = config.Bind("OutOfBounds","vertical_offset",0.2f
                    ,"vertical offset to apply to objects on load");
                //LogSpam
                LogSpam.Enabled = config.Bind("LogSpam","enabled",true
                    ,"prevent some annoying log spam");
                LogSpam.CalculatePolygonPath = config.Bind("LogSpam","CalculatePolygonPath",true
                    ,"stop pathfinding for dead Enemies");
                LogSpam.ZeroSurfaceArea = config.Bind("LogSpam","zero_surface_area",true
                    ,"use sphere shape for lightning particles ( this also makes them more visible on all items )");
                LogSpam.MoreCompany = config.Bind("LogSpam","more_company",true
                    ,"Remove some leftover Exceptions caused by MoreCompany");
                //JoinQueue
                JoinQueue.Enabled = config.Bind("JoinQueue","enabled",true
                    ,"handle joining players as a queue instead of at the same time");
                JoinQueue.ConnectionTimeout = config.Bind("JoinQueue","connection_timeout_ms",30000L
                    ,"After how much time discard a hanging connection");
                JoinQueue.ConnectionDelay = config.Bind("JoinQueue","connection_timeout_ms",500L
                    ,"After how much time discard a hanging connection");

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
            
            internal static class CupBoard
            {
                internal static ConfigEntry<bool> Enabled;
                internal static ConfigEntry<float> Tolerance;
                internal static ConfigEntry<float> Shift;
            }
            internal static class Radar
            {
                internal static ConfigEntry<bool> Enabled;
                internal static ConfigEntry<bool> RemoveDeleted;
                internal static ConfigEntry<bool> RemoveOnShip;
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
            
            internal static class ItemClipping
            {
                internal static ConfigEntry<bool> Enabled;
                internal static ConfigEntry<bool> RotateOnSpawn;
                internal static ConfigEntry<float> VerticalOffset;
                internal static ConfigEntry<string> ManualOffsets;
            }
            
            internal static class SaveLimit
            {
                internal static ConfigEntry<bool> Enabled;
            }
            
            internal static class InvisiblePlayer
            {
                internal static ConfigEntry<bool> Enabled;
            }
            
            internal static class OutOfBounds
            {
                internal static ConfigEntry<bool> Enabled;
                internal static ConfigEntry<float> VerticalOffset;
            }
            
            internal static class LogSpam
            {
                internal static ConfigEntry<bool> Enabled;
                internal static ConfigEntry<bool> CalculatePolygonPath;
                internal static ConfigEntry<bool> ZeroSurfaceArea;
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