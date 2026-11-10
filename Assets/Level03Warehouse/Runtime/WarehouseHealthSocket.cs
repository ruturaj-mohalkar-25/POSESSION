using UnityEngine;

namespace Possession.Warehouse
{
    // A placement contract for the team's incoming health pickup, not a second health system.
    [DisallowMultipleComponent]
    public sealed class WarehouseHealthSocket : MonoBehaviour
    {
        [SerializeField,Range(0f,1f)] private float suggestedRestoreFraction=.3f;
        public float SuggestedRestoreFraction=>suggestedRestoreFraction;
        public bool RestoresSpiritPool=>true;
        public bool IsGameplayBound=>false;
    }
}
