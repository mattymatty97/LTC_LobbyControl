using System;
using System.Collections.Generic;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LobbyControl.Patches
{
    [HarmonyPatch]
    internal class RadarPatch
    {
        [HarmonyPatch]
        internal class NewShipItemPatch
        {
            private static readonly HashSet<GrabbableObject> UpdatableObjects = new HashSet<GrabbableObject>();
            [HarmonyPrefix]
            [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.Start))]
            private static void TrackNew(GrabbableObject __instance)
            {
                if (!LobbyControl.PluginConfig.Radar.Enabled.Value ||
                    !LobbyControl.PluginConfig.Radar.RemoveOnShip.Value)
                    return;

                if (!__instance.transform.IsChildOf(StartOfRound.Instance.elevatorTransform))
                    return;

                UpdatableObjects.Add(__instance);
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.Update))]
            [HarmonyPriority(0)]
            private static void UpdatePatch(GrabbableObject __instance, bool __runOriginal)
            {
                if (!__runOriginal || !UpdatableObjects.Remove(__instance))
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
            private static void DestroyPatch(NetworkBehaviour __instance)
            {
                if (!LobbyControl.PluginConfig.Radar.Enabled.Value ||
                    !LobbyControl.PluginConfig.Radar.RemoveDeleted.Value)
                    return;

                var obj = __instance as GrabbableObject;
                if (obj != null && obj.radarIcon != null && obj.radarIcon.gameObject != null)
                    Object.Destroy(obj.radarIcon.gameObject);
            }
        }
        
        [HarmonyFinalizer]
        [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.Start))]
        private static Exception GracefullyHandleExceptionsFromMods(GrabbableObject __instance, Exception __exception)
        {
            if (__exception != null)
            {
                Debug.LogException(__exception);
            }
            return null;
        }
    }
}