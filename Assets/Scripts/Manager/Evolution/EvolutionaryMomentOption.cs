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
