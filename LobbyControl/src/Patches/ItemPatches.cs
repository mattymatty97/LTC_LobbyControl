using System;
using System.Collections.Generic;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace LobbyControl.Patches
{
    [HarmonyPatch]
    internal class ItemPatches
    {
        private static readonly HashSet<Item> ComputedItems = new HashSet<Item>();

        private static readonly Dictionary<string, List<float>> ItemFixes = new Dictionary<string, List<float>>
        {
            {
                "Flashlight[6]",
                new List<float>(3) { 90f, 0f, 90f }
            },
            {
                "Jetpack[13]",
                new List<float>(3) { 45f, 0f, 0f }
            },
            {
                "Key[14]",
                new List<float>(3) { 180f, 0f, 90f }
            },
            {
                "Apparatus[3]",
                new List<float>(3) { 0f, 0f, 135f }
            },
            {
                "Pro-flashlight[1]",
                new List<float>(3) { 90f, 0f, 90f }
            },
            {
                "Shovel[7]",
                new List<float>(3) { 0f, -90f, -90f }
            },
            {
                "Stun grenade[12]",
                new List<float>(3) { 0f, 0f, 90f }
            },
            {
                "Extension ladder[17]",
                new List<float>(3) { 0f, 0f, 0f }
            },
            {
                "TZP-Inhalant[15]",
                new List<float>(3) { 0f, 0f, -90f }
            },
            {
                "Zap gun[5]",
                new List<float>(3) { 95f, 0f, 90f }
            },
            {
                "Magic 7 ball[0]",
                new List<float>(3) { 0f, 0f, 0f }
            },
            {
                "Airhorn[0]",
                new List<float>(3) { 0f, 90f, 270f }
            },
            {
                "Big bolt[0]",
                new List<float>(3) { -21f, 0f, 0f }
            },
            {
                "Bottles[0]",
                new List<float>(3) { -90f, 0f, 0f }
            },
            {
                "Brush[0]",
                new List<float>(3) { 90f, 0f, 0f }
            },
            {
                "Candy[0]",
                new List<float>(3) { 90f, 0f, 0f }
            },
            {
                "Chemical jug[0]",
                new List<float>(3) { -90f, 0f, 0f }
            },
            {
                "Clown horn[0]",
                new List<float>(3) { -90f, 0f, 0f }
            },
            {
                "Large axle[0]",
                new List<float>(3) { 7f, 0f, 0f }
            },
            {
                "Teeth[0]",
                new List<float>(3) { -90f, 0f, 0f }
            },
            {
                "V-type engine[0]",
                new List<float>(3) { -90f, 0f, 0f }
            },
            {
                "Plastic fish[0]",
                new List<float>(3) { -45f, 0f, 90f }
            },
            {
                "Laser pointer[1]",
                new List<float>(3) { 0f, 0f, 0f }
            },
            {
                "Gold bar[0]",
                new List<float>(3) { -90f, 0f, -90f }
            },
            {
                "Magnifying glass[0]",
                new List<float>(3) { 0f, 90f, -90f }
            },
            {
                "Cookie mold pan[0]",
                new List<float>(3) { -90f, 0f, 90f }
            },
            {
                "Mug[0]",
                new List<float>(3) { -90f, 0f, 0f }
            },
            {
                "Perfume bottle[0]",
                new List<float>(3) { -90f, 0f, 0f }
            },
            {
                "Old phone[0]",
                new List<float>(3) { -90f, 0f, -90f }
            },
            {
                "Jar of pickles[0]",
                new List<float>(3) { -90f, 0f, 0f }
            },
            {
                "Pill bottle[0]",
                new List<float>(3) { -90f, 0f, 0f }
            },
            {
                "Ring[0]",
                new List<float>(3) { 0f, -90f, 90f }
            },
            {
                "Toy robot[0]",
                new List<float>(3) { -90f, 0f, 0f }
            },
            {
                "Rubber Ducky[0]",
                new List<float>(3) { -90f, 0f, -90f }
            },
            {
                "Steering wheel[0]",
                new List<float>(3) { -90f, 0f, 0f }
            },
            {
                "Toothpaste[0]",
                new List<float>(3) { -90f, 0f, 0f }
            },
            {
                "Hive[1531]",
                new List<float>(3) { 7f, 0f, 0f }
            },
            {
                "Radar-booster[16]",
                new List<float>(3) { 0f, 0f, 0f }
            },
            {
                "Shotgun[17]",
                new List<float>(3) { 180f, 0f, -5f }
            },
            {
                "Ammo[17]",
                new List<float>(3) { 0f, 0f, 90f }
            },
            {
                "Spray paint[18]",
                new List<float>(3) { 0f, 0f, 195f }
            },
            {
                "Homemade flashbang[0]",
                new List<float>(3) { 0f, 0f, 90f }
            },
            {
                "Gift[152767]",
                new List<float>(3) { -90f, 0f, 0f }
            },
            {
                "Flask[0]",
                new List<float>(3) { 25f, 0f, 0f }
            },
            {
                "Tragedy[0]",
                new List<float>(3) { -90f, 0f, 0f }
            },
            {
                "Comedy[0]",
                new List<float>(3) { -90f, 0f, 0f }
            },
            {
                "Whoopie cushion[0]",
                new List<float>(3) { -90f, 0f, 0f }
            }
        };

        internal static Vector3 FixPlacement(Vector3 hitPoint, Transform shelfTransform, GrabbableObject heldObject)
        {
            hitPoint.y = shelfTransform.position.y + shelfTransform.localScale.z / 2f;
            return hitPoint + Vector3.up * (heldObject.itemProperties.verticalOffset -
                                            LobbyControl.PluginConfig.ItemClipping.VerticalOffset.Value);
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlaceableObjectsSurface), nameof(PlaceableObjectsSurface.itemPlacementPosition))]
        private static bool ItemPlacementPositionPatch(PlaceableObjectsSurface __instance, ref Vector3 __result,
            Transform gameplayCamera, GrabbableObject heldObject)
        {
            if (!LobbyControl.PluginConfig.ItemClipping.Enabled.Value)
                return true;

            try
            {
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
            catch (Exception ex)
            {
                LobbyControl.Log.LogError($"Exception while finding the Cupboard Placement {ex}");
                return true;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.Awake))]
        private static void AwakePatch(StartOfRound __instance, bool __runOriginal)
        {
            try
            {
                foreach (var itemType in __instance.allItemsList.itemsList)
                {
                    if (itemType.spawnPrefab is null)
                        continue;

                    itemType.spawnPrefab.transform.rotation = Quaternion.Euler(itemType.restingRotation);

                    MakeMeshReadable(itemType.spawnPrefab);
                }
            }
            catch (Exception ex)
            {
                LobbyControl.Log.LogError($"Crash while setting meshes to Readable! {ex}");
            }

            if (!LobbyControl.PluginConfig.ItemClipping.Enabled.Value || !__runOriginal)
                return;

            try
            {
                foreach (var itemType in __instance.allItemsList.itemsList)
                {
                    if (itemType.spawnPrefab == null)
                        continue;

                    if (!ItemFixes.TryGetValue($"{itemType.itemName}[{itemType.itemId}]", out List<float> value))
                        value = new List<float>();

                    if (value.Count > 1)
                    {
                        itemType.restingRotation.Set(value[0], value[1], value[2]);
                        LobbyControl.Log.LogInfo(
                            $"{itemType.itemName} modified rotation is now {itemType.restingRotation}");
                    }
                    else
                    {
                        LobbyControl.Log.LogInfo(
                            $"{itemType.itemName} original rotation is now {itemType.restingRotation}");
                    }
                }
            }
            catch (Exception ex)
            {
                LobbyControl.Log.LogError($"An Object crashed badly! {ex}");
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(NetworkBehaviour), nameof(NetworkBehaviour.OnNetworkSpawn))]
        [HarmonyPriority(20)]
        private static void SpawnPostfix(NetworkBehaviour __instance)
        {
            if (!(__instance is GrabbableObject grabbable))
                return;

            if (!LobbyControl.PluginConfig.ItemClipping.RotateOnSpawn.Value)
                return;

            if (__instance.transform.name == "ClipboardManual" || __instance.transform.name == "StickyNoteItem")
                return;

            if (!StartOfRound.Instance.shipInnerRoomBounds.bounds.Contains(__instance.transform.position))
                return;

            try
            {
                grabbable.transform.rotation = Quaternion.Euler(
                    grabbable.itemProperties.restingRotation.x,
                    grabbable.floorYRot == -1
                        ? grabbable.transform.eulerAngles.y
                        : grabbable.floorYRot + grabbable.itemProperties.floorYOffset + 90f,
                    grabbable.itemProperties.restingRotation.z);
            }
            catch (Exception ex)
            {
                LobbyControl.Log.LogError($"Exception while setting rotation :{ex}");
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.Start))]
        [HarmonyPriority(20)]
        private static void StartPrefix(GrabbableObject __instance)
        {
            if (!LobbyControl.PluginConfig.ItemClipping.Enabled.Value)
                return;

            if (__instance.transform.name == "ClipboardManual" || __instance.transform.name == "StickyNoteItem")
                return;

            if (ComputedItems.Contains(__instance.itemProperties))
                return;

            Dictionary<string, float> manualOffsets = new Dictionary<string, float>();
            var offsetString = LobbyControl.PluginConfig.ItemClipping.ManualOffsets.Value;
            foreach (var entry in offsetString.Split(','))
            {
                var parts = entry.Split(':');
                if (parts.Length <= 1)
                    continue;

                var name = parts[0];
                if (float.TryParse(parts[1], out var value))
                    manualOffsets.Add(name, value);
            }

            var itemType = __instance.itemProperties;
            var go = __instance.gameObject;
            var pos = go.transform.position;
            var oldRot = go.transform.rotation;

            go.transform.rotation = Quaternion.Euler(itemType.restingRotation);

            try
            {
                if (!manualOffsets.TryGetValue(
                        itemType.itemName, out var offset))
                {
                    Bounds? bounds = CalculateColliderBounds(go);

                    if (bounds.HasValue)
                    {
                        itemType.verticalOffset = (pos.y - bounds.Value.min.y) +
                                                  LobbyControl.PluginConfig.ItemClipping.VerticalOffset
                                                      .Value;

                        LobbyControl.Log.LogInfo(
                            $"{itemType.itemName} computed vertical offset is now {itemType.verticalOffset}");
                    }
                    else
                    {
                        LobbyControl.Log.LogInfo(
                            $"{itemType.itemName} original vertical offset is {itemType.verticalOffset}");
                    }
                }
                else
                {
                    itemType.verticalOffset = offset +
                                              LobbyControl.PluginConfig.ItemClipping.VerticalOffset
                                                  .Value;

                    LobbyControl.Log.LogInfo(
                        $"{itemType.itemName} manual vertical offset is now {itemType.verticalOffset}");
                }
            }
            catch (Exception ex)
            {
                LobbyControl.Log.LogError($"{itemType.itemName} crashed! {ex}");
            }

            go.transform.rotation = oldRot;
            ComputedItems.Add(__instance.itemProperties);
        }

        internal static Bounds? CalculateColliderBounds(GameObject go)
        {
            MakeMeshReadable(go);
            MeshFilter[] meshFilters;
            var filter = go.GetComponent<MeshFilter>();
            if (filter != null)
                meshFilters = new[] { filter };
            else
            {
                meshFilters = go.GetComponentsInChildren<MeshFilter>();
            }

            Bounds? bounds = null;

            foreach (var meshFilter in meshFilters)
            {
                var collider = meshFilter.gameObject.AddComponent<MeshCollider>();
                Physics.SyncTransforms();
                collider.convex = true;
                collider.sharedMesh = null;
                collider.sharedMesh = meshFilter.mesh;
                var cbounds = collider.bounds;
                if (bounds.HasValue)
                    bounds.Value.Encapsulate(cbounds);
                else
                    bounds = cbounds;
                Object.Destroy(collider);
            }

            return bounds;
        }

        internal static void MakeMeshReadable(GameObject go)
        {
            MeshFilter[] renderers;
            var renderer = go.GetComponent<MeshFilter>();
            if (!(renderer is null))
                renderers = new[] { renderer };
            else
                renderers = go.GetComponentsInChildren<MeshFilter>();

            foreach (var meshFilter in renderers)
            {
                var mesh = meshFilter.mesh;

                if (!mesh.isReadable)
                {
                    meshFilter.mesh = MakeReadableMeshCopy(mesh);
                }
            }
        }

        public static Mesh MakeReadableMeshCopy(Mesh nonReadableMesh)
        {
            Mesh meshCopy = new Mesh();
            meshCopy.indexFormat = nonReadableMesh.indexFormat;

            // Handle vertices
            nonReadableMesh.vertexBufferTarget |= GraphicsBuffer.Target.Vertex;
            GraphicsBuffer verticesBuffer = nonReadableMesh.GetVertexBuffer(0);
            int totalSize = verticesBuffer.stride * verticesBuffer.count;
            byte[] data = new byte[totalSize];
            verticesBuffer.GetData(data);
            meshCopy.SetVertexBufferParams(nonReadableMesh.vertexCount, nonReadableMesh.GetVertexAttributes());
            meshCopy.SetVertexBufferData(data, 0, 0, totalSize);
            verticesBuffer.Release();

            // Handle triangles
            nonReadableMesh.indexBufferTarget |= GraphicsBuffer.Target.Index;
            meshCopy.subMeshCount = nonReadableMesh.subMeshCount;
            GraphicsBuffer indexesBuffer = nonReadableMesh.GetIndexBuffer();
            int tot = indexesBuffer.stride * indexesBuffer.count;
            byte[] indexesData = new byte[tot];
            indexesBuffer.GetData(indexesData);
            meshCopy.SetIndexBufferParams(indexesBuffer.count, nonReadableMesh.indexFormat);
            meshCopy.SetIndexBufferData(indexesData, 0, 0, tot);
            indexesBuffer.Release();

            // Restore submesh structure
            uint currentIndexOffset = 0;
            for (int i = 0; i < meshCopy.subMeshCount; i++)
            {
                uint subMeshIndexCount = nonReadableMesh.GetIndexCount(i);
                meshCopy.SetSubMesh(i, new SubMeshDescriptor((int)currentIndexOffset, (int)subMeshIndexCount));
                currentIndexOffset += subMeshIndexCount;
            }

            // Recalculate normals and bounds
            meshCopy.RecalculateNormals();
            meshCopy.RecalculateBounds();

            return meshCopy;
        }
        
        [HarmonyPatch(typeof(StormyWeather))]
        internal class StormyWeatherPatch
        {
            [HarmonyPrefix]
            [HarmonyPatch(nameof(StormyWeather.SetStaticElectricityWarning))]
            private static void ChangeParticleShape(StormyWeather __instance, NetworkObject warningObject)
            {
                var shapeModule = __instance.staticElectricityParticle.shape;
                if (LobbyControl.PluginConfig.LightingParticle.Enabled.Value)
                {
                    Bounds? bounds = CalculateColliderBounds(warningObject.gameObject);

                    shapeModule.shapeType = ParticleSystemShapeType.Sphere;
                    shapeModule.radiusThickness = 0.01f;
                    if (!bounds.HasValue)
                        return;
                    var diagonal = Vector3.Distance(bounds.Value.min, bounds.Value.max);
                    shapeModule.radius = diagonal / 2f;
                    shapeModule.position = bounds.Value.center - warningObject.gameObject.transform.position +
                                           Vector3.up * 0.5f;
                }
                else
                {
                    shapeModule.shapeType = ParticleSystemShapeType.MeshRenderer;
                    shapeModule.radiusThickness = 0f;
                    shapeModule.radius = 0f;
                    shapeModule.position = Vector3.zero;
                }
            }
        }
    }
}