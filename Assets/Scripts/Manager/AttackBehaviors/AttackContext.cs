using System;
using System.Collections.Generic;
using Core;
using UnityEngine;

namespace Manager.AttackBehaviors
{
    public sealed class AttackContext
    {
        private readonly Action<int, float, string, int> _damageApplier;

        public AttackContext(
            List<UnitRuntimeData> units,
            float dt,
            Transform effectRoot,
            Action<int, float, string, int> damageApplier)
        {
            Units = units;
            Dt = dt;
            EffectRoot = effectRoot;
            _damageApplier = damageApplier;
        }

        public List<UnitRuntimeData> Units { get; }
        public float Dt { get; }
        public Transform EffectRoot { get; }


        public void ApplyDamage(int targetIndex, float damage, string attackerName, int attackerUnitId)
        {
            _damageApplier(targetIndex, damage, attackerName, attackerUnitId);
        }

        public bool IsValidTargetIndex(int targetIndex)
        {
            return targetIndex >= 0 && targetIndex < Units.Count;
        }
    }
}
