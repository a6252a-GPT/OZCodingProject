namespace TeamProject01.Gameplay
{
    public static class EnemyStageDifficultyApplier
    {
        public static void Apply(EnemyController enemy, WaveStageDifficulty difficulty)
        {
            if (enemy == null || enemy.Grade == EnemyGrade.Boss)
            {
                return; // 보스는 BossWaveController 쪽 밸런스에서 별도 처리
            }

            enemy.GetComponent<EnemyHealth>()?.ApplyMaxHpMultiplierKeepingRatio(difficulty.HealthMultiplier);
            enemy.GetComponent<EnemyMovement>()?.ApplyMoveSpeedMultiplier(difficulty.MoveSpeedMultiplier);
            enemy.GetComponent<EnemyMeleeAttack>()?.ApplyAttackDamageMultiplier(difficulty.NexusDamageMultiplier);
            enemy.GetComponent<EnemyRangedAttack>()?.ApplyAttackPowerMultiplier(difficulty.NexusDamageMultiplier);
        }
    }
}
