using System.Collections.Generic;
using Core;
using Manager.AttackBehaviors;
using Manager.Evolution;
using UnityEngine;

namespace Manager
{
    public static class AttackSystem
    {
        private static Transform _effectRoot;

        public static void HandleAttack(List<UnitRuntimeData> units, float dt)
        {
            EnsureEffectRoot();

            EvolutionaryMomentSystem.TickSkills(units, dt);

            var context = new AttackContext(
                units,
                dt,
                _effectRoot,
                (targetIndex, damage, attackerName, attackerUnitId) => ApplyDamage(units, targetIndex, damage, attackerName, attackerUnitId));

            for (int i = 0; i < units.Count; i++)
            {
                var unit = units[i];
                if (!unit.alive) continue;
                if (!unit.CanAttack)
                {
                    unit.targetIndex = -1;
                    units[i] = unit;
                    continue;
                }

                if (unit.targetIndex < 0 || unit.targetIndex >= units.Count) continue;

                unit.attackTimer += dt;

                if (AttackBehaviorRegistry.TryGetBehavior(unit.unitType, out var behavior))
                {
                    behavior.Handle(ref unit, context);
                    units[i] = unit;
                    continue;
                }

                // 默认攻击逻辑保留在主系统里，特殊伙伴攻击下沉到行为类，后续新增伙伴只需注册行为即可。
                var target = units[unit.targetIndex];
                if (Vector3.Distance(target.position, unit.position) < unit.attackRange &&
                    unit.attackTimer >= EvolutionaryMomentSystem.GetEffectiveAttackInterval(unit))
                {
                    ApplyDamage(units, unit.targetIndex, unit.attack, unit.name, unit.id);
                    unit.attackTimer = 0;
                }

                units[i] = unit;
            }

            foreach (var behavior in AttackBehaviorRegistry.GetAll())
            {
                behavior.Tick(context);
                behavior.Cleanup(context);
            }
        }

        private static void EnsureEffectRoot()
        {
            if (_effectRoot != null) return;

            var go = GameObject.Find("AttackEffects");
            if (go == null)
            {
                go = new GameObject("AttackEffects");
                Object.DontDestroyOnLoad(go);
            }

            _effectRoot = go.transform;
        }

        public static void ResetState()
        {
            foreach (var behavior in AttackBehaviorRegistry.GetAll())
            {
                behavior.ResetState();
            }

            if (_effectRoot != null)
            {
                Object.Destroy(_effectRoot.gameObject);
                _effectRoot = null;
            }
        }

        private static void ApplyDamage(List<UnitRuntimeData> units, int targetIndex, float damage, string attackerName, int attackerUnitId)
        {
            if (targetIndex < 0 || targetIndex >= units.Count) return;

            var target = units[targetIndex];
            if (!target.alive) return;

            target.hp -= damage;
            target.lastDamagerUnitId = attackerUnitId;
            Debug.Log($"{attackerName}攻击了{target.name},造成了{damage}点伤害");
            units[targetIndex] = target;
        }
    }
}
