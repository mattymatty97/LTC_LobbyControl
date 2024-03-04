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
            [HarmonyPrefix]
            [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.Start))]
            private static void TrackNew(GrabbableObject __instance)
            {
                if (!LobbyControl.PluginConfig.Radar.Enabled.Value ||
                    !LobbyControl.PluginConfig.Radar.RemoveOnShip.Value)
                    return;

                if (!__instance.transform.IsChildOf(StartOfRound.Instance.elevatorTransform))
                    return;

                Traverse.Create(__instance).Field<bool>("_LCRadarUpdate").Value = true;
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.Update))]
            private static void UpdatePatch(GrabbableObject __instance, bool __runOriginal)
            {
                Traverse<bool> shouldUpdate = Traverse.Create(__instance).Field<bool>("_LCRadarUpdate");
                if (!__runOriginal || !shouldUpdate.Value)
                    return;

                shouldUpdate.Value = false;

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
    }
}