using System;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using LogLevel = BepInEx.Logging.LogLevel;

namespace LobbyControl.Patches
{
    [HarmonyPatch]
    internal class LogSpamFix
    {
        [HarmonyPatch(typeof(EnemyAI))]
        internal class EnemyAIPatch
        {
            [HarmonyPrefix]
            [HarmonyPatch(nameof(EnemyAI.SetDestinationToPosition))]
            private static bool StopIfDead1(EnemyAI __instance)
            {
                if (!LobbyControl.PluginConfig.LogSpam.Enabled.Value ||
                    !LobbyControl.PluginConfig.LogSpam.CalculatePolygonPath.Value)
                    return true;
                
                return !__instance.isEnemyDead;
            }
            
            [HarmonyPrefix]
            [HarmonyPatch(nameof(EnemyAI.DoAIInterval))]
            private static void StopIfDead2(EnemyAI __instance)
            {
                if (!LobbyControl.PluginConfig.LogSpam.Enabled.Value ||
                    !LobbyControl.PluginConfig.LogSpam.CalculatePolygonPath.Value)
                    return;
                if (!__instance.isEnemyDead)
                    return;

                __instance.moveTowardsDestination = false;
            }
            
            [HarmonyPrefix]
            [HarmonyPatch(nameof(EnemyAI.PathIsIntersectedByLineOfSight))]
            private static bool StopIfDead3(EnemyAI __instance)
            {
                if (!LobbyControl.PluginConfig.LogSpam.Enabled.Value ||
                    !LobbyControl.PluginConfig.LogSpam.CalculatePolygonPath.Value)
                    return true;
                
                return !__instance.isEnemyDead;
            }
            
        }

    }
}