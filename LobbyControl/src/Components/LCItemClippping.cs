using System;
using UnityEngine;

namespace LobbyControl.Components
{
    public class LCItemClippping : MonoBehaviour
    {
        private void Awake()
        {
            GrabbableObject _grabbable = GetComponent<GrabbableObject>();
                
            if (_grabbable is null)
                return;
            
            try
            {
                gameObject.transform.rotation = Quaternion.Euler(
                    _grabbable.itemProperties.restingRotation.x,
                    _grabbable.floorYRot == -1
                        ? gameObject.transform.eulerAngles.y
                        : _grabbable.floorYRot + _grabbable.itemProperties.floorYOffset + 90f,
                    _grabbable.itemProperties.restingRotation.z);
            }
            catch (Exception ex)
            {
                LobbyControl.Log.LogError($"Exception while setting rotation :{ex}");
            }
        }

    }
}