using System.Collections.Generic;
using Core;

namespace Manager.Evolution.Skills
{
    public readonly struct EvolutionSkillContext
    {
        public EvolutionSkillContext(List<UnitRuntimeData> units, float dt)
        {
            Units = units;
            Dt = dt;
        }

        public List<UnitRuntimeData> Units { get; }
        public float Dt { get; }
    }
}
