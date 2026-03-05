using System.Collections.Generic;
using Core;
using UnityEngine;

namespace Manager
{
    public static class AttackSystem
    {
        public static void HandleAttack(List<UnitRuntimeData> units, float dt)
        {
            for (int i = 0; i < units.Count; i++)
            {
                var unit = units[i];
                if (!unit.alive) continue;
                if (unit.targetIndex < 0 || unit.targetIndex >= units.Count) continue;

                unit.attackTimer += dt;
                var target = units[unit.targetIndex];

                if (Vector3.Distance(target.position, unit.position) < unit.attackRange &&
                    unit.attackTimer >= unit.attackInterval)
                {
                    if (target.alive)
                    {
                        target.hp -= unit.attack;
                        Debug.Log($"{unit.name}攻击了{target.name},造成了{unit.attack}点伤害");
                        units[unit.targetIndex] = target;
                    }

                    unit.attackTimer = 0;
                }

                units[i] = unit;
            }
        }
    }
}
