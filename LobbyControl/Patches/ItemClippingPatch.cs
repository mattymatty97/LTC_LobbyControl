using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace LobbyControl.Patches
{
    [HarmonyPatch]
    internal class ItemClippingPatch
    {
        private static readonly Dictionary<string, List<float>> ItemFixes = new Dictionary<string, List<float>>
        {
            {
                "Flashlight[6]",
                new List<float>(4) { 0.035f, 270f, 0f, 90f }
            },
            {
                "Jetpack[13]",
                new List<float>(4) { 0.25f, 45f, 0f, 0f }
            },
            {
                "Key[14]",
                new List<float>(4) { 0.07f, 180f, 0f, 90f }
            },
            {
                "Apparatus[3]",
                new List<float>(4) { 0.25f, 0f, 0f, 135f }
            },
            {
                "Pro-flashlight[1]",
                new List<float>(4) { 0.035f, 0f, 0f, 90f }
            },
            {
                "Shovel[7]",
                new List<float>(4) { 0f, 0f, -90f, -90f }
            },
            {
                "Stun grenade[12]",
                new List<float>(4) { 0.03f, 0f, 0f, 90f }
            },
            {
                "Extension ladder[17]",
                new List<float>(4) { -0.01f, 0f, 0f, 0f }
            },
            {
                "TZP-Inhalant[15]",
                new List<float>(4) { 0.05f, 0f, 0f, -90f }
            },
            {
                "Walkie-talkie[7]",
                new List<float>(1) { 0.05f }
            },
            {
                "Zap gun[5]",
                new List<float>(4) { 0.06f, 95f, 0f, 90f }
            },
            {
                "Magic 7 ball[0]",
                new List<float>(4) { 0.1f, 0f, 0f, 0f }
            },
            {
                "Airhorn[0]",
                new List<float>(4) { 0.05f, 0f, 90f, 270f }
            },
            {
                "Big bolt[0]",
                new List<float>(4) { 0.15f, -21f, 0f, 0f }
            },
            {
                "Bottles[0]",
                new List<float>(4) { 0.25f, -90f, 0f, 0f }
            },
            {
                "Brush[0]",
                new List<float>(4) { 0.01f, 90f, 0f, 0f }
            },
            {
                "Candy[0]",
                new List<float>(4) { -0.01f, 90f, 0f, 0f }
            },
            {
                "Chemical jug[0]",
                new List<float>(4) { 0.3f, -90f, 0f, 0f }
            },
            {
                "Clown horn[0]",
                new List<float>(4) { 0.07f, -90f, 0f, 0f }
            },
            {
                "Large axle[0]",
                new List<float>(4) { 0.55f, 7f, 0f, 0f }
            },
            {
                "Teeth[0]",
                new List<float>(4) { 0.03f, -90f, 0f, 0f }
            },
            {
                "V-type engine[0]",
                new List<float>(4) { 0.3f, -90f, 0f, 0f }
            },
            {
                "Plastic fish[0]",
                new List<float>(4) { 0.08f, -45f, 0f, 90f }
            },
            {
                "Laser pointer[1]",
                new List<float>(4) { 0f, 0f, 0f, 0f }
            },
            {
                "Gold bar[0]",
                new List<float>(4) { 0.05f, -90f, 0f, -90f }
            },
            {
                "Magnifying glass[0]",
                new List<float>(4) { 0.01f, 0f, 90f, -90f }
            },
            {
                "Metal sheet[0]",
                new List<float>(1) { 0.024f }
            },
            {
                "Cookie mold pan[0]",
                new List<float>(4) { -0.01f, -90f, 0f, 90f }
            },
            {
                "Mug[0]",
                new List<float>(4) { 0.05f, -90f, 0f, 0f }
            },
            {
                "Perfume bottle[0]",
                new List<float>(4) { 0.05f, -90f, 0f, 0f }
            },
            {
                "Old phone[0]",
                new List<float>(4) { 0.04f, -90f, 0f, -90f }
            },
            {
                "Jar of pickles[0]",
                new List<float>(4) { 0.25f, -90f, 0f, 0f }
            },
            {
                "Pill bottle[0]",
                new List<float>(4) { 0.01f, -90f, 0f, 0f }
            },
            {
                "Ring[0]",
                new List<float>(4) { -0.01f, 0f, -90f, 90f }
            },
            {
                "Toy robot[0]",
                new List<float>(4) { 0.4f, -90f, 0f, 0f }
            },
            {
                "Rubber Ducky[0]",
                new List<float>(4) { 0.05f, -90f, 0f, -90f }
            },
            {
                "Steering wheel[0]",
                new List<float>(4) { 0f, -90f, 0f, 0f }
            },
            {
                "Toothpaste[0]",
                new List<float>(4) { 0f, -90f, 0f, 0f }
            },
            {
                "Hive[1531]",
                new List<float>(4) { 0.3f, 7f, 0f, 0f }
            },
            {
                "Radar-booster[16]",
                new List<float>(4) { -0.02f, 0f, 0f, 0f }
            },
            {
                "Shotgun[17]",
                new List<float>(4) { 0.08f, 180f, 0f, -5f }
            },
            {
                "Ammo[17]",
                new List<float>(4) { 0f, 0f, 0f, 90f }
            },
            {
                "Spray paint[18]",
                new List<float>(4) { 0.03f, 0f, 0f, 195f }
            },
            {
                "Homemade flashbang[0]",
                new List<float>(4) { 0.05f, 0f, 0f, 90f }
            },
            {
                "Gift[152767]",
                new List<float>(4) { 0.4f, -90f, 0f, 0f }
            },
            {
                "Flask[0]",
                new List<float>(4) { 0.19f, 25f, 0f, 0f }
            },
            {
                "Tragedy[0]",
                new List<float>(4) { 0.05f, -90f, 0f, 0f }
            },
            {
                "Comedy[0]",
                new List<float>(4) { 0.05f, -90f, 0f, 0f }
            },
            {
                "Whoopie cushion[0]",
                new List<float>(4) { -0.01f, -90f, 0f, 0f }
            }
        };

        private static readonly HashSet<Item> manualItems = new HashSet<Item>();

        internal static Vector3 FixPlacement(Vector3 hitPoint, Transform shelfTransform, GrabbableObject heldObject)
        {
            hitPoint.y = shelfTransform.position.y + shelfTransform.localScale.z / 2f;
            return hitPoint + Vector3.up * (heldObject.itemProperties.verticalOffset -
                                            (manualItems.Contains(heldObject.itemProperties)
                                                ? 0
                                                : LobbyControl.PluginConfig.ItemClipping.groundYOffset.Value));
        }

        [HarmonyPatch(typeof(PlaceableObjectsSurface))]
        public class PlaceableObjectsSurfacePatch
        {
            [HarmonyPrefix]
            [HarmonyPatch(nameof(PlaceableObjectsSurface.itemPlacementPosition))]
            public static bool itemPlacementPositionPatch(PlaceableObjectsSurface __instance, ref Vector3 __result,
                Transform gameplayCamera, GrabbableObject heldObject)
            {
                if (!LobbyControl.PluginConfig.ItemClipping.Enabled.Value)
                    return true;

                if (Physics.Raycast(gameplayCamera.position, gameplayCamera.forward, out var val, 7f,
                        StartOfRound.Instance.collidersAndRoomMask, (QueryTriggerInteraction)1))
                {
                    var bounds = __instance.placeableBounds.bounds;

                    if (bounds.Contains(val.point))
                    {
                        __result = FixPlacement(val.point, __instance.transform, heldObject);
                        return false;
                    }

                    var hitPoint = __instance.placeableBounds.ClosestPoint(val.point);
                    __result = FixPlacement(hitPoint, __instance.transform, heldObject);
                    return false;
                }

                __result = Vector3.zero;
                return false;
            }
        }

        [HarmonyPatch(typeof(StartOfRound))]
        public class StartOfRoundPatch
        {
            [HarmonyPostfix]
            [HarmonyPatch(nameof(StartOfRound.Awake))]
            public static void AwakePatch(StartOfRound __instance, bool __runOriginal)
            {
                if (!LobbyControl.PluginConfig.ItemClipping.Enabled.Value)
                    return;

                if (!__runOriginal)
                    return;

                foreach (var itemType in __instance.allItemsList.itemsList)
                {
                    if (!ItemFixes.TryGetValue($"{itemType.itemName}[{itemType.itemId}]", out List<float> value))
                        continue;
                    if (value.Count > 1)
                        itemType.restingRotation.Set(value[1], value[2], value[3]);

                    GameObject go = UnityEngine.Object.Instantiate<GameObject>(itemType.spawnPrefab, Vector3.zero,
                        Quaternion.Euler(itemType.restingRotation));
                    Renderer renderer = go.GetComponent<Renderer>();

                    if (renderer != null)
                    {
                        itemType.verticalOffset = renderer.bounds.extents.y +
                                                  LobbyControl.PluginConfig.ItemClipping.groundYOffset.Value;

                        LobbyControl.Log.LogInfo(
                            $"{itemType.itemName} computed vertical offset is now {itemType.verticalOffset}");
                    }
                    else
                    {
                        itemType.verticalOffset = value[0];
                        manualItems.Add(itemType);
                        LobbyControl.Log.LogInfo(
                            $"{itemType.itemName} manual vertical offset is now {itemType.verticalOffset}");
                    }

                    Object.Destroy(go);
                }
            }
        }

        [HarmonyPatch(typeof(GrabbableObject))]
        internal class GrabbableObjectPatch
        {
            private static readonly HashSet<GrabbableObject> ObjectsToUpdate = new HashSet<GrabbableObject>();

            [HarmonyPostfix]
            [HarmonyPatch(nameof(GrabbableObject.Start))]
            public static void StartPostfix(GrabbableObject __instance)
            {
                if (__instance.transform.name == "ClipboardManual" || __instance.transform.name == "StickyNoteItem")
                    return;
                //defer the update to the next tick
                ObjectsToUpdate.Add(__instance);
            }

            [HarmonyPrefix]
            [HarmonyPatch(nameof(GrabbableObject.Update))]
            [HarmonyPriority(Priority.First)]
            public static void UpdatePrefix(GrabbableObject __instance)
            {
                if (!ObjectsToUpdate.Remove(__instance))
                    return;

                __instance.transform.rotation = Quaternion.Euler(
                    __instance.itemProperties.restingRotation.x,
                    __instance.floorYRot == -1
                        ? __instance.transform.eulerAngles.y
                        : __instance.floorYRot + __instance.itemProperties.floorYOffset + 90f,
                    __instance.itemProperties.restingRotation.z);
            }
        }
    }
}