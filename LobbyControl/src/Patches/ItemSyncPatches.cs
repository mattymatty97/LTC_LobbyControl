using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;

namespace LobbyControl.Patches
{
    [HarmonyPatch]
    public class ItemSyncPatches
    {
        [HarmonyPatch]
        internal class GhostItemPatches
        {
            [HarmonyTranspiler]
            [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.GrabObjectServerRpc))]
            private static IEnumerable<CodeInstruction> CheckOverflow(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> codes = instructions.ToList();

                var info = typeof(ItemSyncPatches.GhostItemPatches).GetMethod(nameof(ItemSyncPatches.GhostItemPatches
                    .CheckGrab));

                for (var i = 0; i < codes.Count; i++)
                {
                    if (!codes[i].IsLdarga())
                        continue;

                    if (!codes[i - 1].IsStloc())
                        continue;

                    codes[i - 3] = new CodeInstruction(OpCodes.Ldarg_0)
                    {
                        labels = codes[i - 3].labels,
                        blocks = codes[i - 3].blocks
                    };
                    codes[i - 2] = new CodeInstruction(OpCodes.Call, info)
                    {
                        labels = codes[i - 2].labels,
                        blocks = codes[i - 2].blocks
                    };
                    LobbyControl.Log.LogDebug("Patched GrabObjectServerRpc!!");
                    break;
                }

                return codes;
            }

            public static bool CheckGrab(PlayerControllerB controllerB)
            {
                if (!LobbyControl.PluginConfig.ItemSync.GhostItems.Value)
                    return true;

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
        internal class ShotgunPatches
        {
        }

        [HarmonyPatch]
        internal class HeldSlotPatch
        {
            private static readonly MethodInfo BeginSendClientRpc = typeof(StartOfRound).GetMethod(nameof(StartOfRound.__beginSendClientRpc), BindingFlags.NonPublic | BindingFlags.Instance);
            private static readonly MethodInfo EndSendClientRpc = typeof(StartOfRound).GetMethod(nameof(StartOfRound.__endSendClientRpc), BindingFlags.NonPublic | BindingFlags.Instance);

            
            [HarmonyPostfix]
            [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.GrabObjectClientRpc))]
            private static void SyncNewlyHeldObject(PlayerControllerB __instance)
            {
                NetworkManager networkManager = __instance.NetworkManager;
                if (networkManager == null || !networkManager.IsListening)
                    return;
                if (__instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Client ||
                    !networkManager.IsClient && !networkManager.IsHost)
                    return;
                if (__instance.IsLocalPlayer)
                    return;
                if (!LobbyControl.PluginConfig.ItemSync.ForceSyncSlot.Value)
                    return;

                GrabbableObject[] objectsByType = __instance.ItemSlots;
                List<NetworkObjectReference> networkObjectReferenceList = new List<NetworkObjectReference>();
                List<int> intList1 = new List<int>();
                List<int> intList2 = new List<int>();
                List<int> intList3 = new List<int>();
                for (int index1 = 0; index1 < objectsByType.Length; ++index1)
                {
                    if (objectsByType[index1] is null || __instance.currentItemSlot != index1)
                        continue;
                    
                    intList1.Add((int)__instance.playerClientId);
                    networkObjectReferenceList.Add((NetworkObjectReference)objectsByType[index1].NetworkObject);
                    intList2.Add(index1);

                    if (objectsByType[index1].isPocketed)
                    {
                        intList3.Add(networkObjectReferenceList.Count - 1);
                    }
                }

                ClientRpcParams clientRpcParams = new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new []{__instance.actualClientId}
                    }
                };

                if (networkObjectReferenceList.Count <= 0)
                    return;

                SkipOnce.Add(__instance.actualClientId);
                // this.SyncAlreadyHeldObjectsClientRpc(networkObjectReferenceList.ToArray(), intList1.ToArray(), intList2.ToArray(), intList3.ToArray(), joiningClientId);
                var gObjects = networkObjectReferenceList.ToArray();
                var playersHeldBy = intList1.ToArray();
                var itemSlotNumbers = intList2.ToArray();
                var isObjectPocketed = intList3.ToArray();
                var syncWithClient = __instance.playerClientId;
                
                FastBufferWriter bufferWriter = (FastBufferWriter)BeginSendClientRpc.Invoke(StartOfRound.Instance, new object[]{1613265729U, clientRpcParams, RpcDelivery.Reliable});
                bufferWriter.WriteValueSafe(true);
                bufferWriter.WriteValueSafe(gObjects);
                bufferWriter.WriteValueSafe(true);
                bufferWriter.WriteValueSafe(playersHeldBy);
                bufferWriter.WriteValueSafe(true);
                bufferWriter.WriteValueSafe(itemSlotNumbers);
                bufferWriter.WriteValueSafe(true);
                bufferWriter.WriteValueSafe(isObjectPocketed);
                BytePacker.WriteValueBitPacked(bufferWriter, syncWithClient);
                EndSendClientRpc.Invoke(StartOfRound.Instance, new object[]{bufferWriter, 1613265729U, clientRpcParams, RpcDelivery.Reliable});
            }
            
            private static readonly HashSet<ulong> SkipOnce = new HashSet<ulong>();
            
            [HarmonyPrefix]
            [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.__rpc_handler_744998938))]
            private static bool DoNotRespawn(
                NetworkBehaviour target,
                __RpcParams rpcParams)
            {
                StartOfRound __instance = (StartOfRound)target;
                if (!__instance.IsServer)
                    return true;
                if (!SkipOnce.Remove(rpcParams.Server.Receive.SenderClientId))
                    return true;
                LobbyControl.Log.LogDebug($"Skipping SyncShipUnlockablesServerRpc by {__instance.allPlayerScripts[__instance.ClientPlayerList[rpcParams.Server.Receive.SenderClientId]].playerUsername}.");
                return false;
            } 
            
            [HarmonyPrefix]
            [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.OnPlayerDC))]
            private static void ClearIfDC(ulong clientId)
            {
                SkipOnce.Remove(clientId);
            } 
        }
    }
}