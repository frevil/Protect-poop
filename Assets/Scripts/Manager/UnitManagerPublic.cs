using System.Collections.Generic;
using Core;
using Manager.Evolution;

namespace Manager
{
    public partial class UnitManager
    {
        public static List<UnitRuntimeData> GetUnits()
        {
            return _instance.units;
        }

        public static int SpawnUnit(UnitRuntimeData data)
        {
            data.id = _instance.units.Count;
            _instance.units.Add(data);
            return data.id;
        }

        public static void ApplyEvolutionaryMomentOption(EvolutionaryMomentOption option)
        {
            for (var i = 0; i < _instance.units.Count; i++)
            {
                var unit = _instance.units[i];
                if (!unit.alive) continue;
                if (unit.unitType != "Frog") continue;

                unit.attackInterval += option.attackIntervalDelta;
                unit.attackInterval = unit.attackInterval < 0.1f ? 0.1f : unit.attackInterval;

                unit.attackRange += option.attackRangeDelta;
                unit.attack += option.attackDelta;

                _instance.units[i] = unit;
            }
        }
    }
}
