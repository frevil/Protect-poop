using System.Collections.Generic;
using Core;
using UnityEngine;

namespace Manager.AttackBehaviors
{
    public sealed class FrogAttackBehavior : IAttackBehavior
    {
        private const float TongueSpeed = 16f;
        private const float TongueHitDistance = 0.25f;

        private static readonly Dictionary<int, FrogTongueState> FrogTongueStates = new();

        public string UnitType => "Frog";

        public void Handle(ref UnitRuntimeData frog, AttackContext context)
        {
            var hasState = FrogTongueStates.TryGetValue(frog.id, out var state);
            if (!hasState)
            {
                var target = context.Units[frog.targetIndex];
                var inRange = Vector3.Distance(target.position, frog.position) <= frog.attackRange;
                if (frog.attackTimer < frog.attackInterval || !inRange || !target.alive) return;

                state = new FrogTongueState
                {
                    targetIndex = frog.targetIndex,
                    tongueTip = frog.position,
                    phase = FrogTonguePhase.Extending,
                    lineRenderer = CreateTongueLineRenderer(context.EffectRoot, frog.id)
                };

                FrogTongueStates[frog.id] = state;
                frog.attackTimer = 0;
            }

            if (!context.IsValidTargetIndex(state.targetIndex))
            {
                FinishFrogTongue(frog.id);
                return;
            }

            var targetUnit = context.Units[state.targetIndex];
            var tipDestination = state.phase == FrogTonguePhase.Extending && targetUnit.alive
                ? targetUnit.position
                : frog.position;

            state.tongueTip = Vector3.MoveTowards(state.tongueTip, tipDestination, TongueSpeed * context.Dt);

            if (state.phase == FrogTonguePhase.Extending && targetUnit.alive &&
                Vector3.Distance(state.tongueTip, targetUnit.position) <= TongueHitDistance)
            {
                state.phase = FrogTonguePhase.Retracting;
                state.hasCapturedTarget = true;
            }

            if (state.hasCapturedTarget && targetUnit.alive)
            {
                targetUnit.position = state.tongueTip;
                context.Units[state.targetIndex] = targetUnit;
            }

            UpdateTongueLineRenderer(state, frog.position);

            if (state.phase == FrogTonguePhase.Retracting && Vector3.Distance(state.tongueTip, frog.position) <= TongueHitDistance)
            {
                if (state.hasCapturedTarget)
                {
                    context.ApplyDamage(state.targetIndex, frog.attack, frog.name);
                }

                FinishFrogTongue(frog.id);
                return;
            }

            FrogTongueStates[frog.id] = state;
        }

        public void Tick(AttackContext context)
        {
        }

        public void Cleanup(AttackContext context)
        {
            // 使用一次性HashSet判断存活，避免每条舌头状态都全量扫描单位列表（伙伴增多时会明显放大开销）。
            var aliveFrogIds = new HashSet<int>();
            for (int i = 0; i < context.Units.Count; i++)
            {
                var unit = context.Units[i];
                if (unit.unitType == UnitType && unit.alive)
                {
                    aliveFrogIds.Add(unit.id);
                }
            }

            var deadFrogIds = new List<int>();
            foreach (var item in FrogTongueStates)
            {
                if (!aliveFrogIds.Contains(item.Key))
                {
                    deadFrogIds.Add(item.Key);
                }
            }

            foreach (var deadFrogId in deadFrogIds)
            {
                FinishFrogTongue(deadFrogId);
            }
        }


        private static LineRenderer CreateTongueLineRenderer(Transform effectRoot, int frogId)
        {
            var go = new GameObject($"FrogTongue_{frogId}");
            go.transform.SetParent(effectRoot, false);

            var lineRenderer = go.AddComponent<LineRenderer>();
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startWidth = 0.12f;
            lineRenderer.endWidth = 0.08f;
            lineRenderer.positionCount = 2;
            lineRenderer.useWorldSpace = true;
            lineRenderer.startColor = new Color(0.95f, 0.55f, 0.65f);
            lineRenderer.endColor = new Color(0.95f, 0.45f, 0.6f);
            lineRenderer.sortingOrder = 11;

            return lineRenderer;
        }

        private static void FinishFrogTongue(int frogId)
        {
            if (!FrogTongueStates.TryGetValue(frogId, out var state)) return;

            if (state.lineRenderer != null)
            {
                Object.Destroy(state.lineRenderer.gameObject);
            }

            FrogTongueStates.Remove(frogId);
        }

        private static void UpdateTongueLineRenderer(FrogTongueState state, Vector3 frogPosition)
        {
            if (state.lineRenderer == null) return;

            state.lineRenderer.SetPosition(0, frogPosition);
            state.lineRenderer.SetPosition(1, state.tongueTip);
        }

        private enum FrogTonguePhase
        {
            Extending,
            Retracting
        }

        private struct FrogTongueState
        {
            public int targetIndex;
            public bool hasCapturedTarget;
            public FrogTonguePhase phase;
            public Vector3 tongueTip;
            public LineRenderer lineRenderer;
        }
    }
}
