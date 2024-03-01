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
    internal class LobbyControl : BaseUnityPlugin
    {
        public const string GUID = "mattymatty.LobbyControl";
        public const string NAME = "LobbyControl";
        public const string VERSION = "2.2.4";

        internal static ManualLogSource Log;

        public static bool CanModifyLobby = true;

        public static bool CanSave = true;
        public static bool AutoSaveEnabled = true;

        public static bool Enabled = true;

        private TerminalModRegistry _commands;

        private static readonly string[] IncompatibleGUIDs = new string[]
        {
            "com.github.tinyhoot.ShipLobby",
            "twig.latecompany",
            "com.potatoepet.AdvancedCompany"
        };

        private void Awake()
        {
            Log.LogInfo("Initializing Configs");

            PluginConfig.Init(this);

            _commands = TerminalRegistry.CreateTerminalRegistry();
            _commands.RegisterFrom<LobbyCommands>();
        }

        private void Start()
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
                    Enabled = false;
                }
                else
                {
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

        internal static void ReloadShipUnlockables()
        {
            try
            {
                if (ES3.KeyExists("UnlockedShipObjects", GameNetworkManager.Instance.currentSaveFileName))
                {
                    StartOfRound.Instance.suitsPlaced = 0;
                    var numArray = ES3.Load<int[]>("UnlockedShipObjects",
                        GameNetworkManager.Instance.currentSaveFileName);
                    for (var index = 0; index < numArray.Length; ++index)
                        if (!StartOfRound.Instance.unlockablesList.unlockables[numArray[index]].alreadyUnlocked ||
                            StartOfRound.Instance.unlockablesList.unlockables[numArray[index]].IsPlaceable)
                        {
                            if (!StartOfRound.Instance.unlockablesList.unlockables[numArray[index]].alreadyUnlocked)
                                StartOfRound.Instance.unlockablesList.unlockables[numArray[index]]
                                    .hasBeenUnlockedByPlayer = true;
                            if (ES3.KeyExists(
                                    "ShipUnlockStored_" + StartOfRound.Instance.unlockablesList
                                        .unlockables[numArray[index]].unlockableName,
                                    GameNetworkManager.Instance.currentSaveFileName) && ES3.Load(
                                    "ShipUnlockStored_" + StartOfRound.Instance.unlockablesList
                                        .unlockables[numArray[index]].unlockableName,
                                    GameNetworkManager.Instance.currentSaveFileName, false))
                                StartOfRound.Instance.unlockablesList.unlockables[numArray[index]].inStorage = true;
                            else
                                SpawnShipUnlockable(numArray[index]);
                        }

                    PlaceableShipObject[] objectsOfType = FindObjectsOfType<PlaceableShipObject>();
                    for (var index = 0; index < objectsOfType.Length; ++index)
                        if (!StartOfRound.Instance.unlockablesList.unlockables[objectsOfType[index].unlockableID]
                                .spawnPrefab && StartOfRound.Instance.unlockablesList
                                .unlockables[objectsOfType[index].unlockableID].inStorage)
                        {
                            objectsOfType[index].parentObject.disableObject = true;
                            Log.LogDebug("DISABLE OBJECT A");
                        }
                }

                for (var index = 0; index < StartOfRound.Instance.unlockablesList.unlockables.Count; ++index)
                    if ((index != 0 || !StartOfRound.Instance.isChallengeFile) &&
                        (StartOfRound.Instance.unlockablesList.unlockables[index].alreadyUnlocked ||
                         (StartOfRound.Instance.unlockablesList.unlockables[index].unlockedInChallengeFile &&
                          StartOfRound.Instance.isChallengeFile)) &&
                        !StartOfRound.Instance.unlockablesList.unlockables[index].IsPlaceable)
                        SpawnShipUnlockable(index);
            }
            catch (Exception ex)
            {
                Log.LogError($"Error attempting to load ship unlockables on the host: {ex}");
            }
        }

        internal static void SpawnShipUnlockable(int unlockableIndex)
        {
            GameObject gameObject = null;
            var unlockable = StartOfRound.Instance.unlockablesList.unlockables[unlockableIndex];
            switch (unlockable.unlockableType)
            {
                case 0:
                    gameObject = StartOfRound.Instance.SpawnedShipUnlockables.Get(unlockableIndex);
                    if (gameObject == null)
                    {
                        gameObject = Instantiate<GameObject>(StartOfRound.Instance.suitPrefab,
                            StartOfRound.Instance.rightmostSuitPosition.position +
                            StartOfRound.Instance.rightmostSuitPosition.forward * 0.18f *
                            StartOfRound.Instance.suitsPlaced,
                            StartOfRound.Instance.rightmostSuitPosition.rotation, null);
                    }
                    else
                    {
                        gameObject.transform.position = StartOfRound.Instance.rightmostSuitPosition.position +
                                                        StartOfRound.Instance.rightmostSuitPosition.forward * 0.18f *
                                                        StartOfRound.Instance.suitsPlaced;
                        gameObject.transform.rotation = StartOfRound.Instance.rightmostSuitPosition.rotation;
                    }

                    gameObject.GetComponent<UnlockableSuit>().syncedSuitID.Value = unlockableIndex;
                    if (!gameObject.GetComponent<NetworkObject>().IsSpawned)
                        gameObject.GetComponent<NetworkObject>().Spawn();
                    var component = gameObject.gameObject.GetComponent<AutoParentToShip>();
                    component.overrideOffset = true;
                    component.positionOffset = new Vector3(-2.45f, 2.75f, -8.41f) +
                                               StartOfRound.Instance.rightmostSuitPosition.forward * 0.18f *
                                               StartOfRound.Instance.suitsPlaced;
                    component.rotationOffset = new Vector3(0.0f, 90f, 0.0f);
                    StartOfRound.Instance.SyncSuitsServerRpc();
                    ++StartOfRound.Instance.suitsPlaced;
                    break;
                case 1:
                    gameObject = StartOfRound.Instance.SpawnedShipUnlockables.Get(unlockableIndex);
                    if (unlockable.spawnPrefab)
                    {
                        if (gameObject == null)
                        {
                            gameObject = Instantiate<GameObject>(unlockable.prefabObject,
                                StartOfRound.Instance.elevatorTransform.position, Quaternion.identity, null);
                        }
                        else
                        {
                            gameObject.transform.position = StartOfRound.Instance.elevatorTransform.position;
                            gameObject.transform.rotation = Quaternion.identity;
                        }
                    }
                    else
                    {
                        Debug.Log("Placing scene object at saved position: " + StartOfRound.Instance
                            .unlockablesList.unlockables[unlockableIndex].unlockableName);
                        if (gameObject == null)
                        {
                            PlaceableShipObject[] objectsOfType =
                                FindObjectsOfType<PlaceableShipObject>();
                            for (var index = 0; index < objectsOfType.Length; ++index)
                                if (objectsOfType[index].unlockableID == unlockableIndex)
                                    gameObject = objectsOfType[index].parentObject.gameObject;

                            if (gameObject == null)
                                return;
                        }
                    }

                    if (ES3.KeyExists("ShipUnlockMoved_" + unlockable.unlockableName,
                            GameNetworkManager.Instance.currentSaveFileName))
                    {
                        var placementPosition = ES3.Load("ShipUnlockPos_" + unlockable.unlockableName,
                            GameNetworkManager.Instance.currentSaveFileName, Vector3.zero);
                        var placementRotation = ES3.Load("ShipUnlockRot_" + unlockable.unlockableName,
                            GameNetworkManager.Instance.currentSaveFileName, Vector3.zero);
                        Debug.Log(string.Format("Loading placed object position as: {0}",
                            placementPosition));
                        ShipBuildModeManager.Instance.PlaceShipObject(placementPosition, placementRotation,
                            gameObject.GetComponentInChildren<PlaceableShipObject>(), false);
                    }

                    if (!gameObject.GetComponent<NetworkObject>().IsSpawned)
                        gameObject.GetComponent<NetworkObject>().Spawn();

                    break;
            }

            if (gameObject == null)
                return;

            StartOfRound.Instance.SpawnedShipUnlockables[unlockableIndex] = gameObject;
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
                CupBoard.Shift = config.Bind("CupBoard","shift",new Vector3(0, 0.1f, 0)
                    ,"how much move the items inside the closet on load");
                //Radar
                Radar.Enabled = config.Bind("Radar","enabled",true
                    ,"remove orphan radar icons from deleted/collected scrap");
                Radar.RemoveDeleted = config.Bind("Radar","deleted_scrap",true
                    ,"remove orphan radar icons from deleted scrap ( company building )");
                Radar.RemoveOnShip = config.Bind("Radar","ship_loot",true
                    ,"remove orphan radar icons from scrap on the ship in a recently created game");
                //GhostItems
                GhostItems.Enabled = config.Bind("GhostItems","enabled",true
                    ,"prevent the creation of non-grabbable items in case of inventory desync");
                //ItemClipping
                ItemClipping.Enabled = config.Bind("ItemClipping","enabled",true
                    ,"fix rotation and height of various items when on the Ground");
                ItemClipping.VerticalOffset = config.Bind("ItemClipping","vertical_offset",0.07f
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
            }
            
            internal static class SteamLobby
            {
                internal static ConfigEntry<bool> AutoLobby;
            }
            
            internal static class CupBoard
            {
                internal static ConfigEntry<bool> Enabled;
                internal static ConfigEntry<float> Tolerance;
                internal static ConfigEntry<Vector3> Shift;
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