using System.Collections.Generic;
using Core;
using Enemies;
using Manager.Evolution;
using Scripts.Core;
using UnityEngine;

namespace Manager.AttackBehaviors
{
    public sealed class SarcophagidaeAttackBehavior : IAttackBehavior
    {
        private const float MaggotDamageInterval = 1f;
        private const int MaggotDamageTicksBeforeHatch = 10;
        private static readonly Vector3 MaggotAttachOffset = new(0f, -0.22f, -0.05f);

        private static readonly Dictionary<int, MaggotState> MaggotStatesByUnitId = new();

        public string UnitType => "Sarcophagidae";

        public void Handle(ref UnitRuntimeData attacker, AttackContext context)
        {
            if (!context.IsValidTargetIndex(attacker.targetIndex)) return;

            var target = context.Units[attacker.targetIndex];
            if (!target.alive) return;

            var inRange = Vector3.Distance(target.position, attacker.position) <= attacker.attackRange;
            if (!inRange || attacker.attackTimer < EvolutionaryMomentSystem.GetEffectiveAttackInterval(attacker)) return;

            context.ApplyDamage(attacker.targetIndex, attacker.attack, attacker.name, attacker.id);

            if (IsCompanion(target))
            {
                SpawnAttachedMaggot(target);
            }

            attacker.attackTimer = 0;
        }

        public void Tick(AttackContext context)
        {
            for (int i = 0; i < context.Units.Count; i++)
            {
                var unit = context.Units[i];
                if (!unit.alive || unit.unitType != "Maggot") continue;

                if (!MaggotStatesByUnitId.TryGetValue(unit.id, out var state))
                {
                    unit.alive = false;
                    context.Units[i] = unit;
                    continue;
                }

                if (!UnitManager.TryGetUnitById(state.hostUnitId, out var host) || !host.alive || !IsCompanion(host))
                {
                    unit.alive = false;
                    context.Units[i] = unit;
                    continue;
                }

                unit.position = host.position + MaggotAttachOffset;
                state.damageTimer += context.Dt;

                while (state.damageTimer >= MaggotDamageInterval && host.alive)
                {
                    state.damageTimer -= MaggotDamageInterval;
                    state.damageTickCount += 1;

                    if (UnitManager.TryGetUnitIndexById(host.id, out var hostIndex))
                    {
                        context.ApplyDamage(hostIndex, 1f, "蛆", unit.id);
                        host = context.Units[hostIndex];
                    }
                }

                if (state.damageTickCount >= MaggotDamageTicksBeforeHatch)
                {
                    HatchToFly(unit, host.position);
                    unit.alive = false;
                }

                MaggotStatesByUnitId[unit.id] = state;
                context.Units[i] = unit;
            }
        }

        public void Cleanup(AttackContext context)
        {
            var staleIds = ListPool<int>.Get();
            foreach (var pair in MaggotStatesByUnitId)
            {
                if (!UnitManager.TryGetUnitById(pair.Key, out var maggotUnit) || !maggotUnit.alive)
                {
                    staleIds.Add(pair.Key);
                }
            }

            for (int i = 0; i < staleIds.Count; i++)
            {
                MaggotStatesByUnitId.Remove(staleIds[i]);
            }

            ListPool<int>.Release(staleIds);
        }

        public void ResetState()
        {
            MaggotStatesByUnitId.Clear();
        }

        private static void SpawnAttachedMaggot(UnitRuntimeData host)
        {
            var maggot = EnemiesFactor.CreateMaggot();
            maggot.name = $"蛆_{Time.frameCount}";
            maggot.position = host.position + MaggotAttachOffset;
            var maggotId = UnitManager.SpawnUnit(maggot);
            MaggotStatesByUnitId[maggotId] = new MaggotState
            {
                hostUnitId = host.id,
                damageTimer = 0f,
                damageTickCount = 0
            };
        }

        private static void HatchToFly(UnitRuntimeData maggot, Vector3 spawnPosition)
        {
            var fly = EnemiesFactor.CreateFly();
            fly.name = $"蛆化苍蝇_{Time.frameCount}";
            fly.position = spawnPosition;
            UnitManager.SpawnUnit(fly);
            MaggotStatesByUnitId.Remove(maggot.id);
        }

        private static bool IsCompanion(UnitRuntimeData unit)
        {
            return unit.faction == Faction.Player && unit.unitType != "PlayerBase";
        }

        private struct MaggotState
        {
            public int hostUnitId;
            public float damageTimer;
            public int damageTickCount;
        }

        private static class ListPool<T>
        {
            private static readonly Stack<List<T>> Pool = new();

            public static List<T> Get()
            {
                if (Pool.Count > 0)
                {
                    return Pool.Pop();
                }

                return new List<T>();
            }

            public static void Release(List<T> list)
            {
                list.Clear();
                Pool.Push(list);
            }
        }
    }
}
