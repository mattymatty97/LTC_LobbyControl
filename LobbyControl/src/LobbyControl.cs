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
using LethalAPI.TerminalCommands.Models;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;
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
                //ItemSync
                ItemSync.GhostItems = config.Bind("ItemSync","ghost_items",true
                    ,"prevent the creation of non-grabbable items in case of inventory desync");
                ItemSync.ForceDrop = config.Bind("ItemSync","force_drop",true
                    ,"forcefully drop all items of the player causing the desync");
                ItemSync.ForceSyncSlot = config.Bind("ItemSync","force_sync",true
                    ,"forcefully tell clients which slot they are using when grabbing items");
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
                LogSpam.AudioSpacializer = config.Bind("LogSpam","zero_surface_area",true
                    ,"WIP");

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
                internal static ConfigEntry<bool> ForceSyncSlot;
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
                internal static ConfigEntry<bool> AudioSpacializer;
            }
        }

        // ReSharper disable Unity.PerformanceAnalysis
        internal static IEnumerator LoadLobbyCoroutine()
        {
            var startOfRound = StartOfRound.Instance;
            var terminal = Object.FindObjectOfType<Terminal>();
            //remove all items
            startOfRound.ResetShipFurniture();
            startOfRound.ResetPooledObjects(true);
            //remove remaining unlockables
            foreach (var unlockable in StartOfRound.Instance.SpawnedShipUnlockables.ToList())
            {
                var itemEntry = startOfRound.unlockablesList.unlockables[unlockable.Key];
                if (!itemEntry.alreadyUnlocked || !itemEntry.spawnPrefab) 
                    continue;
                                
                if(unlockable.Value == null)
                    continue;
                                
                NetworkObject component = unlockable.Value.GetComponent<NetworkObject>();
                if (component != null && component.IsSpawned)
                    component.Despawn();
            }
            startOfRound.SpawnedShipUnlockables.Clear();
            startOfRound.suitsPlaced = 0;
            //wait
            yield return new WaitForSeconds(0.2f);
            //read new misc data
            startOfRound.SetTimeAndPlanetToSavedSettings();
            startOfRound.SetMapScreenInfoToCurrentLevel();
            //restart the terminal
            terminal.Start();
            //if there are any players
            if (startOfRound.connectedPlayersAmount >= 1)
            {
                //send the updated misc variables to the players
                RefreshLobby();
            }   
            //spawn the new unlockables and update their position
            ReloadUnlockables();
            yield return new WaitForSeconds(0.2f);
            //spawn new items
            startOfRound.LoadShipGrabbableItems();
            yield return new WaitForSeconds(0.1f);
            //if there are any players
            if (startOfRound.connectedPlayersAmount >= 1)
            {
                //sync item variables
                startOfRound.SyncShipUnlockablesServerRpc();
            }
        }
        
        internal static void ReloadUnlockables()
        {
            StartOfRound startOfRound = StartOfRound.Instance;
            startOfRound.LoadUnlockables();
            try
            {
                //7 - closet
                //8 - cabinet
                //15 - beds
                //16 - terminal
                for (var baseUnlockable = 0; baseUnlockable < startOfRound.unlockablesList.unlockables.Count; baseUnlockable++)
                {
                    var unlockable = startOfRound.unlockablesList.unlockables[baseUnlockable];
                    if (unlockable.alreadyUnlocked && !startOfRound.SpawnedShipUnlockables.ContainsKey(baseUnlockable) &&
                        !unlockable.inStorage)
                    {
                        startOfRound.SpawnUnlockable(baseUnlockable);
                        PlaceableShipObject shipObject = startOfRound.SpawnedShipUnlockables[baseUnlockable]
                            .GetComponent<PlaceableShipObject>();
                        if (!shipObject)
                            shipObject = startOfRound.SpawnedShipUnlockables[baseUnlockable]
                                .GetComponentInChildren<PlaceableShipObject>();
                        var parentObject = shipObject.parentObject;
                        if (parentObject != null)
                        {
                            var offset = parentObject.positionOffset;
                            var localOffset = startOfRound.elevatorTransform.TransformPoint(offset);
                            var position = shipObject.mainMesh.transform.position;
                            var placementPosition = localOffset -
                                                    (shipObject.parentObject.transform.position - position) -
                                                    (position - shipObject.placeObjectCollider.transform.position);
                            unlockable.placedPosition = placementPosition;
                            var rotation = Quaternion.Euler(parentObject.rotationOffset);
                            var step1 = rotation * Quaternion.Inverse(parentObject.transform.rotation);
                            var step2 = step1 * shipObject.mainMesh.transform.rotation;
                            var placementRotation = step2.eulerAngles;
                            unlockable.placedRotation = placementRotation;
                        }
                    }
                }

                if (startOfRound.connectedPlayersAmount >= 1)
                {
                    List<ulong> rpcList = startOfRound.ClientPlayerList.Keys.ToList();

                    ClientRpcParams clientRpcParams = new ClientRpcParams()
                    {
                        Send = new ClientRpcSendParams()
                        {
                            TargetClientIds = rpcList,
                        },
                    };

                    for (var baseUnlockable = 0; baseUnlockable < startOfRound.unlockablesList.unlockables.Count; baseUnlockable++)
                    {
                        var unlockable = startOfRound.unlockablesList.unlockables[baseUnlockable];
                        if (unlockable.alreadyUnlocked && !unlockable.inStorage)
                        {
                            FastBufferWriter bufferWriter = (FastBufferWriter)BeginSendClientRpc.Invoke(startOfRound,
                                new object[] { 1076853239U, clientRpcParams, RpcDelivery.Reliable });
                            BytePacker.WriteValueBitPacked(bufferWriter, baseUnlockable);
                            EndSendClientRpc.Invoke(startOfRound,
                                new object[] { bufferWriter, 1076853239U, clientRpcParams, RpcDelivery.Reliable });
                        }
                    }

                    startOfRound.SyncShipUnlockablesServerRpc();
                }
            }
            catch (Exception ex)
            {
                LobbyControl.Log.LogError(ex);
            }
        }

        internal static void RefreshLobby()
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