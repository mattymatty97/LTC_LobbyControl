﻿using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace LobbyControl.Patches
{
    [HarmonyPatch]
    internal class UnnamedPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(NetworkBehaviour), nameof(NetworkBehaviour.OnNetworkSpawn))]
        [HarmonyPriority(Priority.First)]
        private static void ObjectCreation(NetworkBehaviour __instance)
        {
            if (!LobbyControl.PluginConfig.UnnamedPatch.Enabled.Value)
                return;
            
            if (!(__instance is GrabbableObject obj))
                return;

            if (!obj.isInShipRoom)
                return;
            
            Collider collider = StartOfRound.Instance.shipInnerRoomBounds;

            var position = __instance.transform.position;
            if (obj.itemProperties.itemSpawnsOnGround)
                position += Vector3.up * LobbyControl.PluginConfig.UnnamedPatch.verticalOffest.Value;
            position = collider.ClosestPoint(position);
            __instance.transform.position = position;
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.DespawnPropsAtEndOfRound))]
        private static void ShipLeave(RoundManager __instance, bool despawnAllItems)
        {
            if (!LobbyControl.PluginConfig.UnnamedPatch.Enabled.Value)
                return;
            
            GrabbableObject[] objectsOfType = UnityEngine.Object.FindObjectsOfType<GrabbableObject>();
            
            Collider collider = StartOfRound.Instance.shipInnerRoomBounds;
            
            foreach (var item in objectsOfType)
            {
                if (!item.isInShipRoom)
                    continue;
                
                item.transform.position = collider.ClosestPoint(item.transform.position);
            }
            
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.OnHitGround))]
        private static void AfterFall(GrabbableObject __instance)
        {
            if (!LobbyControl.PluginConfig.UnnamedPatch.Enabled.Value)
                return;
            
            Collider collider = StartOfRound.Instance.shipInnerRoomBounds;
            
            var position = __instance.transform.position;
            position = collider.ClosestPoint(position);
            __instance.transform.position = position;
        }
    }
}