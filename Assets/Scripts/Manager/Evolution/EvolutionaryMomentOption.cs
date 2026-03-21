using System;

namespace Manager.Evolution
{
    [Serializable]
    public class EvolutionaryMomentOption
    {
        public string id;
        public string title;
        public string description;

        // talent: stat_buff / skill
        public string optionType;

        // target: specific_unit_type / all_companions / all_enemies
        public string targetType;
        public string targetUnitType;

        public float attackIntervalDelta;
        public float attackRangeDelta;
        public float attackDelta;
        public int projectileCountDelta;
        public float expGainMultiplierDelta;
        public float nutritionDropChanceOnKill;
        public float healOnKill;
        public int killCountThreshold;
        public float permanentAttackSpeedGainPerThreshold;
        public float slowPercent;
        public float slowDuration;
        public float poisonDamagePerSecond;
        public float poisonDuration;
        public bool poisonDamageDoublesOnSlowed;
        public bool poisonSpreadOnDeath;
        public float burstChance;
        public int burstProjectileCount;

        // skill config
        public string skillId;
        public float skillCooldown;
        public float skillDuration;
        public float skillAttackIntervalScale;

        // unlock config
        public bool unlockedByDefault;
        public string requiredCompanionUnitType;
        public string[] nextOptionIds;
    }
}
