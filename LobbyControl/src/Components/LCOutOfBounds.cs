using UnityEngine;

namespace LobbyControl.Components
{
    public class LCOutOfBounds: MonoBehaviour
    {
        private GrabbableObject _grabbable;
        
        private void Awake()
        {
            _grabbable = GetComponent<GrabbableObject>();
            
            GameObject closet = GameObject.Find("/Environment/HangarShip/StorageCloset");
            
            if (_grabbable.itemProperties.itemSpawnsOnGround && gameObject.transform.parent != closet.transform)
                gameObject.transform.position += Vector3.up * LobbyControl.PluginConfig.OutOfBounds.VerticalOffset.Value;
        }

        private void FixedUpdate()
        {
            if (!_grabbable.isInShipRoom)
                return;
            
            Collider collider = StartOfRound.Instance.shipInnerRoomBounds;
            
            var position = gameObject.transform.position;
            position = collider.ClosestPoint(position);
            gameObject.transform.position = position;
        }
    }
}