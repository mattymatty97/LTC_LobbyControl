using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace LobbyControl.Patches
{
    [HarmonyPatch]
    internal class OutOfBoundsItemsFix
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(NetworkBehaviour), nameof(NetworkBehaviour.OnNetworkSpawn))]
        [HarmonyPriority(Priority.First)]
        private static void ObjectCreation(NetworkBehaviour __instance)
        {
            if (!LobbyControl.PluginConfig.OutOfBounds.Enabled.Value)
                return;

            if (!(__instance is GrabbableObject obj))
                return;

            if (!obj.isInShipRoom)
                return;

            var collider = StartOfRound.Instance.shipInnerRoomBounds;

            var position = __instance.transform.position;
            if (obj.itemProperties.itemSpawnsOnGround)
                position += Vector3.up * LobbyControl.PluginConfig.OutOfBounds.VerticalOffset.Value;
            position = collider.ClosestPoint(position);
            __instance.transform.position = position;
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