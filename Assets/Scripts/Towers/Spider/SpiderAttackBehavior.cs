using System.Collections.Generic;
using Core;
using Manager.Evolution;
using UnityEngine;

namespace Manager.AttackBehaviors
{
    public sealed class SpiderAttackBehavior : IAttackBehavior
    {
        private const float ProjectileSpeed = 18f;
        private const float ProjectileHitRadius = 0.35f;
        private const float WebRadius = 2.2f;
        private const float WebExpandDuration = 0.22f;

        private static readonly List<SpiderWebProjectileState> SpiderProjectiles = new();

        public string UnitType => "Spider";

        public void Handle(ref UnitRuntimeData spider, AttackContext context)
        {
            var target = context.Units[spider.targetIndex];
            if (!target.alive) return;

            var inRange = Vector3.Distance(target.position, spider.position) <= spider.attackRange;
            if (!inRange || spider.attackTimer < EvolutionaryMomentSystem.GetEffectiveAttackInterval(spider)) return;

            SpawnWebVolley(spider, target.position, context);

            spider.attackTimer = 0;
        }

        public void Tick(AttackContext context)
        {
            for (int i = 0; i < SpiderProjectiles.Count; i++)
            {
                var projectile = SpiderProjectiles[i];
                if (projectile.phase == SpiderProjectilePhase.Done)
                {
                    SpiderProjectiles[i] = projectile;
                    continue;
                }

                if (projectile.phase == SpiderProjectilePhase.Flying)
                {
                    var stepDistance = ProjectileSpeed * context.Dt;
                    projectile.position += projectile.direction * stepDistance;
                    projectile.remainingDistance -= stepDistance;

                    if (projectile.visual != null)
                    {
                        projectile.visual.transform.position = projectile.position;
                    }

                    if (ShouldExplodeDuringFlight(context, projectile) || projectile.remainingDistance <= 0f)
                    {
                        ExplodeProjectile(context, ref projectile);
                    }
                }
                else if (projectile.phase == SpiderProjectilePhase.Expanding)
                {
                    projectile.expandTimer += context.Dt;
                    var t = Mathf.Clamp01(projectile.expandTimer / WebExpandDuration);
                    var scale = Mathf.Lerp(0.28f, WebRadius * 2f, t);

                    if (projectile.visual != null)
                    {
                        projectile.visual.transform.localScale = Vector3.one * scale;
                    }

                    if (t >= 1f)
                    {
                        projectile.phase = SpiderProjectilePhase.Done;
                    }
                }

                SpiderProjectiles[i] = projectile;
            }
        }

        public void Cleanup(AttackContext context)
        {
            for (int i = SpiderProjectiles.Count - 1; i >= 0; i--)
            {
                if (SpiderProjectiles[i].phase != SpiderProjectilePhase.Done) continue;

                if (SpiderProjectiles[i].visual != null)
                {
                    Object.Destroy(SpiderProjectiles[i].visual);
                }

                SpiderProjectiles.RemoveAt(i);
            }
        }

        public void ResetState()
        {
            for (int i = SpiderProjectiles.Count - 1; i >= 0; i--)
            {
                if (SpiderProjectiles[i].visual != null)
                {
                    Object.Destroy(SpiderProjectiles[i].visual);
                }
            }

            SpiderProjectiles.Clear();
        }

        private static GameObject CreateProjectileVisual(Transform effectRoot, string texturePath, float size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "SpiderWebProjectile";
            go.transform.SetParent(effectRoot, false);
            go.transform.localScale = Vector3.one * size;

            var collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }

            var meshRenderer = go.GetComponent<MeshRenderer>();
            meshRenderer.material = new Material(Shader.Find("Unlit/Transparent"));
            var texture = Resources.Load<Texture2D>(texturePath);
            if (texture != null)
            {
                meshRenderer.material.mainTexture = texture;
            }

            meshRenderer.sortingOrder = 12;
            return go;
        }

        private static void SpawnWebVolley(UnitRuntimeData spider, Vector3 targetPosition, AttackContext context)
        {
            var burstProjectileCount = EvolutionaryMomentSystem.GetSpiderBurstProjectileCount();
            var projectileCount = burstProjectileCount > 0
                ? burstProjectileCount
                : Mathf.Clamp(spider.projectileCount, 1, 5);
            var spreadStep = 8f;
            var totalSpread = spreadStep * (projectileCount - 1);
            var startAngle = -totalSpread * 0.5f;
            var toTarget = (targetPosition - spider.position);
            if (toTarget.sqrMagnitude < 0.0001f)
            {
                toTarget = Vector3.right;
            }

            for (var i = 0; i < projectileCount; i++)
            {
                var angle = startAngle + spreadStep * i;
                var rotatedDir = Quaternion.Euler(0f, 0f, angle) * toTarget.normalized;
                SpiderProjectiles.Add(new SpiderWebProjectileState
                {
                    attackerFaction = spider.faction,
                    attackerUnitId = spider.id,
                    damage = spider.attack,
                    position = spider.position,
                    direction = rotatedDir,
                    remainingDistance = spider.attackRange,
                    visual = CreateProjectileVisual(context.EffectRoot, "UnitVisuals/net_flying", 0.28f),
                    phase = SpiderProjectilePhase.Flying
                });
            }
        }

        private static bool ShouldExplodeDuringFlight(AttackContext context, SpiderWebProjectileState projectile)
        {
            for (int i = 0; i < context.Units.Count; i++)
            {
                var enemy = context.Units[i];
                if (!enemy.alive || enemy.faction == projectile.attackerFaction) continue;

                if (Vector3.Distance(enemy.position, projectile.position) <= ProjectileHitRadius)
                {
                    return true;
                }
            }

            return false;
        }

        private static void ExplodeProjectile(AttackContext context, ref SpiderWebProjectileState projectile)
        {
            projectile.phase = SpiderProjectilePhase.Expanding;
            projectile.expandTimer = 0;

            if (projectile.visual != null)
            {
                var renderer = projectile.visual.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    var webTex = Resources.Load<Texture2D>("UnitVisuals/net");
                    if (webTex != null)
                    {
                        renderer.material.mainTexture = webTex;
                    }
                }
            }

            for (int i = 0; i < context.Units.Count; i++)
            {
                var enemy = context.Units[i];
                if (!enemy.alive || enemy.faction == projectile.attackerFaction) continue;

                if (Vector3.Distance(enemy.position, projectile.position) <= WebRadius)
                {
                    enemy.hp -= projectile.damage;
                    enemy.lastDamagerUnitId = projectile.attackerUnitId;
                    UnitManager.RecordDamagePopup(enemy.id, projectile.damage);
                    context.Units[i] = enemy;
                    EvolutionaryMomentSystem.ApplySpiderWebHit(context.Units, i, projectile.attackerUnitId);
                }
            }
        }

        private enum SpiderProjectilePhase
        {
            Flying,
            Expanding,
            Done
        }

        private struct SpiderWebProjectileState
        {
            public int attackerFaction;
            public int attackerUnitId;
            public float damage;
            public Vector3 position;
            public Vector3 direction;
            public float remainingDistance;
            public float expandTimer;
            public SpiderProjectilePhase phase;
            public GameObject visual;
        }
    }
}
