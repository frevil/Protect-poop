using System;
using System.Collections.Generic;
using Core;
using UnityEngine;

namespace Manager.AttackBehaviors
{
    public sealed class AttackContext
    {
        private readonly Func<string, float, GameObject> _projectileFactory;
        private readonly Func<int, LineRenderer> _tongueFactory;
        private readonly Action<int, float, string> _damageApplier;

        public AttackContext(
            List<UnitRuntimeData> units,
            float dt,
            Transform effectRoot,
            Func<string, float, GameObject> projectileFactory,
            Func<int, LineRenderer> tongueFactory,
            Action<int, float, string> damageApplier)
        {
            Units = units;
            Dt = dt;
            EffectRoot = effectRoot;
            _projectileFactory = projectileFactory;
            _tongueFactory = tongueFactory;
            _damageApplier = damageApplier;
        }

        public List<UnitRuntimeData> Units { get; }
        public float Dt { get; }
        public Transform EffectRoot { get; }

        public GameObject CreateProjectileVisual(string texturePath, float size)
        {
            return _projectileFactory(texturePath, size);
        }

        public LineRenderer CreateTongue(int frogId)
        {
            return _tongueFactory(frogId);
        }

        public void ApplyDamage(int targetIndex, float damage, string attackerName)
        {
            _damageApplier(targetIndex, damage, attackerName);
        }

        public bool IsValidTargetIndex(int targetIndex)
        {
            return targetIndex >= 0 && targetIndex < Units.Count;
        }
    }
}
