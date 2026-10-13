using UnityEngine;

namespace Possession
{
    public class SpiritController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float jumpForce = 8f;

        [Header("Health")]
        [SerializeField] private int maxHP = 3;

        private Rigidbody rb;
        private int currentHP;
        private bool isGrounded;
        private bool isVisible = true;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            currentHP = maxHP;
        }

        private void Update()
        {
            if (!isVisible) return;
            if (GameManager.Instance != null && GameManager.Instance.CurrentState != LevelState.InProgress)
                return;

            HandleMovement();
            HandleJump();
        }

        private void HandleMovement()
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            Vector3 dir = new Vector3(h, 0f, v).normalized;
            rb.linearVelocity = new Vector3(dir.x * moveSpeed, rb.linearVelocity.y, dir.z * moveSpeed);
        }

        private void HandleJump()
        {
            if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
            {
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                isGrounded = false;
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.contacts[0].normal.y > 0.5f)
                isGrounded = true;
        }

        public void TakeDamage(int amount)
        {
            currentHP -= amount;
            EventBus.SpiritDamaged?.Invoke(currentHP, maxHP);
            if (currentHP <= 0)
                EventBus.SpiritDied?.Invoke();
        }

        public void SetVisible(bool visible)
        {
            isVisible = visible;
            isGrounded = false;
            var renderer = GetComponent<Renderer>();
            if (renderer != null) renderer.enabled = visible;
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = visible;
            if (rb != null)
            {
                if (visible) rb.isKinematic = false;
                if (!rb.isKinematic)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                if (!visible) rb.isKinematic = true;
            }
        }

        public void RestoreAt(Vector3 position)
        {
            if (rb != null) rb.position = position;
            transform.position = position;
            SetVisible(true);
        }

        public int CurrentHP => currentHP;
        public int MaxHP => maxHP;
    }
}
