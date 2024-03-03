using System;
using LobbyControl.Patches;
using UnityEngine;

namespace LobbyControl.Components
{
    public class LCCupboardParent : MonoBehaviour
    {
        public bool ShouldSkipFall() => _skipFall;
        private GrabbableObject _grabbable;
        private bool _skipFall = false;
        private void Awake()
        {
            _grabbable = GetComponent<GrabbableObject>();
            if (_grabbable == null)
                return; 

            var tolerance = LobbyControl.PluginConfig.CupBoard.Tolerance.Value;
            try
            {
                var pos = gameObject.transform.position;
                
                GameObject closet = GameObject.Find("/Environment/HangarShip/StorageCloset");
                PlaceableObjectsSurface[] storageShelves =
                    closet.GetComponentsInChildren<PlaceableObjectsSurface>();
                MeshCollider collider = closet.GetComponent<MeshCollider>();
                float distance = float.MaxValue;
                PlaceableObjectsSurface found = null;
                Vector3? closest = null;
                

                if (collider.bounds.Contains(pos))
                {
                    foreach (var shelf in storageShelves)
                    {
                        var hitPoint = shelf.GetComponent<Collider>().ClosestPoint(pos);
                        var tmp = pos.y - hitPoint.y;
                        LobbyControl.Log.LogDebug(
                            $"{_grabbable.itemProperties.itemName}({_grabbable.gameObject.GetInstanceID()}) - Shelve is {tmp} away!");
                        if (tmp >= 0 && tmp < distance)
                        {
                            found = shelf;
                            distance = tmp;
                            closest = hitPoint;
                        }
                    }

                    LobbyControl.Log.LogDebug(
                        $"{_grabbable.itemProperties.itemName}({_grabbable.gameObject.GetInstanceID()}) - Chosen Shelve is {distance} away!");
                    LobbyControl.Log.LogDebug(
                        $"{_grabbable.itemProperties.itemName}({_grabbable.gameObject.GetInstanceID()}) - With hitpoint at {closest}!");
                }

                if (found != null && closest.HasValue)
                {
                    if (LobbyControl.PluginConfig.ItemClipping.Enabled.Value)
                    {
                        var newPos = ItemClippingPatch.FixPlacement(closest.Value, found.transform, _grabbable);
                        gameObject.transform.position = newPos;
                    }
                    else
                    {
                        gameObject.transform.position = closest.Value + Vector3.up * LobbyControl.PluginConfig.CupBoard.Shift.Value;
                    }

                    gameObject.transform.parent = closet.transform;

                    _skipFall = true;
                }
                else
                {
                    //check if we're above the closet
                    var hitPoint = collider.bounds.ClosestPoint(pos);
                    var xDelta = hitPoint.x - pos.x;
                    var zDelta = hitPoint.z - pos.z;
                    var yDelta = pos.y - hitPoint.y;
                    if (Math.Abs(xDelta) < tolerance && Math.Abs(zDelta) < tolerance && yDelta > 0)
                    {
                        LobbyControl.Log.LogDebug(
                            $"{_grabbable.itemProperties.itemName}({_grabbable.gameObject.GetInstanceID()}) - Was above the Cupboard!");
                        gameObject.transform.position = pos;

                        _skipFall = true;

                        if (Math.Abs(xDelta) > 0)
                            gameObject.transform.position += new Vector3(xDelta, 0, 0);
                        if (Math.Abs(zDelta) > 0)
                            gameObject.transform.position += new Vector3(0, 0, zDelta);
                    }
                }
            }
            catch (Exception ex)
            {
                LobbyControl.Log.LogError($"Exception while checking for Cupboard {ex}");
            }
        }
    }
}