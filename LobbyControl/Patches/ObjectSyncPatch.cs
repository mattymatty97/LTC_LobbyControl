using System;
using HarmonyLib;
using Unity.Netcode.Components;

namespace LobbyControl.Patches
{
    [HarmonyPatch]
    internal class ObjectSyncPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.Start))]
        private static void SyncGrabbable(GrabbableObject __instance)
        {
            try
            {
                if (!StartOfRound.Instance.IsServer) 
                    return;
                
                NetworkTransform sync = __instance.gameObject.AddComponent<NetworkTransform>();
                sync.SyncScaleX = false;
                sync.SyncScaleY = false;
                sync.SyncScaleZ = false;
                sync.InLocalSpace = true;
                sync.UseHalfFloatPrecision = true;
                sync.PositionThreshold = 0.2f;
                sync.RotAngleThreshold = 5f;
            }
            catch (Exception ex)
            {
                LobbyControl.Log.LogError(ex);
            }
        }
        
        public static bool UnlockablesDirty = false;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SpawnUnlockable))]
        private static void TrackUnlockable(StartOfRound __instance)
        {
            UnlockablesDirty = true;
        }
        
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.LateUpdate))]
        private static void SyncUnlockable(StartOfRound __instance)
        {
            try
            {
                if (!__instance.IsServer || !UnlockablesDirty) 
                    return;
                
                UnlockablesDirty = false;
                var unlockables = __instance.SpawnedShipUnlockables.Values;
                foreach (var gameObject in unlockables)
                {
                    if (gameObject.GetComponent<NetworkTransform>() != null)
                        continue;

                    NetworkTransform sync = gameObject.AddComponent<NetworkTransform>();
                    sync.SyncScaleX = false;
                    sync.SyncScaleY = false;
                    sync.SyncScaleZ = false;
                    sync.InLocalSpace = true;
                    sync.UseHalfFloatPrecision = true;
                    sync.PositionThreshold = 0.5f;
                    sync.RotAngleThreshold = 30f;
                }
            }
            catch (Exception ex)
            {
                LobbyControl.Log.LogError(ex);
            }
        }
    }
}