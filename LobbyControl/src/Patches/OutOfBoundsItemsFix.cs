using HarmonyLib;
using LobbyControl.Patches.Utility;
using Unity.Netcode;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LobbyControl.Patches
{
    [HarmonyPatch]
    internal class OutOfBoundsItemsFix
    {
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(NetworkBehaviour), nameof(NetworkBehaviour.OnNetworkSpawn))]
        private static void ObjectCreation(NetworkBehaviour __instance)
        {
            if (!LobbyControl.PluginConfig.OutOfBounds.Enabled.Value)
                return;
            
            if (__instance.transform.name == "ClipboardManual" || __instance.transform.name == "StickyNoteItem")
                return;

            if (!(__instance is GrabbableObject obj))
                return;

            if (!StartOfRound.Instance.shipInnerRoomBounds.bounds.Contains(__instance.transform.position))
                return;
            
            if (StartOfRound.Instance.localPlayerController != null && !StartOfRound.Instance.localPlayerController.justConnected)
                GrabbableObjectUtility.AppendToHolder(obj, nameof(CupBoardFix), (int)GrabbableObjectUtility.DelayValues.OutOfBounds, UpdateCallback);
            else if(obj.IsServer)
                GrabbableObjectUtility.AppendToHolder(obj,nameof(OutOfBoundsItemsFix), (int)GrabbableObjectUtility.DelayValues.OutOfBoundsServer, UpdateCallback);
            else
                GrabbableObjectUtility.AppendToHolder(obj,nameof(OutOfBoundsItemsFix), (int)GrabbableObjectUtility.DelayValues.OutOfBoundsClient, UpdateCallback);
        }

        private static void UpdateCallback(GrabbableObject __instance,
            GrabbableObjectUtility.UpdateHolder updateHolder)
        {
            var collider = StartOfRound.Instance.shipInnerRoomBounds;
                            
            var hangarShip = GameObject.Find("/Environment/HangarShip");
            var transform = __instance.transform;

            if (transform.parent == hangarShip.transform)
            {
                var position = updateHolder.OriginalPos;
                position += Vector3.up * LobbyControl.PluginConfig.OutOfBounds.VerticalOffset.Value;
                position = collider.ClosestPoint(position);
                transform.position = position;
                                
                __instance.FallToGround();
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.DespawnPropsAtEndOfRound))]
        private static void ShipLeave(RoundManager __instance, bool despawnAllItems)
        {
            if (!LobbyControl.PluginConfig.OutOfBounds.Enabled.Value)
                return;

            GrabbableObject[] objectsOfType = Object.FindObjectsOfType<GrabbableObject>();

            var collider = StartOfRound.Instance.shipInnerRoomBounds;

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
            if (!LobbyControl.PluginConfig.OutOfBounds.Enabled.Value)
                return;

            var collider = StartOfRound.Instance.shipInnerRoomBounds;

            var position = __instance.transform.position;
            position = collider.ClosestPoint(position);
            __instance.transform.position = position;
        }
    }
}