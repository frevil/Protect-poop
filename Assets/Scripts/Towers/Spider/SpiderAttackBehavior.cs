using System.Collections.Generic;
using Core;
using Manager.Evolution;
using UnityEngine;

namespace Manager.AttackBehaviors
{
    public sealed class SpiderAttackBehavior : IAttackBehavior
    {
        private const float ProjectileSpeed = 18f;
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

            SpiderProjectiles.Add(new SpiderWebProjectileState
            {
                targetIndex = spider.targetIndex,
                attackerFaction = spider.faction,
                damage = spider.attack,
                position = spider.position,
                targetLastPosition = target.position,
                visual = CreateProjectileVisual(context.EffectRoot, "Art/Images/net_flying", 0.28f),
                phase = SpiderProjectilePhase.Flying
            });

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
                    if (context.IsValidTargetIndex(projectile.targetIndex) && context.Units[projectile.targetIndex].alive)
                    {
                        projectile.targetLastPosition = context.Units[projectile.targetIndex].position;
                    }

                    projectile.position = Vector3.MoveTowards(
                        projectile.position,
                        projectile.targetLastPosition,
                        ProjectileSpeed * context.Dt);

                    if (projectile.visual != null)
                    {
                        projectile.visual.transform.position = projectile.position;
                    }

                    if (Vector3.Distance(projectile.position, projectile.targetLastPosition) <= 0.1f)
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

        private static void ExplodeProjectile(AttackContext context, ref SpiderWebProjectileState projectile)
        {
            projectile.phase = SpiderProjectilePhase.Expanding;
            projectile.expandTimer = 0;

            if (projectile.visual != null)
            {
                var renderer = projectile.visual.GetComponent<MeshRenderer>();
                if (renderer != null)
                {
                    var webTex = Resources.Load<Texture2D>("Art/Images/net");
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
                    context.Units[i] = enemy;
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
            public int targetIndex;
            public int attackerFaction;
            public float damage;
            public Vector3 position;
            public Vector3 targetLastPosition;
            public float expandTimer;
            public SpiderProjectilePhase phase;
            public GameObject visual;
        }
    }
}
