using System.Collections.Generic;
using UnityEngine;

namespace Possession
{
    // The wall fader skips doors, so a door between the camera and the player could hide them.
    // Fade any door that blocks the view, and put it back once the player is visible again.
    public sealed class TutorialDoorFader : MonoBehaviour
    {
        private const float FadedOpacity = 0.2f;
        private const float FadeSpeed = 8f;
        private static readonly int OpacityId = Shader.PropertyToID("_P5OcclusionAlpha");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private sealed class DoorState
        {
            public Door door;
            public Renderer renderer;
            public Material original;
            public Material fade;
            public bool openWhenFaded;
            public float opacity = 1f;
        }

        private readonly List<DoorState> doors = new();
        private PossessionController possession;
        private Shader fadeShader;
        private Camera view;

        public void Initialize(PossessionController controller)
        {
            possession = controller;
            fadeShader = Resources.Load<Shader>("Visibility/P5WallFade");
            foreach (var door in FindObjectsByType<Door>(FindObjectsSortMode.None))
            {
                var r = door.GetComponent<Renderer>();
                if (r != null) doors.Add(new DoorState { door = door, renderer = r });
            }
        }

        private void LateUpdate()
        {
            if (fadeShader == null || possession == null) return;
            if (view == null) view = Camera.main;
            if (view == null) return;

            Transform target = possession.PossessedEnemy != null ? possession.PossessedEnemy.transform : possession.transform;
            Vector3 eye = view.transform.position;

            foreach (var s in doors)
            {
                if (s.renderer == null) continue;
                bool blocking = target != null && target.gameObject.activeInHierarchy && s.renderer.enabled
                    && (Blocks(s.renderer.bounds, eye, target.position + Vector3.up * 0.5f)
                        || Blocks(s.renderer.bounds, eye, target.position + Vector3.up * 1.5f));

                if (blocking && s.fade == null) BeginFade(s);
                if (s.fade == null) continue;

                // Door.Open swaps in its own green material; carry its colour over and keep fading
                if (s.renderer.sharedMaterial != s.fade)
                {
                    s.fade.SetColor(ColorId, s.renderer.sharedMaterial.color);
                    s.renderer.sharedMaterial = s.fade;
                }

                s.opacity = Mathf.MoveTowards(s.opacity, blocking ? FadedOpacity : 1f, FadeSpeed * Time.unscaledDeltaTime);
                s.fade.SetFloat(OpacityId, s.opacity);
                if (!blocking && s.opacity >= 0.999f) EndFade(s);
            }
        }

        private static bool Blocks(Bounds bounds, Vector3 eye, Vector3 point)
        {
            Vector3 dir = point - eye;
            float dist = dir.magnitude;
            if (dist < 0.01f || bounds.Contains(point)) return false;
            bounds.Expand(0.3f);
            return bounds.IntersectRay(new Ray(eye, dir / dist), out float hit) && hit < dist;
        }

        private void BeginFade(DoorState s)
        {
            s.original = s.renderer.sharedMaterial;
            s.openWhenFaded = s.door != null && s.door.IsOpen;
            s.fade = new Material(fadeShader) { hideFlags = HideFlags.HideAndDontSave };
            if (s.original != null) s.fade.CopyPropertiesFromMaterial(s.original);
            s.fade.SetFloat(OpacityId, 1f);
            s.opacity = 1f;
            s.renderer.sharedMaterial = s.fade;
        }

        private void EndFade(DoorState s)
        {
            if (s.renderer != null)
            {
                s.renderer.sharedMaterial = s.original;
                // The door opened while faded: show its open colour, as Door.Open would have
                if (s.door != null && s.door.IsOpen && !s.openWhenFaded)
                    s.renderer.material.color = s.fade.GetColor(ColorId);
            }
            Destroy(s.fade);
            s.fade = null;
        }

        private void OnDestroy()
        {
            foreach (var s in doors)
                if (s.fade != null)
                {
                    if (s.renderer != null) s.renderer.sharedMaterial = s.original;
                    Destroy(s.fade);
                }
        }
    }
}
