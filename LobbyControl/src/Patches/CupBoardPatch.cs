using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using LobbyControl.Components;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LobbyControl.Patches
{
    [HarmonyPatch]
    internal class CupBoardPatch
    {
            
        [HarmonyPrefix]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.LoadShipGrabbableItems))]
        public static void AddComponent(StartOfRound __instance)
        {
            if (!LobbyControl.PluginConfig.ItemClipping.Enabled.Value)
                return;
                
            foreach (var item in __instance.allItemsList.itemsList)
            {
                if (item.spawnPrefab is null)
                    continue;
                
                if (!item.spawnPrefab.GetComponent<LCCupboardParent>())
                    item.spawnPrefab.AddComponent<LCCupboardParent>();
            }
        }            
            
        [HarmonyPostfix]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.LoadShipGrabbableItems))]
        public static void RemoveComponent(StartOfRound __instance)
        {
            foreach (var item in __instance.allItemsList.itemsList)
            {
                if (item.spawnPrefab is null)
                    continue;
                
                if (item.spawnPrefab.TryGetComponent<LCCupboardParent>(out var component))
                    UnityEngine.Object.Destroy(component);
            }
        }
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.Start))]
        private static void ObjectLoad(GrabbableObject __instance, ref object[] __state)
        {
            __state = new object[] { __instance.itemProperties.itemSpawnsOnGround };

            if (__instance.TryGetComponent<LCCupboardParent>(out var component) && component.ShouldSkipFall())
            {
                __instance.itemProperties.itemSpawnsOnGround = false;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.Start))]
        private static void ObjectLoad2(GrabbableObject __instance, object[] __state, bool __runOriginal)
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
    }
}