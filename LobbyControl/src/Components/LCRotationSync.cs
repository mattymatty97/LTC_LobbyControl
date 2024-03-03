using Unity.Netcode.Components;

namespace LobbyControl.Components
{
    public class LCRotationSync: NetworkTransform
    {
        private GrabbableObject _grabbable;
        
        public override void Awake()
        {
            base.Awake();
            _grabbable = GetComponent<GrabbableObject>();
            SyncPositionX = false;
            SyncPositionY = false;
            SyncPositionZ = false;
            SyncScaleX = false;
            SyncScaleY = false;
            SyncScaleZ = false;
            SyncRotAngleX = true;
            SyncRotAngleY = true;
            SyncRotAngleZ = true;
            UseHalfFloatPrecision = true;
        }

        public override void Update()
        {
            if (_grabbable && !_grabbable.isHeld)
                base.Update();
        }
    }
}