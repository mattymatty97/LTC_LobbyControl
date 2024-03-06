using System.Collections.Generic;
using System.Linq;
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

                var info = typeof(ItemSyncPatches.GhostItemPatches).GetMethod(nameof(ItemSyncPatches.GhostItemPatches.CheckGrab));

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
            [HarmonyPrefix]
            [HarmonyPatch(typeof(ShotgunItem), nameof(ShotgunItem.ReloadGunEffectsServerRpc))]
            private static void UseCorrectSlot(ShotgunItem __instance)
            {
                NetworkManager networkManager = __instance.NetworkManager;
                if (networkManager == null || !networkManager.IsListening)
                    return;
                if (__instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Server ||
                    !networkManager.IsServer && !networkManager.IsHost)
                    return;

                if (!LobbyControl.PluginConfig.ItemSync.ShotGunReload.Value)
                    return;

                if (__instance.playerHeldBy == null ||
                    __instance.playerHeldBy.currentlyHeldObjectServer == __instance)
                    return;

                LobbyControl.Log.LogWarning(
                    $"{__instance.playerHeldBy.playerUsername} is reloading a shotgun he is not holding! attempting re-sync!");
                HeldSlotPatch.UpdateQueue.Add(__instance.playerHeldBy);
            }
        }
        
        [HarmonyPatch]
        internal class HeldSlotPatch{
            internal static readonly HashSet<PlayerControllerB> UpdateQueue = new HashSet<PlayerControllerB>();
            
            [HarmonyPrefix]
            [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.ThrowObjectClientRpc))]
            private static void UseCorrectSlot(PlayerControllerB __instance, 
                NetworkObjectReference grabbedObject)
            {
                NetworkManager networkManager = __instance.NetworkManager;
                if (networkManager == null || !networkManager.IsListening)
                    return;
                if (__instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Client || !networkManager.IsClient && !networkManager.IsHost)
                    return;
                if (!LobbyControl.PluginConfig.ItemSync.MultipleDrop.Value)
                    return;
                if (!grabbedObject.TryGet(out var networkObject))
                    return;
                if (!networkObject.TryGetComponent<GrabbableObject>(out var component))
                    return;
                if (component != __instance.currentlyHeldObjectServer)
                    UpdateQueue.Add(__instance);
            }            
            
            private static readonly HashSet<ulong> SkipOnce = new HashSet<ulong>();
            private static bool _flag = false;
            
            [HarmonyPrefix]
            [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.Update))]
            private static void UpdateSlots(PlayerControllerB __instance)
            {
                if (!StartOfRound.Instance.IsServer)
                    return;
                if (!UpdateQueue.Remove(__instance))
                    return;
                LobbyControl.Log.LogWarning($"{__instance.playerUsername} might have been desynced! forcing re-sync.");
                _flag = true;
                StartOfRound.Instance.SyncAlreadyHeldObjectsServerRpc((int)__instance.actualClientId);
                _flag = false;
                SkipOnce.Add(__instance.actualClientId);
            }
            
            [HarmonyPrefix]
            [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.__rpc_handler_744998938))]
            private static void DoNotRespawn(
                NetworkBehaviour target,
                __RpcParams rpcParams)
            {
                StartOfRound __instance = (StartOfRound)target;
                if (!__instance.IsServer)
                    return;
                if (!SkipOnce.Remove(rpcParams.Server.Receive.SenderClientId))
                    return;
                LobbyControl.Log.LogDebug($"Skipping SyncShipUnlockablesServerRpc by {__instance.allPlayerScripts[__instance.ClientPlayerList[rpcParams.Server.Receive.SenderClientId]].playerUsername}.");
                _flag = true;
            }           
            
            [HarmonyPostfix]
            [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.__rpc_handler_744998938))]
            private static void ResetDoNotRespawn()
            {
                 _flag = false;
            }
            
            [HarmonyPrefix]
            [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SyncShipUnlockablesServerRpc))]
            private static bool DoNotSync(StartOfRound __instance, bool __runOriginal)
            {
                NetworkManager networkManager = __instance.NetworkManager;
                if (networkManager == null || !networkManager.IsListening)
                    return __runOriginal;
                if (__instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Server || !networkManager.IsServer && !networkManager.IsHost)
                    return __runOriginal;
                if (!_flag)
                    return __runOriginal;
                LobbyControl.Log.LogDebug("SyncShipUnlockablesServerRpc skipped");
                return false;
            }   
        }
    }
}