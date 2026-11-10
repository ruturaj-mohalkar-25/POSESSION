using UnityEngine;

namespace Possession.Warehouse
{
    // Compatibility for authored Warehouse scenes; all visuals now come from the shared component.
    [DisallowMultipleComponent, RequireComponent(typeof(EjectShieldVisual))]
    public sealed class WarehouseEjectShield : MonoBehaviour
    {
        public float ProtectionSeconds => GetComponent<PossessionController>().EjectProtectionSeconds;
        public bool IsVisible => GetComponent<EjectShieldVisual>().IsVisible;
    }
}
