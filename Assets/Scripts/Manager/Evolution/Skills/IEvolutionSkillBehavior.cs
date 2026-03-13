using Core;

namespace Manager.Evolution.Skills
{
    public interface IEvolutionSkillBehavior
    {
        string SkillId { get; }
        void Tick(ref EvolutionSkillRuntime runtime, ref UnitRuntimeData owner, EvolutionSkillContext context);
    }
}
