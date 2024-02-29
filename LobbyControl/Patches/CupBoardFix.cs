using System;
using System.Collections.Generic;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LobbyControl.Patches
{
    [HarmonyPatch]
    internal class CupBoardFix
    {
        private static readonly HashSet<GrabbableObject> NoGravityObjects = new HashSet<GrabbableObject>();

        [HarmonyPrefix]
        [HarmonyPatch(typeof(NetworkBehaviour), nameof(NetworkBehaviour.OnNetworkSpawn))]
        private static void ObjectLoad(NetworkBehaviour __instance)
        {
            if (__instance is GrabbableObject grabbable)
            {
                if (!LobbyControl.PluginConfig.CupBoard.Enabled.Value)
                    return;

                var tolerance = LobbyControl.PluginConfig.CupBoard.Tolerance.Value;
                try
                {
                    var pos = grabbable.transform.position;
                    
                    if (LobbyControl.PluginConfig.UnnamedPatch.Enabled.Value && grabbable.itemProperties.itemSpawnsOnGround)
                        pos -= Vector3.up * LobbyControl.PluginConfig.UnnamedPatch.verticalOffest.Value;
                    
                    GameObject closet = GameObject.Find("/Environment/HangarShip/StorageCloset");
                    PlaceableObjectsSurface[] storageShelves =
                        closet.GetComponentsInChildren<PlaceableObjectsSurface>();
                    MeshCollider collider = closet.GetComponent<MeshCollider>();
                    float distance = float.MaxValue;
                    PlaceableObjectsSurface found = null;
                    Vector3? closest = null;
                    foreach (var shelf in storageShelves)
                    {
                        if (Vector3.Distance(pos, shelf.placeableBounds.bounds.ClosestPoint(pos)) > tolerance)
                            continue;

                        var hitPoint = shelf.GetComponent<Collider>().ClosestPoint(pos);
                        var tmp = pos.y - hitPoint.y;
                        if (tmp >= 0 && tmp < distance)
                        {
                            found = shelf;
                            distance = tmp;
                            closest = hitPoint;
                        }
                    }

                    if (found != null && closest.HasValue)
                    {
                        var transform = grabbable.transform;
                        if (LobbyControl.PluginConfig.ItemClipping.Enabled.Value)
                        {
                            var newPos = ItemClippingPatch.FixPlacement(closest.Value, found.transform, grabbable);
                            transform.position = newPos;
                        }
                        else
                        {
                            transform.position = closest.Value + LobbyControl.PluginConfig.CupBoard.Shift.Value;
                        }
                        
                        transform.parent = closet.transform;
                        
                        NoGravityObjects.Add(grabbable);
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
                            grabbable.transform.position = pos;
                            
                            NoGravityObjects.Add(grabbable);

                            if (Math.Abs(xDelta) > 0)
                                grabbable.transform.position += new Vector3(xDelta, 0, 0);
                            if (Math.Abs(zDelta) > 0)
                                grabbable.transform.position += new Vector3(0, 0, zDelta);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LobbyControl.Log.LogError(ex);
                }
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.Start))]
        private static void ObjectLoad(GrabbableObject __instance, ref object[] __state)
        {
            __state = new object[] { __instance.itemProperties.itemSpawnsOnGround };
            
            if (!LobbyControl.PluginConfig.CupBoard.Enabled.Value)
                return;

            if (NoGravityObjects.Remove(__instance))
            {
                __instance.itemProperties.itemSpawnsOnGround = false;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.Start))]
        private static void ObjectLoad2(GrabbableObject __instance, object[] __state, bool __runOriginal)
        {
            if (!LobbyControl.PluginConfig.CupBoard.Enabled.Value)
                return;

            __instance.itemProperties.itemSpawnsOnGround = (bool)__state[0];
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.LoadUnlockables))]
        private static void CozyImprovementsFix(StartOfRound __instance)
        {
            GameObject closet = GameObject.Find("/Environment/HangarShip/StorageCloset");
            if (closet == null)
                return;

            foreach (Light light in closet.GetComponentsInChildren<Light>())
            {
                if (light.gameObject.transform.name == "StorageClosetLight")
                    Object.Destroy(light.gameObject);
            }
        }
    }
}