using UnityEngine;
using Object = UnityEngine.Object;

namespace LobbyControl.Components
{
    public class LCRadarFix : MonoBehaviour
    {
        private GrabbableObject _grabbable;

        private void Awake()
        {
            _grabbable = GetComponent<GrabbableObject>();

            if (!LobbyControl.PluginConfig.Radar.RemoveOnShip.Value)
                return;

            if (!transform.IsChildOf(StartOfRound.Instance.elevatorTransform))
                return;

            RemoveRadarIcon();
        }

        private void OnDestroy()
        {
            if (!LobbyControl.PluginConfig.Radar.RemoveDeleted.Value)
                return;

            RemoveRadarIcon();
        }

        private void RemoveRadarIcon()
        {
            if (_grabbable != null && _grabbable.radarIcon != null && _grabbable.radarIcon.gameObject != null)
                Object.Destroy(_grabbable.radarIcon.gameObject);
        }
    }
}