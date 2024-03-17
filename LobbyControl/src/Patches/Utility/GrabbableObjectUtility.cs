using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using GameNetcodeStuff;
using HarmonyLib;
using UnityEngine;

namespace LobbyControl.Patches.Utility
{
    [HarmonyPatch]
    internal class GrabbableObjectUtility
    {
        internal enum DelayValues
        {
            OutOfBounds = 6,
            CupBoard = 4,
            CupBoardClient = -1,
            OutOfBoundsClient = -2,
            CupBoardServer= -10,
            OutOfBoundsServer = -11
        }
        
        internal struct UpdateHolder
        {
            internal Dictionary<string,int> DelayTicks;

            internal Vector3 OriginalPos;

            public UpdateHolder(Dictionary<string, int> delayTicks, Vector3 originalPos)
            {
                DelayTicks = delayTicks;
                OriginalPos = originalPos;
            }
        }

        private static readonly ConcurrentDictionary<GrabbableObject, UpdateHolder> UpdateHolders = new ConcurrentDictionary<GrabbableObject, UpdateHolder>();

        private static readonly Dictionary<string, Action<GrabbableObject,UpdateHolder>> UpdateCallbacks = new Dictionary<string, Action<GrabbableObject,UpdateHolder>>();

        internal static void AppendToHolder(GrabbableObject obj, string key, int delay, Action<GrabbableObject,UpdateHolder> callback)
        {
            if (!UpdateCallbacks.ContainsKey(key))
                UpdateCallbacks.Add(key, callback);
            
            if (!UpdateHolders.TryGetValue(obj, out var updateHolder))
            {
                var transform = obj.transform;
                UpdateHolders[obj] = updateHolder = new UpdateHolder(new Dictionary<string, int>(),transform.position);
            }
            updateHolder.DelayTicks.Add(key, delay);
        }
        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(GrabbableObject), nameof(GrabbableObject.Update))]
        private static void BeforeUpdate(GrabbableObject __instance)
        {
            try
            {
                if (UpdateHolders.TryGetValue(__instance, out var updateHolder))
                {
                    foreach (KeyValuePair<string,int> keyValuePair in updateHolder.DelayTicks.ToList())
                    {
                        var count = keyValuePair.Value;
                        if (count > 0)
                        {
                            updateHolder.DelayTicks[keyValuePair.Key] = --count;

                            if (count <= 0)
                            {
                                updateHolder.DelayTicks.Remove(keyValuePair.Key);
                                
                                if (UpdateCallbacks.TryGetValue(keyValuePair.Key, out var action))
                                    action.Invoke(__instance, updateHolder);
                                
                                if (updateHolder.DelayTicks.Count == 0)
                                    UpdateHolders.TryRemove(__instance, out _);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LobbyControl.Log.LogError(ex);
            } 
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.SyncShipUnlockablesClientRpc))]
        private static void AfterSync(StartOfRound __instance)
        {
            try
            {
                foreach (var updateHolder in UpdateHolders.Values)
                {
                    foreach (KeyValuePair<string,int> keyValuePair in updateHolder.DelayTicks.ToList())
                    {
                        switch (keyValuePair.Value)
                        {
                            case (int)DelayValues.OutOfBoundsClient:
                                updateHolder.DelayTicks[keyValuePair.Key] = (int)DelayValues.OutOfBounds;
                                break;
                            case (int)DelayValues.CupBoardClient:
                                updateHolder.DelayTicks[keyValuePair.Key] = (int)DelayValues.CupBoard;
                                break;
                        }
                    }    
                }
            }
            catch (Exception ex)
            {
                LobbyControl.Log.LogError(ex);
            } 
        }
        
        [HarmonyPostfix]
        [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.ConnectClientToPlayerObject))]
        private static void AfterConnect(PlayerControllerB __instance)
        {
            if (!__instance.IsServer)
                return;
            
            try
            {
                foreach (var updateHolder in UpdateHolders.Values)
                {
                    foreach (KeyValuePair<string,int> keyValuePair in updateHolder.DelayTicks.ToList())
                    {
                        switch (keyValuePair.Value)
                        {
                            case (int)DelayValues.OutOfBoundsServer:
                                updateHolder.DelayTicks[keyValuePair.Key] = (int)DelayValues.OutOfBounds;
                                break;
                            case (int)DelayValues.CupBoardServer:
                                updateHolder.DelayTicks[keyValuePair.Key] = (int)DelayValues.CupBoard;
                                break;
                        }
                    }    
                }
            }
            catch (Exception ex)
            {
                LobbyControl.Log.LogError(ex);
            } 
        }
    }
}