using UnityEngine;

namespace Possession
{
    // Lives on the Projectile prefab: muzzle flash when fired, spark puff where it hits.
    public class ProjectileFX : MonoBehaviour
    {
        [SerializeField] private Material flashMaterial;
        [SerializeField] private float maxAgeForImpact = 2.9f; // skip the puff when it just expires in the air

        private float age;
        private static bool quitting;

        [RuntimeInitializeOnLoadMethod]
        private static void ResetQuitFlag()
        {
            quitting = false;
            Application.quitting += () => quitting = true;
        }

        private void Start()
        {
            SimpleFX.Burst(transform.position, flashMaterial, 0.45f, 0.05f, 0.08f);
        }

        private void Update()
        {
            age += Time.deltaTime;
        }

        private void OnDestroy()
        {
            // Unity also destroys objects when the scene unloads; no effects then
            if (quitting || !gameObject.scene.isLoaded) return;
            if (age > maxAgeForImpact) return;
            SimpleFX.Burst(transform.position, flashMaterial, 0.15f, 0.7f, 0.15f);
        }
    }
}
