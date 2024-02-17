using HarmonyLib;
using UnityEngine;

namespace LobbyControl.Patches
{
    [HarmonyPatch(typeof(StartOfRound))]
    internal class RadarPatch
    {
        private static bool UpdateNextTick = false;
			
        [HarmonyPostfix]
        [HarmonyPatch(nameof(StartOfRound.LoadShipGrabbableItems))]
        public static void GrabbablePatch(StartOfRound __instance)
        {
            UpdateNextTick = true;
        }
			
        [HarmonyPostfix]
        [HarmonyPatch(nameof(StartOfRound.Update))]
        public static void UpdatePatch(StartOfRound __instance)
        {
            
            if (!__instance.IsServer || !UpdateNextTick) 
                return;

            UpdateNextTick = false;
				
            GrabbableObject[] objects = UnityEngine.Object.FindObjectsOfType<GrabbableObject>();
					
            foreach (GrabbableObject itemObject in objects)
            {
                if (itemObject.radarIcon != null && itemObject.radarIcon.gameObject != null)
                {
                    Object.Destroy(itemObject.radarIcon.gameObject);
                }
            }
        }
    }
}