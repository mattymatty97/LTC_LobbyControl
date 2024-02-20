using System.Collections.Generic;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace LobbyControl.Patches
{
    [HarmonyPatch]
    internal class RadarPatch
    {

        
        [HarmonyPatch]
        internal class NewItemPatch
        {
            
            private static bool _loadingLobby;

            private static readonly HashSet<GrabbableObject> ObjectsToUpdate = new HashSet<GrabbableObject>();
            
            [HarmonyPrefix]
            [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.LoadShipGrabbableItems))]
            public static void TrackSpawn(StartOfRound __instance)
            {
                _loadingLobby = true;
            }            
            
            [HarmonyPostfix]
            [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.LoadShipGrabbableItems))]
            public static void TrackSpawn2(StartOfRound __instance)
            {
                _loadingLobby = false;
            }
            
            [HarmonyPrefix]
            [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.SetScrapValue))]
            public static void TrackNew(GrabbableObject __instance)
            {
                if (!_loadingLobby)
                    return;

                ObjectsToUpdate.Add(__instance);
            }
            
            [HarmonyPostfix]
            [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.Update))]
            public static void UpdatePatch(GrabbableObject __instance)
            {
                if (!__instance.IsServer || !ObjectsToUpdate.Remove(__instance))
                    return;
                
                if (__instance.radarIcon != null && __instance.radarIcon.gameObject != null)
                        Object.Destroy(__instance.radarIcon.gameObject);
            }
        }

        [HarmonyPatch]
        internal class DeletedObjectPatch
        {
            [HarmonyPrefix]
            [HarmonyPatch(typeof(NetworkBehaviour), nameof(NetworkBehaviour.OnDestroy))]
            public static void DestroyPatch(NetworkBehaviour __instance)
            {
                var obj = __instance as GrabbableObject;
                if (obj != null && obj.radarIcon != null && obj.radarIcon.gameObject != null)
                    Object.Destroy(obj.radarIcon.gameObject);
            }
        }
    }
}