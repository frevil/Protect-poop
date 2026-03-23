using System.Collections.Generic;
using Core;
using Scripts.Core;
using UnityEngine;

namespace Manager
{
    public static class TargetingSystem
    {
        public static void UpdateTargets(List<UnitRuntimeData> units)
        {
            BattleViewBounds.TryGetViewRectOnBattlePlane(out var viewCenter, out var viewHalfSize);

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

                var hasValidTarget = unit.targetIndex >= 0 && unit.targetIndex < units.Count &&
                                     units[unit.targetIndex].alive && units[unit.targetIndex].isTargetable &&
                                     IsInView(units[unit.targetIndex].position, viewCenter, viewHalfSize);

                // 可攻击单位仅在「当前目标还在攻击范围内」时保留旧目标，
                // 否则要重新索敌，以便切换到新进入攻击范围的更近敌人。
                if (hasValidTarget)
                {
                    if (!unit.CanAttack)
                    {
                        continue;
                    }

                    var currentTarget = units[unit.targetIndex];
                    var inAttackRange = (currentTarget.position - unit.position).sqrMagnitude <= unit.attackRange * unit.attackRange;
                    if (inAttackRange)
                    {
                        continue;
                    }
                }

                float minDist = float.MaxValue;
                int closest = -1;

                if (unit.unitType == "Sarcophagidae")
                {
                    var companionsExist = ExistsAliveCompanionInView(units, viewCenter, viewHalfSize);

                    for (int j = 0; j < units.Count; j++)
                    {
                        if (!units[j].alive) continue;
                        if (!units[j].isTargetable) continue;
                        if (!IsInView(units[j].position, viewCenter, viewHalfSize)) continue;
                        if (units[j].faction == unit.faction) continue;

                        var isCompanion = units[j].faction == Faction.Player && units[j].unitType != "PlayerBase";
                        if (companionsExist && !isCompanion) continue;

                        float dist = (units[j].position - unit.position).sqrMagnitude;
                        if (dist < minDist)
                        {
                            minDist = dist;
                            closest = j;
                        }
                    }
                }
                // 丽蝇特殊索敌：优先且固定盯住便便本体。
                else if (unit.unitType == "BlowFly")
                {
                    for (int j = 0; j < units.Count; j++)
                    {
                        if (!units[j].alive) continue;
                        if (units[j].unitType != "PlayerBase") continue;
                        if (!units[j].isTargetable) continue;
                        if (!IsInView(units[j].position, viewCenter, viewHalfSize)) continue;

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
                        if (!IsInView(units[j].position, viewCenter, viewHalfSize)) continue;
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

        private static bool ExistsAliveCompanionInView(List<UnitRuntimeData> units, Vector2 center, Vector2 halfSize)
        {
            for (int i = 0; i < units.Count; i++)
            {
                var unit = units[i];
                if (!unit.alive) continue;
                if (!unit.isTargetable) continue;
                if (unit.faction != Faction.Player) continue;
                if (unit.unitType == "PlayerBase") continue;
                if (!IsInView(unit.position, center, halfSize)) continue;
                return true;
            }

            return false;
        }

        private static bool IsInView(Vector3 worldPosition, Vector2 center, Vector2 halfSize)
        {
            return worldPosition.x >= center.x - halfSize.x &&
                   worldPosition.x <= center.x + halfSize.x &&
                   worldPosition.y >= center.y - halfSize.y &&
                   worldPosition.y <= center.y + halfSize.y;
        }
    }
}
