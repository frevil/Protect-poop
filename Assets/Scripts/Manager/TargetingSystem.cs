using System.Collections.Generic;
using Core;

namespace Manager
{
    public static class TargetingSystem
    {
        public static void UpdateTargets(List<UnitRuntimeData> units)
        {
            for (int i = 0; i < units.Count; i++)
            {
                var unit = units[i];

                if (!unit.alive) continue;

                if (!unit.CanTarget)
                {
                    unit.targetIndex = -1;
                    units[i] = unit;
                    continue;
                }

                if (unit.targetIndex >= 0 && unit.targetIndex < units.Count &&
                    units[unit.targetIndex].alive && units[unit.targetIndex].isTargetable)
                {
                    continue;
                }

                float minDist = float.MaxValue;
                int closest = -1;

                // 丽蝇特殊索敌：优先且固定盯住便便本体。
                if (unit.unitType == "BlowFly")
                {
                    for (int j = 0; j < units.Count; j++)
                    {
                        if (!units[j].alive) continue;
                        if (units[j].unitType != "PlayerBase") continue;
                        if (!units[j].isTargetable) continue;

                        closest = j;
                        break;
                    }
                }

                if (closest == -1)
                {
                    for (int j = 0; j < units.Count; j++)
                    {
                        if (!units[j].alive) continue;
                        if (!units[j].isTargetable) continue;
                        if (units[j].faction == unit.faction) continue;
                        float dist = (units[j].position - unit.position).sqrMagnitude;
                        if (dist < minDist)
                        {
                            minDist = dist;
                            closest = j;
                        }
                    }
                }

                unit.targetIndex = closest;
                units[i] = unit;
            }
        }
    }
}
