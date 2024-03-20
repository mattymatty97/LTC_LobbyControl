using System;
using BepInEx;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

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
        
        [HarmonyPatch]
        internal class AudioSpatializerPatch
        {
            [HarmonyPostfix]
            [HarmonyPatch(typeof(NetworkSceneManager), nameof(NetworkSceneManager.OnSceneLoaded))]
            private static void DisableSpatializers()
            {
                
                if (!LobbyControl.PluginConfig.LogSpam.AudioSpatializer.Value || !AudioSettings.GetSpatializerPluginName().IsNullOrWhiteSpace())
                    return;
                
                try
                {
                    AudioSource[] array = Resources.FindObjectsOfTypeAll<AudioSource>();
                    foreach (AudioSource val in array)
                    {
                        if (val.spatialize)
                        {
                            val.spatialize = false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LobbyControl.Log.LogError($"Exception disabling spatializers: {ex}");
                }
            }
        }

    }
}