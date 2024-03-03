using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using LethalAPI.LibTerminal;
using LethalAPI.LibTerminal.Models;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements.Collections;
using Object = UnityEngine.Object;
using PluginInfo = BepInEx.PluginInfo;


namespace LobbyControl
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency("LethalAPI.Terminal","1.0.0")]
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

        private TerminalModRegistry _commands;

        private static readonly string[] IncompatibleGUIDs = new string[]
        {
            "com.github.tinyhoot.ShipLobby",
            "twig.latecompany",
            "com.potatoepet.AdvancedCompany"
        };

        private void Awake()
        {
            Log = Logger;
            try
            {
                PluginInfo[] incompatibleMods = Chainloader.PluginInfos.Values.Where(p => IncompatibleGUIDs.Contains(p.Metadata.GUID)).ToArray();
                if (incompatibleMods.Length > 0)
                {                    
                    foreach (var mod in incompatibleMods)
                    {
                        Log.LogWarning($"{mod.Metadata.Name} is incompatible!");   
                    }
                    Log.LogError($"{incompatibleMods.Length} incompatible mods found! Disabling!");
                }
                else
                {
                    Log.LogInfo("Initializing Configs");

                    PluginConfig.Init(this);

                    Log.LogInfo("Registering Commands");
                    _commands = TerminalRegistry.CreateTerminalRegistry();
                    _commands.RegisterFrom<LobbyCommands>();
                    
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
        
        
        private static readonly MethodInfo BeginSendClientRpc = typeof(StartOfRound).GetMethod(nameof(StartOfRound.__beginSendClientRpc), BindingFlags.NonPublic | BindingFlags.Instance);
        private static readonly MethodInfo EndSendClientRpc = typeof(StartOfRound).GetMethod(nameof(StartOfRound.__endSendClientRpc), BindingFlags.NonPublic | BindingFlags.Instance);

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
                //GhostItems
                GhostItems.Enabled = config.Bind("GhostItems","enabled",false
                    ,"prevent the creation of non-grabbable items in case of inventory desync");
                //ItemClipping
                ItemClipping.Enabled = config.Bind("ItemClipping","enabled",true
                    ,"fix rotation and height of various items when on the Ground");
                ItemClipping.VerticalOffset = config.Bind("ItemClipping","vertical_offset",0f
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
                //RotationSync
                RotationSync.Enabled = config.Bind("RotationSync","enabled",true
                    ,"attempt to keep item rotation synced while not held ( ladders being in different rotation across clients )\nOther Clients need to have the mod installed to see the changes");
                
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
            
            internal static class GhostItems
            {
                internal static ConfigEntry<bool> Enabled;
            }
            
            internal static class ItemClipping
            {
                internal static ConfigEntry<bool> Enabled;
                internal static ConfigEntry<float> VerticalOffset;
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
            
            internal static class RotationSync
            {
                internal static ConfigEntry<bool> Enabled;
            }
        }

        public static IEnumerator LoadItemsCoroutine()
        {
            yield return new WaitForSeconds(2);
            StartOfRound.Instance.LoadShipGrabbableItems();
        }

        public static void RefreshLobby()
        {
            var startOfRound = StartOfRound.Instance;
            List<ulong> ulongList = new List<ulong>();
            List<ulong> rpcList = new List<ulong>();
            for (var index = 0; index < startOfRound.allPlayerObjects.Length; ++index)
            {
                var component = startOfRound.allPlayerObjects[index].GetComponent<NetworkObject>();
                if (!component.IsOwnedByServer)
                {
                    ulongList.Add(component.OwnerClientId);
                    rpcList.Add(component.OwnerClientId);
                }
                else if (index == 0)
                    ulongList.Add(NetworkManager.Singleton.LocalClientId);
                else
                    ulongList.Add(999UL);
            }
            
            ClientRpcParams clientRpcParams = new ClientRpcParams() {
                Send = new ClientRpcSendParams() {
                    TargetClientIds = rpcList,
                },
            };
            
            var groupCredits = Object.FindObjectOfType<Terminal>().groupCredits;
            var profitQuota = TimeOfDay.Instance.profitQuota;
            var quotaFulfilled = TimeOfDay.Instance.quotaFulfilled;
            var timeUntilDeadline = (int)TimeOfDay.Instance.timeUntilDeadline;
            var controller = StartOfRound.Instance.localPlayerController;

            FastBufferWriter bufferWriter = (FastBufferWriter)BeginSendClientRpc.Invoke(startOfRound, new object[]{886676601U, clientRpcParams, RpcDelivery.Reliable});
            BytePacker.WriteValueBitPacked(bufferWriter, controller.actualClientId);
            BytePacker.WriteValueBitPacked(bufferWriter, startOfRound.connectedPlayersAmount - 1);
            bufferWriter.WriteValueSafe<bool>(true);
            bufferWriter.WriteValueSafe<ulong>(ulongList.ToArray());
            BytePacker.WriteValueBitPacked(bufferWriter, startOfRound.ClientPlayerList[controller.actualClientId]);
            BytePacker.WriteValueBitPacked(bufferWriter, groupCredits);
            BytePacker.WriteValueBitPacked(bufferWriter, startOfRound.currentLevelID);
            BytePacker.WriteValueBitPacked(bufferWriter, profitQuota);
            BytePacker.WriteValueBitPacked(bufferWriter, timeUntilDeadline);
            BytePacker.WriteValueBitPacked(bufferWriter, quotaFulfilled);
            BytePacker.WriteValueBitPacked(bufferWriter,  startOfRound.randomMapSeed);
            bufferWriter.WriteValueSafe<bool>(in startOfRound.isChallengeFile, new FastBufferWriter.ForPrimitives());
            EndSendClientRpc.Invoke(startOfRound, new object[]{bufferWriter, 886676601U, clientRpcParams, RpcDelivery.Reliable});

        }
    }
}