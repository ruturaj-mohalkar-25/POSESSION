using UnityEngine;

namespace Possession.LocalLevel04
{
    public sealed class LocalLevel04Smash : SmashAbility
    {
        protected override void Execute()
        {
            Vector3 origin = transform.position + Vector3.up * .5f - transform.forward * .8f;
            if (Physics.SphereCast(origin, .8f, transform.forward, out var hit, Range,
                ~0, QueryTriggerInteraction.Ignore))
                hit.collider.GetComponent<LocalLevel04Gate>()?.TrySmash(GetComponent<HeavyEnemy>());
        }
    }
}
