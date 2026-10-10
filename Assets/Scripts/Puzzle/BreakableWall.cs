using UnityEngine;

namespace Possession
{
    public class BreakableWall : MonoBehaviour, IDamageable
    {
        [SerializeField] private int hitsToBreak = 2;

        private int currentHP;

        public int CurrentHP => currentHP;

        private void Awake()
        {
            currentHP = hitsToBreak;
        }

        public void TakeDamage(int amount)
        {
            currentHP -= amount;

            // Visual feedback: change color on damage
            var renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                float t = 1f - (float)currentHP / hitsToBreak;
                renderer.material.color = Color.Lerp(Color.gray, Color.red, t);
            }

            if (currentHP <= 0)
                Break();
        }

        private void Break()
        {
            EventBus.WallBroken?.Invoke(this);
            gameObject.SetActive(false);
        }
    }
}
