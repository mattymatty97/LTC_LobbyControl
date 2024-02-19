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
            
            private static bool _updateNextTick;
            
            [HarmonyPostfix]
            [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.LoadShipGrabbableItems))]
            public static void GrabbablePatch(StartOfRound __instance)
            {
                _updateNextTick = true;
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Update))]
            public static void UpdatePatch(StartOfRound __instance)
            {
                if (!__instance.IsServer || !_updateNextTick)
                    return;

                _updateNextTick = false;

                GrabbableObject[] objects = Object.FindObjectsOfType<GrabbableObject>();

                foreach (var itemObject in objects)
                    if (itemObject.radarIcon != null && itemObject.radarIcon.gameObject != null)
                        Object.Destroy(itemObject.radarIcon.gameObject);
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

        [HarmonyPatch]
        internal class DespawnPatch
        {
            private static readonly Vector3 DespawnDelta = new Vector3(0, -10000, 0);

            private static readonly Dictionary<NetworkObject, bool>
                DeferredDespawns = new Dictionary<NetworkObject, bool>();
            
            [HarmonyPrefix]
            [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.DespawnPropsAtEndOfRound))]
            public static void TranslateRemovedObjects(RoundManager __instance, bool despawnAllItems)
            {
                if (!__instance.IsServer)
                    return;

                GrabbableObject[] objectsOfType = Object.FindObjectsOfType<GrabbableObject>();
                foreach (var grabbable in objectsOfType)
                {
                    if (!despawnAllItems && (grabbable.isHeld || grabbable.isInShipRoom) &&
                        !grabbable.deactivated && (!StartOfRound.Instance.allPlayersDead ||
                                                   !grabbable.itemProperties.isScrap))
                        continue;

                    var component = grabbable.gameObject.GetComponent<NetworkObject>();
                    if (component != null && component.IsSpawned) component.transform.position -= DespawnDelta;
                }
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(DepositItemsDesk), nameof(DepositItemsDesk.CheckAllPlayersSoldItemsServerRpc))]
            public static void TranslateSoldObjects(DepositItemsDesk __instance)
            {
                var networkManager = __instance.NetworkManager;
                if (networkManager == null || !networkManager.IsListening)
                    return;

                if (__instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Server ||
                    (!networkManager.IsServer && !networkManager.IsHost))
                    return;

                foreach (var networkObject in __instance.itemsOnCounterNetworkObjects)
                    if (networkObject.IsSpawned)
                        networkObject.transform.position -= DespawnDelta;
            }

            [HarmonyPrefix]
            [HarmonyPatch(typeof(NetworkObject), nameof(NetworkObject.Despawn))]
            public static bool DeferDespawn(NetworkObject __instance, bool destroy)
            {
                if (DeferredDespawns.ContainsKey(__instance))
                {
                    DeferredDespawns.Remove(__instance);
                }
                else if (__instance.GetComponentInParent<GrabbableObject>() != null)
                {
                    DeferredDespawns[__instance] = destroy;
                    return false;
                }
                
                return true;
            }

            [HarmonyPostfix]
            [HarmonyPatch(typeof(NetworkObject), nameof(NetworkObject.Update))]
            public static void HandleDeferredDespawn(NetworkObject __instance)
            {
                if (DeferredDespawns.TryGetValue(__instance, out var destroy)) 
                    __instance.Despawn(destroy);
            }
        }
    }
}