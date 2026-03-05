using System.Collections.Generic;
using Core;
using Scripts.Core;
using UnityEngine;

namespace Manager
{
    public partial class UnitManager : MonoBehaviour
    {
        public readonly List<UnitRuntimeData> units = new();
        private static UnitManager _instance;

        private void Awake()
        {
            _instance = this;
            SpawnUnit(UnitRuntimeData.Player);
        }

        void Update()
        {
            float deltaTime = Time.deltaTime;

            HandleTargeting();
            MoveUnits(deltaTime);
            HandleAttack(deltaTime);
            HandleDeath();
        }
        
        public static int SpawnUnit(UnitRuntimeData data)
        {
            data.id = _instance.units.Count;
            _instance.units.Add(data);
            return data.id;
        }
        
        void MoveUnits(float dt)
        {
            for (int i = 0; i < units.Count; i++)
            {
                var unit = units[i];
                if (!unit.alive) continue;

                if (unit.faction == 1) // Enemy
                {
                    if (unit.targetIndex < 0) continue;
                    var target = GetUnitByIndex(unit.targetIndex);
                    Vector3 dir = (target.position - unit.position).normalized; // 示例
                    unit.position += dir * unit.moveSpeed * dt;
                    units[i] = unit;
                }
            }
        }

        
        void HandleTargeting()
        {
            for (int i = 0; i < units.Count; i++)
            {
                if (!units[i].alive) continue;

                
                float minDist = float.MaxValue;
                int closest = -1;

                for (int j = 0; j < units.Count; j++)
                {
                    if (!units[j].alive) continue;
                    if (units[j].faction == units[i].faction) continue; // 同阵营则忽略

                    float dist = (units[j].position - units[i].position).sqrMagnitude;

                    if (dist < minDist)
                    {
                        minDist = dist;
                        closest = j;
                    }
                }

                var unit = units[i];
                unit.targetIndex = closest;
                units[i] = unit;
                
                // if (units[i].faction == 0) // Player
                // {
                //     float minDist = float.MaxValue;
                //     int closest = -1;
                //
                //     for (int j = 0; j < units.Count; j++)
                //     {
                //         if (!units[j].alive) continue;
                //         if (units[j].faction != 1) continue;
                //
                //         float dist = (units[j].position - units[i].position).sqrMagnitude;
                //
                //         if (dist < minDist)
                //         {
                //             minDist = dist;
                //             closest = j;
                //         }
                //     }
                //
                //     var unit = units[i];
                //     unit.targetIndex = closest;
                //     units[i] = unit;
                // }
                // else
                // {
                //     float minDist = float.MaxValue;
                //     int closest = -1;
                //
                //     for (int j = 0; j < units.Count; j++)
                //     {
                //         if (!units[j].alive) continue;
                //         if (units[j].faction != 0) continue;
                //
                //         float dist = (units[j].position - units[i].position).sqrMagnitude;
                //
                //         if (dist < minDist)
                //         {
                //             minDist = dist;
                //             closest = j;
                //         }
                //     }
                //
                //     var unit = units[i];
                //     unit.targetIndex = closest;
                //     units[i] = unit;
                // }
            }
        }

        
        void HandleAttack(float dt)
        {
            for (int i = 0; i < units.Count; i++)
            {
                if (!units[i].alive) continue;

                var unit = units[i];

                if (unit.targetIndex < 0) continue;

                unit.attackTimer += dt;
                
                var target = units[unit.targetIndex];
                
                // 在攻击范围内，并且攻击间隔已到才进行攻击
                if (Vector3.Distance(target.position, unit.position) < unit.attackRange && unit.attackTimer >= unit.attackInterval)
                {
                    if (units[unit.targetIndex].alive)
                    {
                        // 这里应该是出发攻击动作，造成实质伤害应该放在伤害判定系统中做，目前简单处理
                        target.hp -= unit.attack;
                        Debug.Log($"{unit.name}攻击了{target.name},造成了{unit.attack}点伤害");
                        units[unit.targetIndex] = target;
                    }

                    unit.attackTimer = 0;
                }

                units[i] = unit;
            }
        }

        void HandleDeath()
        {
            for (int i = 0; i < units.Count; i++)
            {
                if (!units[i].alive) continue;

                if (units[i].hp <= 0)
                {
                    var unit = units[i];
                    unit.alive = false;
                    units[i] = unit;

                    Debug.Log($"_{unit.name}_噶了");
                    if (unit.faction == Faction.Enemy)
                    {
                        LevelSystem.GotExp(unit.killExp);
                    }
                }
            }
        }

        UnitRuntimeData GetUnitByIndex(int index)
        {
            if (index >= units.Count) return UnitRuntimeData.Empty;
            return units[index];
        }

    }

}