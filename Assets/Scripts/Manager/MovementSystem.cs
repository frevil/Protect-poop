using System.Collections.Generic;
using Core;
using Scripts.Core;

namespace Manager
{
    public static class MovementSystem
    {
        public static void MoveUnits(List<UnitRuntimeData> units, float dt)
        {
            for (int i = 0; i < units.Count; i++)
            {
                var unit = units[i];
                if (!unit.alive) continue;

                if (unit.faction != Faction.Enemy) continue;
                if (unit.targetIndex < 0 || unit.targetIndex >= units.Count) continue;

                var target = units[unit.targetIndex];
                var dir = (target.position - unit.position).normalized;
                unit.position += dir * unit.moveSpeed * dt;
                units[i] = unit;
            }
        }
    }
}
