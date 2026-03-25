using Core;
using Manager.Evolution;
using UnityEngine;

namespace Manager.AttackBehaviors
{
    public sealed class VampireMosquitoAttackBehavior : IAttackBehavior
    {
        private const float FrenzyThresholdRatio = 0.5f;
        private const float FrenzyAttackSpeedMultiplier = 1.5f;
        private const float LifestealRatio = 1f;

        public string UnitType => "VampireMosquito";

        public void Handle(ref UnitRuntimeData attacker, AttackContext context)
        {
            if (!context.IsValidTargetIndex(attacker.targetIndex)) return;

            var target = context.Units[attacker.targetIndex];
            if (!target.alive) return;

            var inRange = Vector3.Distance(target.position, attacker.position) <= attacker.attackRange;
            var attackInterval = GetEffectiveAttackInterval(attacker);
            if (!inRange || attacker.attackTimer < attackInterval) return;

            context.ApplyDamage(attacker.targetIndex, attacker.attack, attacker.name, attacker.id);
            HealByLifesteal(ref attacker, attacker.attack * LifestealRatio);
            attacker.attackTimer = 0f;
        }

        public void Tick(AttackContext context)
        {
        }

        public void Cleanup(AttackContext context)
        {
        }

        public void ResetState()
        {
        }

        private static float GetEffectiveAttackInterval(UnitRuntimeData attacker)
        {
            var interval = EvolutionaryMomentSystem.GetEffectiveAttackInterval(attacker);
            if (attacker.hp > attacker.maxHp * FrenzyThresholdRatio) return interval;
            return interval / FrenzyAttackSpeedMultiplier;
        }

        private static void HealByLifesteal(ref UnitRuntimeData attacker, float healAmount)
        {
            if (healAmount <= 0f) return;
            attacker.hp = Mathf.Min(attacker.maxHp, attacker.hp + healAmount);
        }
    }
}
