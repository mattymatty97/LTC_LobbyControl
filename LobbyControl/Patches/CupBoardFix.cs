﻿using System;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LobbyControl.Patches
{
    [HarmonyPatch]
    internal class CupBoardFix
    {
        private const float Tolerance = 0.3f;
        private const float Shift = 0.02f;
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.Start))]
        private static void ObjectLoad(GrabbableObject __instance, ref object[] __state)
        {
            try
            {
                var pos = __instance.transform.position += new Vector3(0, Shift, 0);
                __state = new object[] { __instance.itemProperties.itemSpawnsOnGround };
                GameObject closet = GameObject.Find("/Environment/HangarShip/StorageCloset");
                MeshCollider collider = closet.GetComponent<MeshCollider>();
                if (collider.bounds.Contains(pos))
                {
                    __instance.transform.parent = closet.transform;
                    __instance.itemProperties.itemSpawnsOnGround = false;
                }
                else
                {
                    var closest = collider.bounds.ClosestPoint(pos);
                    if (Math.Abs(closest.x - pos.x) < Tolerance && Math.Abs(closest.z - pos.z) < Tolerance &&
                        closest.y - Tolerance <= pos.y)
                        __instance.itemProperties.itemSpawnsOnGround = false;
                }
            }
            catch (Exception ex)
            {
                LobbyControl.Log.LogError(ex);
            }
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.Start))]
        private static void ObjectLoad2(GrabbableObject __instance, object[] __state)
        {
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
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.OnPlayerConnectedClientRpc))]
        private static void CozyImprovementsFix2(StartOfRound __instance, ulong clientId)
        {
            if (clientId != NetworkManager.Singleton.LocalClientId)
                return;
            
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