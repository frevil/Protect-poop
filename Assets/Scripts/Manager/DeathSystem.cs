using System.Collections.Generic;
using Core;
using Manager.Evolution;
using Scripts.Core;
using UnityEngine;

namespace Manager
{
    public static class DeathSystem
    {
        public static void HandleDeath(List<UnitRuntimeData> units)
        {
            for (int i = 0; i < units.Count; i++)
            {
                var unit = units[i];
                if (!unit.alive) continue;
                if (unit.hp > 0) continue;

                unit.alive = false;
                units[i] = unit;

                Debug.Log($"_{unit.name}_噶了");
                if (unit.faction == Faction.Enemy)
                {
                    LevelSystem.GotExp(EvolutionaryMomentSystem.GetModifiedKillExp(unit.killExp));
                    EvolutionaryMomentSystem.OnEnemyKilled(units, unit);
                }
            }
        }
    }
}
