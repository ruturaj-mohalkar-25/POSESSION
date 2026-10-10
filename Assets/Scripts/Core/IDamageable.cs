namespace Possession
{
    public interface IDamageable
    {
        void TakeDamage(int amount);
        int CurrentHP { get; }
    }
}
