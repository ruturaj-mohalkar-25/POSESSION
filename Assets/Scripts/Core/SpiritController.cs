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

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            currentHP = maxHP;
        }

        private void Update()
        {
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
            gameObject.SetActive(visible);
        }

        public int CurrentHP => currentHP;
        public int MaxHP => maxHP;
    }
}
