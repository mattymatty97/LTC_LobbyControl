using System;
using HarmonyLib;
using LobbyControl.Patches.Utility;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LobbyControl.Patches
{
    [HarmonyPatch]
    internal class CupBoardFix
    {
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(NetworkBehaviour), nameof(NetworkBehaviour.OnNetworkSpawn))]
        private static void ObjectLoad(NetworkBehaviour __instance)
        {
            if (__instance is GrabbableObject grabbable)
            {
                if (!LobbyControl.PluginConfig.CupBoard.Enabled.Value)
                    return;

                if (__instance.transform.name == "ClipboardManual" || __instance.transform.name == "StickyNoteItem")
                    return;

                if (!StartOfRound.Instance.shipInnerRoomBounds.bounds.Contains(__instance.transform.position))
                    return;
                
                if (StartOfRound.Instance.localPlayerController != null && !StartOfRound.Instance.localPlayerController.justConnected)
                    GrabbableObjectUtility.AppendToHolder(grabbable, nameof(CupBoardFix), (int)GrabbableObjectUtility.DelayValues.CupBoard, UpdateCallback);
                else if(grabbable.IsServer)
                    GrabbableObjectUtility.AppendToHolder(grabbable, nameof(CupBoardFix), (int)GrabbableObjectUtility.DelayValues.CupBoardServer, UpdateCallback);
                else
                    GrabbableObjectUtility.AppendToHolder(grabbable, nameof(CupBoardFix), (int)GrabbableObjectUtility.DelayValues.CupBoardClient, UpdateCallback);
            }
        }

        private static void UpdateCallback(GrabbableObject grabbable, GrabbableObjectUtility.UpdateHolder updateHolder)
        {
            LobbyControl.Log.LogDebug(
                $"{grabbable.itemProperties.itemName}({grabbable.gameObject.GetInstanceID()}) - Cupboard Triggered!");
            var tolerance = LobbyControl.PluginConfig.CupBoard.Tolerance.Value;
            try
            {
                var pos = updateHolder.OriginalPos;
                LobbyControl.Log.LogDebug(
                    $"{grabbable.itemProperties.itemName}({grabbable.gameObject.GetInstanceID()}) - Item pos {pos}!");

                var closet = GameObject.Find("/Environment/HangarShip/StorageCloset");
                PlaceableObjectsSurface[] storageShelves =
                    closet.GetComponentsInChildren<PlaceableObjectsSurface>();
                var collider = closet.GetComponent<Collider>();
                var distance = float.MaxValue;
                PlaceableObjectsSurface found = null;
                Vector3? closest = null;
                
               LobbyControl.Log.LogDebug(
                    $"{grabbable.itemProperties.itemName}({grabbable.gameObject.GetInstanceID()}) - Cupboard pos {collider.bounds.min}!");
                
                if (collider.bounds.Contains(pos))
                {
                    foreach (var shelf in storageShelves)
                    {
                        var hitPoint = shelf.GetComponent<Collider>().ClosestPoint(pos);
                        var tmp = pos.y - hitPoint.y;
                        LobbyControl.Log.LogDebug(
                            $"{grabbable.itemProperties.itemName}({grabbable.gameObject.GetInstanceID()}) - Shelve is {tmp} away!");
                        if (tmp >= 0 && tmp < distance)
                        {
                            found = shelf;
                            distance = tmp;
                            closest = hitPoint;
                        }
                    }

                    LobbyControl.Log.LogDebug(
                        $"{grabbable.itemProperties.itemName}({grabbable.gameObject.GetInstanceID()}) - Chosen Shelve is {distance} away!");
                    LobbyControl.Log.LogDebug(
                        $"{grabbable.itemProperties.itemName}({grabbable.gameObject.GetInstanceID()}) - With hitpoint at {closest}!");
                }
                
                var transform = grabbable.transform;
                if (found != null && closest.HasValue)
                {
                    Vector3 newPos;
                    if (LobbyControl.PluginConfig.ItemClipping.Enabled.Value)
                    {
                        newPos = ItemPatches.FixPlacement(closest.Value, found.transform, grabbable);
                    }
                    else
                    {
                        newPos = closest.Value + Vector3.up * LobbyControl.PluginConfig.CupBoard.Shift.Value;
                    }

                    LobbyControl.Log.LogDebug(
                        $"{grabbable.itemProperties.itemName}({grabbable.gameObject.GetInstanceID()}) - With newPos at {newPos}!");
                    transform.parent = closet.transform;
                    transform.position = newPos;
                    grabbable.targetFloorPosition = transform.localPosition;
                }
                else
                {
                    //check if we're above the closet
                    var hitPoint = collider.bounds.ClosestPoint(pos);
                    var xDelta = hitPoint.x - pos.x;
                    var zDelta = hitPoint.z - pos.z;
                    var yDelta = pos.y - hitPoint.y;
                    if (Math.Abs(xDelta) < tolerance && Math.Abs(zDelta) < tolerance && yDelta > 0)
                    {
                        LobbyControl.Log.LogDebug(
                            $"{grabbable.itemProperties.itemName}({grabbable.gameObject.GetInstanceID()}) - Was above the Cupboard!");

                        transform.position = pos;
                        grabbable.targetFloorPosition = transform.localPosition;

                        if (Math.Abs(xDelta) > 0)
                            grabbable.transform.position += new Vector3(xDelta, 0, 0);
                        if (Math.Abs(zDelta) > 0)
                            grabbable.transform.position += new Vector3(0, 0, zDelta);
                    }
                }
            }
            catch (Exception ex)
            {
                LobbyControl.Log.LogError($"Exception while checking for Cupboard {ex}");
            }
        }
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.LoadUnlockables))]
        private static void CozyImprovementsFix(StartOfRound __instance)
        {
            var closet = GameObject.Find("/Environment/HangarShip/StorageCloset");
            if (closet == null)
                return;

            foreach (var light in closet.GetComponentsInChildren<Light>())
                if (light.gameObject.transform.name == "StorageClosetLight")
                    Object.Destroy(light.gameObject);
        }
    }
}