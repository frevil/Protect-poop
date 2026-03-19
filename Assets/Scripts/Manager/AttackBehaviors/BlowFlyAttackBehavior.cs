using System.Collections.Generic;
using Core;
using Enemies;
using Manager.Evolution;
using UnityEngine;

namespace Manager.AttackBehaviors
{
    public sealed class BlowFlyAttackBehavior : IAttackBehavior
    {
        private static readonly Vector2 EggDropHeightRange = new(0f, 0.12f);
        private const float EggFallSpeed = 3.2f;
        private const float EggHatchTime = 10f;
        private static readonly Vector2 HorizontalSpreadRange = new(-0.08f, 0.08f);
        private static readonly Vector2 SpawnJitterXRange = new(-0.01f, 0.01f);
        private static readonly Vector2 SpawnJitterYRange = new(-0.01f, 0.01f);
        private const int EggsPerAttack = 20;

        private static readonly List<EggDropState> EggDrops = new();

        public string UnitType => "BlowFly";

        public void Handle(ref UnitRuntimeData blowFly, AttackContext context)
        {
            if (!context.IsValidTargetIndex(blowFly.targetIndex)) return;

            var target = context.Units[blowFly.targetIndex];
            if (!target.alive) return;

            var inRange = Vector3.Distance(target.position, blowFly.position) <= blowFly.attackRange;
            if (!inRange || blowFly.attackTimer < EvolutionaryMomentSystem.GetEffectiveAttackInterval(blowFly)) return;

            context.ApplyDamage(blowFly.targetIndex, blowFly.attack, blowFly.name);
            SpawnEggDrops(blowFly.position);
            blowFly.attackTimer = 0;
        }

        public void Tick(AttackContext context)
        {
            for (int i = 0; i < EggDrops.Count; i++)
            {
                var egg = EggDrops[i];

                if (egg.phase == EggPhase.Falling)
                {
                    egg.position = Vector3.MoveTowards(
                        egg.position,
                        egg.landingPosition,
                        EggFallSpeed * context.Dt);

                    if (egg.visual != null)
                    {
                        egg.visual.transform.position = egg.position;
                    }

                    if (Vector3.Distance(egg.position, egg.landingPosition) <= 0.02f)
                    {
                        var eggUnit = EnemiesFactor.CreateFlyEgg();
                        eggUnit.name = $"苍蝇卵_{Time.frameCount}_{i}";
                        eggUnit.position = egg.landingPosition;
                        egg.eggUnitId = UnitManager.SpawnUnit(eggUnit);
                        egg.hatchTimer = 0;
                        egg.phase = EggPhase.WaitingToHatch;
                    }
                }
                else if (egg.phase == EggPhase.WaitingToHatch)
                {
                    egg.hatchTimer += context.Dt;
                    if (egg.hatchTimer >= EggHatchTime)
                    {
                        HatchFly(egg, context);
                        egg.phase = EggPhase.Done;
                    }
                }

                EggDrops[i] = egg;
            }
        }

        public void Cleanup(AttackContext context)
        {
            for (int i = EggDrops.Count - 1; i >= 0; i--)
            {
                if (EggDrops[i].phase != EggPhase.Done) continue;

                if (EggDrops[i].visual != null)
                {
                    Object.Destroy(EggDrops[i].visual);
                }

                EggDrops.RemoveAt(i);
            }
        }

        public void ResetState()
        {
            for (int i = EggDrops.Count - 1; i >= 0; i--)
            {
                if (EggDrops[i].visual != null)
                {
                    Object.Destroy(EggDrops[i].visual);
                }
            }

            EggDrops.Clear();
        }

        private static void SpawnEggDrops(Vector3 blowFlyPosition)
        {
            var heightOffset = SpawnPositionResolver.ResolveConfiguredOffset(EggDropHeightRange).y;
            var spawnBase = BattleViewBounds.EnsurePlaneZ(blowFlyPosition + new Vector3(0f, heightOffset, 0f));

            var horizontalSpread = SpawnPositionResolver.ResolveConfiguredOffset(HorizontalSpreadRange).x;
            var jitterX = SpawnPositionResolver.ResolveConfiguredOffset(SpawnJitterXRange).x;
            var jitterY = SpawnPositionResolver.ResolveConfiguredOffset(SpawnJitterYRange).y;
            SpawnPositionResolver.TryGetPlayableBounds(out var playableMin, out var playableMax);

            for (int i = 0; i < EggsPerAttack; i++)
            {
                var spreadX = Random.Range(-horizontalSpread, horizontalSpread);
                var randomLandingY = Random.Range(playableMin.y, playableMax.y);
                var landing = SpawnPositionResolver.ClampToPlayableArea(
                    new Vector3(blowFlyPosition.x + spreadX, randomLandingY, 0f));
                var spawnPosition = SpawnPositionResolver.ClampToPlayableArea(
                    spawnBase + new Vector3(Random.Range(-jitterX, jitterX), Random.Range(-jitterY, jitterY), 0f));

                EggDrops.Add(new EggDropState
                {
                    position = spawnPosition,
                    landingPosition = landing,
                    phase = EggPhase.Falling,
                    eggUnitId = -1,
                    visual = CreateEggVisual(spawnPosition)
                });
            }
        }

        private static void HatchFly(EggDropState egg, AttackContext context)
        {
            if (egg.eggUnitId >= 0 && egg.eggUnitId < context.Units.Count)
            {
                var eggUnit = context.Units[egg.eggUnitId];
                eggUnit.alive = false;
                context.Units[egg.eggUnitId] = eggUnit;
            }

            var fly = EnemiesFactor.CreateFly();
            fly.name = $"孵化苍蝇_{Time.frameCount}";
            fly.position = egg.landingPosition;
            UnitManager.SpawnUnit(fly);
        }

        private static GameObject CreateEggVisual(Vector3 spawnPos)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "BlowFlyEgg";
            go.transform.position = spawnPos;
            go.transform.localScale = new Vector3(0.14f, 0.18f, 1f);

            var collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }

            var renderer = go.GetComponent<MeshRenderer>();
            renderer.material = new Material(Shader.Find("Unlit/Color"));
            renderer.material.color = new Color(0.95f, 0.95f, 0.75f, 1f);
            renderer.sortingOrder = 13;

            return go;
        }

        private enum EggPhase
        {
            Falling,
            WaitingToHatch,
            Done
        }

        private struct EggDropState
        {
            public Vector3 position;
            public Vector3 landingPosition;
            public float hatchTimer;
            public EggPhase phase;
            public int eggUnitId;
            public GameObject visual;
        }
    }
}
