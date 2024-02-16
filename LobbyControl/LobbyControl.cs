using System;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using LethalAPI.TerminalCommands.Models;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements.Collections;

namespace LobbyControl
{
    [BepInPlugin(GUID, NAME, VERSION)]
    internal class LobbyControl : BaseUnityPlugin
    {
        public const string GUID = "com.github.mattymatty.LobbyControl";
        public const string NAME = "LobbyControl";
        public const string VERSION = "2.2.0";

        internal static ManualLogSource Log;

        public static bool CanModifyLobby = true;

        public static bool CanSave = true;
        public static bool AutoSaveEnabled = true;

        private TerminalModRegistry _commands;

        private void Awake()
        {
            Log = Logger;
            try
            {
                var harmony = new Harmony(GUID);
                harmony.PatchAll(Assembly.GetExecutingAssembly());

                _commands = TerminalRegistry.CreateTerminalRegistry();
                _commands.RegisterFrom<LobbyCommands>();
                Log.LogInfo(NAME + " v" + VERSION + " Loaded!");
            }
            catch (Exception ex)
            {
                Log.LogError("Exception while initializing: \n" + ex);
            }
        }

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

                    var objectsOfType = FindObjectsOfType<PlaceableShipObject>();
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
                            var objectsOfType =
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

            if (!(gameObject != null))
                return;

            StartOfRound.Instance.SpawnedShipUnlockables[unlockableIndex] = gameObject;
        }
    }
}