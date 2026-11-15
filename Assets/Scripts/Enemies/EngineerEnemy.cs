namespace Possession
{
    // Uses the shared patrol, detection, possession and ejection rules. Its tool
    // interaction belongs to the mechanical vault rather than a combat ability.
    public sealed class EngineerEnemy : EnemyBase
    {
        private void Reset()
        {
            moveSpeed = 5f;
            maxHP = 3;
        }
    }
}
