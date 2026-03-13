using Core;
using Manager.Evolution;
using Manager.Evolution.Skills;

namespace Towers.FrogSkills
{
    public sealed class FrogFrenzyEvolutionSkillBehavior : IEvolutionSkillBehavior
    {
        public string SkillId => "frog_frenzy_1";

        public void Tick(ref EvolutionSkillRuntime runtime, ref UnitRuntimeData owner, EvolutionSkillContext context)
        {
            if (runtime.isActive)
            {
                runtime.durationTimer += context.Dt;
                owner.attackIntervalScale = runtime.attackIntervalScale;
                if (runtime.durationTimer < runtime.duration) return;

                runtime.isActive = false;
                runtime.durationTimer = 0f;
                runtime.cooldownTimer = 0f;
                owner.attackIntervalScale = 1f;
                return;
            }

            runtime.cooldownTimer += context.Dt;
            if (runtime.cooldownTimer >= runtime.cooldown)
            {
                runtime.isActive = true;
                runtime.durationTimer = 0f;
                owner.attackIntervalScale = runtime.attackIntervalScale;
                return;
            }

            owner.attackIntervalScale = 1f;
        }
    }
}
