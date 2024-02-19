using HarmonyLib;
using UnityEngine;

namespace LobbyControl.Patches
{
    [HarmonyPatch]
    internal class CupBoardFix
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.Start))]
        private static void ShipLoad1(GrabbableObject __instance)
        {
            GameObject closet = GameObject.Find("/Environment/HangarShip/StorageCloset");
            MeshCollider collider = closet.GetComponent<MeshCollider>();
            if (collider.bounds.Contains(__instance.transform.position))
            {
                __instance.transform.SetParent(closet.transform);
            }
        }
    }
}