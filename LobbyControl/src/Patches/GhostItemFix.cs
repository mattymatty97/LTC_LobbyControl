﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace LobbyControl.Patches
{
    [HarmonyPatch]
    public class GhostItemFix
    {

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.GrabObjectServerRpc))]
        private static IEnumerable<CodeInstruction> CheckOverflow(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> codes = instructions.ToList();

            var info = typeof(GhostItemFix).GetMethod(nameof(CheckGrab));

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
            if (!LobbyControl.PluginConfig.GhostItems.Enabled.Value)
                return true;
            
            var slot = controllerB.FirstEmptyItemSlot();
            var flag = slot < controllerB.ItemSlots.Length && slot >= 0;
            LobbyControl.Log.LogDebug($"Attempted Grab for {controllerB.playerUsername}, slot {slot}, max {controllerB.ItemSlots.Length}");
            if (!flag)
            {
                LobbyControl.Log.LogError(
                    $"Grab invalidated for {controllerB.playerUsername} out of inventory space!{(LobbyControl.PluginConfig.GhostItems.ForceDrop.Value?"":" ( use lobby dropall to fix )")}");
                HUDManager.Instance.AddTextToChatOnServer($"{controllerB.playerUsername} is out of inventory space!");
                if (LobbyControl.PluginConfig.GhostItems.ForceDrop.Value)
                    controllerB.DropAllHeldItemsServerRpc();
            }
            else
                LobbyControl.Log.LogInfo($"Grab validated for {controllerB.playerUsername}!");
            return flag;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.ThrowObjectServerRpc))]
        private static void CheckForGhostThrownObject(PlayerControllerB __instance, NetworkObjectReference grabbedObject)
        {
            if (!StartOfRound.Instance.IsServer)
                return;
            
            if (!LobbyControl.PluginConfig.GhostItems.Enabled.Value)
                return;
            
            if (!grabbedObject.TryGet(out NetworkObject networkObject))
                return;

            if (!networkObject.TryGetComponent<GrabbableObject>(out var component))
                return;
            
            if(component == __instance.currentlyHeldObject)
                return;
            
            if (!LobbyControl.PluginConfig.GhostItems.ForceDrop.Value)
                return;
            
            LobbyControl.Log.LogWarning($"{__instance.playerUsername} is throwing an object he is not holding. Attempting re-sync!");
            __instance.DropAllHeldItemsServerRpc();
            
        }
        
    }
}