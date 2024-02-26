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
        internal class NewShipItemPatch
        {

            private static readonly HashSet<GrabbableObject> ObjectsToUpdate = new HashSet<GrabbableObject>();
            
            [HarmonyPrefix]
            [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.Start))]
            public static void TrackNew(GrabbableObject __instance)
            {
                if (!LobbyControl.PluginConfig.Radar.Enabled.Value || !LobbyControl.PluginConfig.Radar.RemoveOnShip.Value)
                    return;
                
                if (!__instance.transform.IsChildOf(StartOfRound.Instance.elevatorTransform))
                    return;

                ObjectsToUpdate.Add(__instance);
            }
            
            [HarmonyPostfix]
            [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.Update))]
            public static void UpdatePatch(GrabbableObject __instance, bool __runOriginal)
            {
                if (!__runOriginal || !ObjectsToUpdate.Remove(__instance))
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
                if (!LobbyControl.PluginConfig.Radar.Enabled.Value || !LobbyControl.PluginConfig.Radar.RemoveDeleted.Value)
                    return;
                
                var obj = __instance as GrabbableObject;
                if (obj != null && obj.radarIcon != null && obj.radarIcon.gameObject != null)
                    Object.Destroy(obj.radarIcon.gameObject);
            }
        }
    }
}