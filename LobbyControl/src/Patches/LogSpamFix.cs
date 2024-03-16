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
        
        [HarmonyPatch(typeof(StormyWeather))]
        internal class StormyWeatherPatch
        {
            [HarmonyPrefix]
            [HarmonyPatch(nameof(StormyWeather.SetStaticElectricityWarning))]
            private static void changeParticleShape(StormyWeather __instance, NetworkObject warningObject)
            {
                if (!LobbyControl.PluginConfig.LogSpam.Enabled.Value ||
                    !LobbyControl.PluginConfig.LogSpam.ZeroSurfaceArea.Value)
                    return;
                
                Bounds? bounds = ItemClippingPatch.CalculateBounds(warningObject.gameObject);
                
                var shapeModule = __instance.staticElectricityParticle.shape;
                
                shapeModule.shapeType = ParticleSystemShapeType.Sphere;
                shapeModule.radiusThickness = 0.01f;
                if (!bounds.HasValue)
                    return;
                var diagonal = Vector3.Distance(bounds.Value.min, bounds.Value.max);
                shapeModule.radius = diagonal / 2f;
            }
            
            [HarmonyPostfix]
            [HarmonyPatch(nameof(StormyWeather.Update))]
            private static void SetCorrectParticlePosition(StormyWeather __instance)
            {
                if (!LobbyControl.PluginConfig.LogSpam.Enabled.Value ||
                    !LobbyControl.PluginConfig.LogSpam.ZeroSurfaceArea.Value)
                    return;
                if (__instance.setStaticToObject==null)
                    return;

                var bounds = ItemClippingPatch.CalculateBounds(__instance.setStaticToObject);
                if(!bounds.HasValue)
                    return;
                
                __instance.staticElectricityParticle.transform.position = bounds.Value.center + Vector3.up * 0.5f;
            }
        }
        

    }
}