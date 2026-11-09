using UnityEngine;

namespace Possession
{
    [RequireComponent(typeof(Rigidbody))]
    public class SpiritController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 8f;
        [SerializeField] private float jumpForce = 8f;
        private const float JumpBufferSeconds = .15f, CoyoteSeconds = .1f;
        private readonly RaycastHit[] groundHits = new RaycastHit[32];
        private Vector2 moveInput;
        private float jumpRequestedUntil = float.NegativeInfinity;
        private float lastGroundedAt = float.NegativeInfinity;

        [Header("Health")]
        [SerializeField] private int maxHP = 5;
        private Rigidbody rb;
        private Collider body;
        private int currentHP;
        private bool isGrounded;
        private bool isVisible = true;
        private float damageProtectedUntil;

        public float MoveSpeed => moveSpeed;
        public float JumpSpeed => jumpForce;
        public bool IsGrounded => isGrounded;
        public float DamageProtectionRemaining => Mathf.Max(0f, damageProtectedUntil - Time.time);
        public bool IsDamageProtected => Time.time < damageProtectedUntil;
        private bool CanControl => isActiveAndEnabled && isVisible && currentHP > 0
            && (GameManager.Instance == null || GameManager.Instance.CurrentState == LevelState.InProgress);

        public void GrantDamageProtection(float seconds)
        {
            if (seconds > 0f) damageProtectedUntil = Mathf.Max(damageProtectedUntil, Time.time + seconds);
        }

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            body = GetComponent<Collider>();
            currentHP = maxHP;
        }

        private void Update()
        {
            if (!CanControl || Time.timeScale <= 0f) { moveInput = Vector2.zero; return; }
            moveInput = Vector2.ClampMagnitude(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), 1f);
            if (Input.GetKeyDown(KeyCode.Space)) RequestJump();
        }

        // Space and other input frontends use the same buffered action; physics runs at a fixed rate.
        public void RequestJump()
        {
            if (CanControl && Time.timeScale > 0f) jumpRequestedUntil = Time.time + JumpBufferSeconds;
        }

        private void OnDisable()
        {
            moveInput = Vector2.zero;
            jumpRequestedUntil = lastGroundedAt = float.NegativeInfinity;
        }

        private void FixedUpdate()
        {
            if (!CanControl || rb.isKinematic) return;
            RefreshGrounding();
            rb.linearVelocity = new Vector3(moveInput.x * moveSpeed, rb.linearVelocity.y, moveInput.y * moveSpeed);
            if (Time.time > jumpRequestedUntil || (!isGrounded && Time.time - lastGroundedAt > CoyoteSeconds)) return;
            jumpRequestedUntil = lastGroundedAt = float.NegativeInfinity;
            isGrounded = false;
            // A consistent launch speed, independent of Rigidbody mass and downward landing velocity.
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
        }

        private void RefreshGrounding()
        {
            isGrounded = false;
            if (body == null) body = GetComponent<Collider>();
            if (body == null || !body.enabled || rb.linearVelocity.y > .1f) return;
            Bounds bounds = body.bounds;
            float radius = Mathf.Max(.05f, Mathf.Min(bounds.extents.x, bounds.extents.z) * .9f);
            float distance = Mathf.Max(0f, bounds.extents.y - radius) + .16f;
            int count = Physics.SphereCastNonAlloc(bounds.center, radius, Vector3.down, groundHits,
                distance, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var hit = groundHits[i];
                if (hit.normal.y < .55f || hit.collider.transform.IsChildOf(transform)
                    || hit.collider.GetComponentInParent<EnemyBase>() != null
                    || hit.collider.GetComponentInParent<SpiritController>() != null) continue;
                isGrounded = true; lastGroundedAt = Time.time; return;
            }
        }

        public void TakeDamage(int amount)
        {
            var possession = GetComponent<PossessionController>();
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != LevelState.InProgress) return;
            if (amount <= 0 || currentHP <= 0 || IsDamageProtected || possession != null && possession.IsEjectShielded) return;
            currentHP = Mathf.Max(0, currentHP - amount);
            EventBus.SpiritDamaged?.Invoke(currentHP, maxHP);
            if (currentHP == 0) EventBus.SpiritDied?.Invoke();
        }

        public void SetVisible(bool visible)
        {
            isVisible = visible;
            isGrounded = false;
            jumpRequestedUntil = lastGroundedAt = float.NegativeInfinity;
            moveInput = Vector2.zero;
            var renderer = GetComponent<Renderer>();
            if (renderer != null) renderer.enabled = visible;
            if (body == null) body = GetComponent<Collider>();
            if (body != null) body.enabled = visible;
            if (rb == null) return;
            if (visible) rb.isKinematic = false;
            if (!rb.isKinematic) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }
            if (!visible) rb.isKinematic = true;
        }

        public void RestoreAt(Vector3 position)
        {
            if (rb != null) rb.position = position;
            transform.position = position;
            SetVisible(true);
            Physics.SyncTransforms();
            RefreshGrounding();
        }
        public int CurrentHP => currentHP;
        public int MaxHP => maxHP;
    }
}
