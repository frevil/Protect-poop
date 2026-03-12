using System;
using Core;

namespace Manager.Evolution
{
    [Serializable]
    public struct EvolutionSkillRuntime
    {
        public string sourceOptionId;
        public string skillId;
        public int ownerUnitId;

        public float cooldown;
        public float duration;
        public float attackIntervalScale;

        public float cooldownTimer;
        public float durationTimer;
        public bool isActive;

        public bool IsValidFor(UnitRuntimeData unit)
        {
            return unit.alive && unit.id == ownerUnitId;
        }
    }
}
