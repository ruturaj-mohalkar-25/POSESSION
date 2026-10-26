using System.Collections;
using UnityEngine;

namespace Possession
{
    // Floating name tag for a level object. Follows its object, faces the camera,
    // and switches to its "done" text when its target / door is completed.
    // Lives outside the object so stretched cubes (doors, walls) don't squash the text.
    [RequireComponent(typeof(TextMesh))]
    public class WorldLabel : MonoBehaviour
    {
        [SerializeField] private Transform follow;
        [SerializeField] private Vector3 offset = new Vector3(0f, 2f, 0f);
        [SerializeField] private Target target;
        [SerializeField] private Door door;
        [SerializeField] private string doneText = "";
        [SerializeField] private Color doneColor = new Color(0.35f, 0.95f, 0.5f);
        [SerializeField] private GameObject hideWhenDone; // e.g. the padlock on the door
        [SerializeField] private bool hiddenUntilWallBroken; // exit label sits behind the brick wall
        [SerializeField] private Renderer background; // optional dark box behind the text
        [SerializeField] private float backgroundPadding = 0.35f;
        private bool wallBroken;
        private Coroutine pulseRoutine;

        private TextMesh label;
        private Renderer labelRenderer;
        private Camera cam;

        public void Init(Transform follow, Vector3 offset, Target target, Door door, string doneText,
            GameObject hideWhenDone, bool hiddenUntilWallBroken = false, Renderer background = null)
        {
            this.background = background;
            this.hiddenUntilWallBroken = hiddenUntilWallBroken;
            this.hideWhenDone = hideWhenDone;
            this.follow = follow;
            this.offset = offset;
            this.target = target;
            this.door = door;
            this.doneText = doneText;
        }

        private void Awake()
        {
            label = GetComponent<TextMesh>();
            labelRenderer = GetComponent<Renderer>();
        }

        private void OnEnable()
        {
            EventBus.TargetHit += OnTargetHit;
            EventBus.DoorOpened += OnDoorOpened;
            EventBus.WallBroken += OnWallBroken;
        }

        private void OnDisable()
        {
            EventBus.TargetHit -= OnTargetHit;
            EventBus.DoorOpened -= OnDoorOpened;
            EventBus.WallBroken -= OnWallBroken;
        }

        private void LateUpdate()
        {
            if (follow != null)
            {
                // Hide when the object is gone (broken wall, defeated enemy)
                labelRenderer.enabled = follow.gameObject.activeInHierarchy
                    && (!hiddenUntilWallBroken || wallBroken);
                transform.position = follow.position + offset;
            }

            if (cam == null) cam = Camera.main;
            if (cam != null) transform.rotation = cam.transform.rotation;

            if (background != null) FitBackground();
        }

        private void FitBackground()
        {
            background.enabled = labelRenderer.enabled;
            if (!labelRenderer.enabled) return;

            // Sized to the current text, sitting just behind it (further from the camera)
            Bounds text = labelRenderer.localBounds;
            var t = background.transform;
            t.localPosition = new Vector3(text.center.x, text.center.y, 0.05f);
            t.localRotation = Quaternion.identity;
            t.localScale = new Vector3(text.size.x + backgroundPadding * 2f, text.size.y + backgroundPadding, 1f);
        }

        private void OnTargetHit(Target hit)
        {
            if (target != null && hit == target) MarkDone();
        }

        private void OnDoorOpened(Door opened)
        {
            if (door != null && opened == door) MarkDone();
        }

        private void OnWallBroken(BreakableWall wall)
        {
            wallBroken = true;
        }

        private void MarkDone()
        {
            if (hideWhenDone != null) hideWhenDone.SetActive(false);
            if (string.IsNullOrEmpty(doneText)) return;
            label.text = doneText;
            label.color = doneColor;
            Pulse();
        }

        private void Pulse()
        {
            if (!isActiveAndEnabled) return;
            if (pulseRoutine != null) StopCoroutine(pulseRoutine);
            pulseRoutine = StartCoroutine(PulseRoutine());
        }

        private IEnumerator PulseRoutine()
        {
            const float duration = 0.6f;
            for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
            {
                float k = 1f + 0.35f * Mathf.Sin(t / duration * Mathf.PI);
                transform.localScale = Vector3.one * k;
                yield return null;
            }
            transform.localScale = Vector3.one;
        }
    }
}
