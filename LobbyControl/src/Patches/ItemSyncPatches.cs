using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using GameNetcodeStuff;
using HarmonyLib;
using LobbyControl.Dependency;
using Unity.Netcode;

namespace LobbyControl.Patches
{
    [HarmonyPatch]
    public class ItemSyncPatches
    {
        
        [HarmonyPatch]
        internal class GhostItemPatches
        {
            
            [HarmonyFinalizer]
            [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.GrabObjectServerRpc))]
            private static Exception CheckOverflow(PlayerControllerB __instance, Exception __exception, NetworkObjectReference grabbedObject)
            {
                if (__exception != null)
                {
                    __instance.GrabObjectClientRpc(false, grabbedObject);
                }
                
                return __exception;
            }
            
            [HarmonyTranspiler]
            [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.GrabObjectServerRpc))]
            private static IEnumerable<CodeInstruction> CheckOverflow(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> codes = instructions.ToList();
                
                var heldByInfo = typeof(GrabbableObject).GetField(nameof(GrabbableObject.heldByPlayerOnServer));
                
                var checkGrabInfo = typeof(ItemSyncPatches.GhostItemPatches).GetMethod(nameof(ItemSyncPatches.GhostItemPatches
                    .CheckGrab), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance);
                var checkOwnershipInfo = typeof(ItemSyncPatches.GhostItemPatches).GetMethod(nameof(ItemSyncPatches.GhostItemPatches
                    .CheckOwnership), BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance);

                for (var i = 0; i < codes.Count; i++)
                {
                    if (codes[i].IsLdarga())
                    {
                        if (!codes[i - 1].IsStloc())
                            continue;

                        codes[i - 3] = new CodeInstruction(OpCodes.Ldarg_0)
                        {
                            labels = codes[i - 3].labels,
                            blocks = codes[i - 3].blocks
                        };
                        codes[i - 2] = new CodeInstruction(OpCodes.Call, checkGrabInfo)
                        {
                            labels = codes[i - 2].labels,
                            blocks = codes[i - 2].blocks
                        };
                        codes.Insert( i - 2, new CodeInstruction(OpCodes.Ldarg_1)
                        {
                            blocks = codes[i - 2].blocks
                        });
                        i++;
                        LobbyControl.Log.LogDebug("Patched GrabObjectServerRpc 1!!");
                    }

                    if (codes[i].LoadsField(heldByInfo))
                    {
                        codes[i - 1] =
                            new CodeInstruction(OpCodes.Ldarg_0)
                            {
                                labels = codes[i - 1].labels,
                                blocks = codes[i - 1].blocks
                            };
                        codes[i] =
                            new CodeInstruction(OpCodes.Call, checkOwnershipInfo)
                            {
                                labels = codes[i].labels,
                                blocks = codes[i].blocks
                            };
                        
                        LobbyControl.Log.LogDebug("Patched GrabObjectServerRpc 2!!");
                    }
                }

                return codes;
            }

            private static bool CheckOwnership(NetworkObject gNetworkObject, PlayerControllerB instance)
            {
                var flag = LobbyControl.PluginConfig.ItemSync.GhostItems.Value;
                var grabbable = gNetworkObject.GetComponentInChildren<GrabbableObject>();
                return grabbable.heldByPlayerOnServer && (!flag || grabbable.playerHeldBy != instance);
            }

            private static bool CheckGrab(PlayerControllerB controllerB, NetworkObjectReference currentlyGrabbing)
            {
                
                if (!LobbyControl.PluginConfig.ItemSync.GhostItems.Value)
                    return true;
                if (ReservedItemSlotChecker.Enabled && currentlyGrabbing.TryGet(out var networkObject))
                {
                   var grabbableObject = networkObject.GetComponentInChildren<GrabbableObject>();
                   if (grabbableObject != null &&
                       ReservedItemSlotChecker.HasEmptySlotForReservedItem(controllerB, grabbableObject))
                   {
                       LobbyControl.Log.LogDebug(
                           $"Attempted Grab for {controllerB.playerUsername}, was Reserved slot");
                       return true;
                   }
                }

                var slot = controllerB.FirstEmptyItemSlot();
                var flag = slot < controllerB.ItemSlots.Length && slot >= 0;
                LobbyControl.Log.LogDebug(
                    $"Attempted Grab for {controllerB.playerUsername}, slot {slot}, max {controllerB.ItemSlots.Length}");
                if (!flag)
                {
                    LobbyControl.Log.LogError(
                        $"Grab invalidated for {controllerB.playerUsername} out of inventory space!{(LobbyControl.PluginConfig.ItemSync.ForceDrop.Value ? "" : " ( use lobby dropall to fix )")}");
                    HUDManager.Instance.AddTextToChatOnServer(
                        $"{controllerB.playerUsername} is out of inventory space!");
                    if (LobbyControl.PluginConfig.ItemSync.ForceDrop.Value)
                        controllerB.DropAllHeldItemsServerRpc();
                }
                else
                    LobbyControl.Log.LogInfo($"Grab validated for {controllerB.playerUsername}!");

                return flag;
            }
        }
        
        [HarmonyPatch]
        internal class ShotgunPatch
        {
            [HarmonyPrefix]
            [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.DestroyItemInSlotServerRpc))]
            private static bool CheckDestroy(PlayerControllerB __instance, int itemSlot)
            {
                NetworkManager networkManager = __instance.NetworkManager;
                if (networkManager == null || !networkManager.IsListening)
                    return true;
                if (__instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Server || !networkManager.IsServer && !networkManager.IsHost)
                    return true;
                if (!LobbyControl.PluginConfig.ItemSync.ShotGunReload.Value)
                    return true;

                var actualSlugSlot = -1;
                var shotgunSlot = -1;
                foreach (var item in __instance.ItemSlots)
                {
                    shotgunSlot++;
                    var shotgun = item as ShotgunItem;
                    if (shotgun != null && shotgun.isReloading)
                    {
                        actualSlugSlot = shotgun.FindAmmoInInventory();
                        break;
                    }
                }

                if (actualSlugSlot == -1 || actualSlugSlot == itemSlot)
                    return true;
                
                LobbyControl.Log.LogWarning($"{__instance.playerUsername} was found de-synced while reloading shotgun! attempting sync.");
                                
                if (!ReservedItemSlotChecker.Enabled || !ReservedItemSlotChecker.CheckIfReservedItemSlot(__instance, shotgunSlot))
                    __instance.SwitchToItemSlot(shotgunSlot);
                
                //give the client the same slot back
                ClientRpcParams clientRpcParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new []{__instance.actualClientId}
                    }
                };
                FastBufferWriter bufferWriter = __instance.__beginSendClientRpc(899109231U, clientRpcParams, RpcDelivery.Reliable);
                BytePacker.WriteValueBitPacked(bufferWriter, itemSlot);
                __instance.__endSendClientRpc(ref bufferWriter, 899109231U, clientRpcParams, RpcDelivery.Reliable);
                
                //tell ourselves the right id and do not update the other clients
                ClientRpcParams clientRpcParams2 = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new []{NetworkManager.Singleton.LocalClientId}
                    }
                };
                FastBufferWriter bufferWriter2 = __instance.__beginSendClientRpc(899109231U, clientRpcParams2, RpcDelivery.Reliable);
                BytePacker.WriteValueBitPacked(bufferWriter2, actualSlugSlot);
                __instance.__endSendClientRpc(ref bufferWriter2, 899109231U, clientRpcParams2, RpcDelivery.Reliable);
                
                return false;
            }
            
        }
        
        [HarmonyPatch]
        internal class GenericUsablePatch
        {
            [HarmonyPrefix]
            [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.ActivateItemClientRpc))]
            private static void CheckUse(GrabbableObject __instance)
            {
                NetworkManager networkManager = __instance.NetworkManager;
                if (networkManager == null || !networkManager.IsListening)
                    return;
                if (__instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Client || !networkManager.IsClient && !networkManager.IsHost || __instance.IsOwner)
                    return;
                if (!LobbyControl.PluginConfig.ItemSync.SyncOnUse.Value)
                    return;
                if(LobbyControl.PluginConfig.ItemSync.SyncIgnoreName.Value.Split(',').Contains(__instance.itemProperties.itemName))
                    return;

                if (__instance == __instance.playerHeldBy.currentlyHeldObjectServer)
                    return;
                
                var itemSlot = -1;
                for (var i=0; i < __instance.playerHeldBy.ItemSlots.Length; i++)
                {
                    var item = __instance.playerHeldBy.ItemSlots[i];
                    if (item == __instance)
                    {
                        itemSlot = i;
                        break;
                    }
                }

                if (itemSlot == -1)
                    return;
                
                if (ReservedItemSlotChecker.Enabled && ReservedItemSlotChecker.CheckIfReservedItemSlot(__instance.playerHeldBy, itemSlot))
                    return;
                
                LobbyControl.Log.LogWarning($"{__instance.playerHeldBy.playerUsername} was found de-synced while using an item! attempting sync.");
                __instance.playerHeldBy.SwitchToItemSlot(itemSlot);
            }
            
            [HarmonyPrefix]
            [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.InteractLeftRightClientRpc))]
            private static void CheckInteract(GrabbableObject __instance)
            {
                NetworkManager networkManager = __instance.NetworkManager;
                if (networkManager == null || !networkManager.IsListening)
                    return;
                if (__instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Client || !networkManager.IsClient && !networkManager.IsHost || __instance.IsOwner)
                    return;
                if (!LobbyControl.PluginConfig.ItemSync.SyncOnInteract.Value)
                    return;
                if(LobbyControl.PluginConfig.ItemSync.SyncIgnoreName.Value.Split(',').Contains(__instance.itemProperties.itemName))
                    return;

                if (__instance == __instance.playerHeldBy.currentlyHeldObject)
                    return;
                
                var itemSlot = -1;
                for (var i=0; i < __instance.playerHeldBy.ItemSlots.Length; i++)
                {
                    var item = __instance.playerHeldBy.ItemSlots[i];
                    if (item == __instance)
                    {
                        itemSlot = i;
                        break;
                    }
                }

                if (itemSlot == -1)
                    return;
                
                if (ReservedItemSlotChecker.Enabled && ReservedItemSlotChecker.CheckIfReservedItemSlot(__instance.playerHeldBy, itemSlot))
                    return;
                
                LobbyControl.Log.LogWarning($"{__instance.playerHeldBy.playerUsername} was found de-synced while interacting with an item! attempting sync.");
                __instance.playerHeldBy.SwitchToItemSlot(itemSlot);
            }
            
        }
        
    }
}