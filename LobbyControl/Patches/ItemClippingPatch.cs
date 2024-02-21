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
                new List<float>(4) { 0.02f, 270f, 0f, 90f }
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
                new List<float>(4) { 0.03f, 0f, 0f, 90f }
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

        private static Vector3 FixPlacement(Vector3 hitPoint, Transform transform, GrabbableObject heldObject)
        {
            hitPoint.y = transform.position.y + transform.localScale.z / 2f;
            return hitPoint + Vector3.up * heldObject.itemProperties.verticalOffset;
        }

        [HarmonyPatch(typeof(PlaceableObjectsSurface))]
        public class PlaceableObjectsSurfacePatch
        {
            [HarmonyPrefix]
            [HarmonyPatch(nameof(PlaceableObjectsSurface.itemPlacementPosition))]
            public static bool itemPlacementPositionPatch(PlaceableObjectsSurface __instance, ref Vector3 __result,
                Transform gameplayCamera, GrabbableObject heldObject)
            {
                var val = default(RaycastHit);
                if (Physics.Raycast(gameplayCamera.position, gameplayCamera.forward, out val, 7f,
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
            private static bool _updateNextTick;

            [HarmonyPostfix]
            [HarmonyPatch(nameof(StartOfRound.Awake))]
            public static void AwakePatch(StartOfRound __instance)
            {
                foreach (var items in __instance.allItemsList.itemsList)
                {
                    if (!ItemFixes.TryGetValue($"{items.itemName}[{items.itemId}]", out List<float> value))
                        continue;

                    items.verticalOffset = value[0];
                    if (value.Count > 1) items.restingRotation.Set(value[1], value[2], value[3]);
                }
            }

            [HarmonyPostfix]
            [HarmonyPatch(nameof(StartOfRound.LoadShipGrabbableItems))]
            public static void GrabbablePatch(StartOfRound __instance)
            {
                _updateNextTick = true;
            }

            [HarmonyPostfix]
            [HarmonyPatch(nameof(StartOfRound.Update))]
            public static void UpdatePatch(StartOfRound __instance)
            {
                if (!__instance.IsServer || !_updateNextTick)
                    return;

                _updateNextTick = false;

                GrabbableObject[] objects = Object.FindObjectsOfType<GrabbableObject>();

                foreach (var itemObject in objects)
                    if (itemObject.transform.name != "ClipboardManual" && itemObject.transform.name != "StickyNoteItem")
                        itemObject.transform.rotation = Quaternion.Euler(
                            itemObject.itemProperties.restingRotation.x,
                            itemObject.floorYRot == -1
                                ? itemObject.transform.eulerAngles.y
                                : itemObject.floorYRot + itemObject.itemProperties.floorYOffset + 90f,
                            itemObject.itemProperties.restingRotation.z);
            }
        }
    }
}