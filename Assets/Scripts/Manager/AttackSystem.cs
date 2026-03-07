using System.Collections.Generic;
using Core;
using UnityEngine;

namespace Manager
{
    public static class AttackSystem
    {
        private const float FrogTongueSpeed = 16f;
        private const float FrogTongueHitDistance = 0.25f;
        private const float SpiderProjectileSpeed = 18f;
        private const float SpiderWebRadius = 2.2f;
        private const float SpiderWebExpandDuration = 0.22f;

        private static readonly Dictionary<int, FrogTongueState> FrogTongueStates = new();
        private static readonly List<SpiderWebProjectileState> SpiderProjectiles = new();
        private static Transform _effectRoot;

        public static void HandleAttack(List<UnitRuntimeData> units, float dt)
        {
            EnsureEffectRoot();

            for (int i = 0; i < units.Count; i++)
            {
                var unit = units[i];
                if (!unit.alive) continue;
                if (unit.targetIndex < 0 || unit.targetIndex >= units.Count) continue;

                unit.attackTimer += dt;

                if (unit.unitType == "Frog")
                {
                    HandleFrogAttack(units, ref unit, dt);
                    units[i] = unit;
                    continue;
                }

                if (unit.unitType == "Spider")
                {
                    HandleSpiderAttack(units, ref unit);
                    units[i] = unit;
                    continue;
                }

                var target = units[unit.targetIndex];
                if (Vector3.Distance(target.position, unit.position) < unit.attackRange &&
                    unit.attackTimer >= unit.attackInterval)
                {
                    ApplyDamage(units, unit.targetIndex, unit.attack, unit.name);
                    unit.attackTimer = 0;
                }

                units[i] = unit;
            }

            UpdateSpiderProjectiles(units, dt);
            CleanupFrogStates(units);
            CleanupSpiderProjectiles();
        }

        private static void HandleFrogAttack(List<UnitRuntimeData> units, ref UnitRuntimeData frog, float dt)
        {
            var hasState = FrogTongueStates.TryGetValue(frog.id, out var state);
            if (!hasState)
            {
                var target = units[frog.targetIndex];
                var inRange = Vector3.Distance(target.position, frog.position) <= frog.attackRange;
                if (frog.attackTimer < frog.attackInterval || !inRange || !target.alive) return;

                state = new FrogTongueState
                {
                    frogId = frog.id,
                    targetIndex = frog.targetIndex,
                    tongueTip = frog.position,
                    phase = FrogTonguePhase.Extending,
                    lineRenderer = CreateTongueLineRenderer(frog.id)
                };

                FrogTongueStates[frog.id] = state;
                frog.attackTimer = 0;
            }

            if (state.targetIndex < 0 || state.targetIndex >= units.Count)
            {
                FinishFrogTongue(frog.id);
                return;
            }

            var targetUnit = units[state.targetIndex];
            var tipDestination = state.phase == FrogTonguePhase.Extending && targetUnit.alive
                ? targetUnit.position
                : frog.position;

            state.tongueTip = Vector3.MoveTowards(state.tongueTip, tipDestination, FrogTongueSpeed * dt);

            if (state.phase == FrogTonguePhase.Extending && targetUnit.alive &&
                Vector3.Distance(state.tongueTip, targetUnit.position) <= FrogTongueHitDistance)
            {
                state.phase = FrogTonguePhase.Retracting;
                state.hasCapturedTarget = true;
            }

            if (state.hasCapturedTarget && targetUnit.alive)
            {
                targetUnit.position = state.tongueTip;
                units[state.targetIndex] = targetUnit;
            }

            UpdateTongueLineRenderer(state, frog.position);

            if (state.phase == FrogTonguePhase.Retracting && Vector3.Distance(state.tongueTip, frog.position) <= FrogTongueHitDistance)
            {
                if (state.hasCapturedTarget)
                {
                    ApplyDamage(units, state.targetIndex, frog.attack, frog.name);
                }

                FinishFrogTongue(frog.id);
                return;
            }

            FrogTongueStates[frog.id] = state;
        }

        private static void HandleSpiderAttack(List<UnitRuntimeData> units, ref UnitRuntimeData spider)
        {
            var target = units[spider.targetIndex];
            if (!target.alive) return;

            var inRange = Vector3.Distance(target.position, spider.position) <= spider.attackRange;
            if (!inRange || spider.attackTimer < spider.attackInterval) return;

            SpiderProjectiles.Add(new SpiderWebProjectileState
            {
                targetIndex = spider.targetIndex,
                attackerFaction = spider.faction,
                damage = spider.attack,
                position = spider.position,
                targetLastPosition = target.position,
                visual = CreateProjectileVisual("Art/Images/net_flying", 0.28f),
                phase = SpiderProjectilePhase.Flying
            });

            spider.attackTimer = 0;
        }

        private static void UpdateSpiderProjectiles(List<UnitRuntimeData> units, float dt)
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
                    if (projectile.targetIndex >= 0 && projectile.targetIndex < units.Count && units[projectile.targetIndex].alive)
                    {
                        projectile.targetLastPosition = units[projectile.targetIndex].position;
                    }

                    projectile.position = Vector3.MoveTowards(
                        projectile.position,
                        projectile.targetLastPosition,
                        SpiderProjectileSpeed * dt);

                    if (projectile.visual != null)
                    {
                        projectile.visual.transform.position = projectile.position;
                    }

                    if (Vector3.Distance(projectile.position, projectile.targetLastPosition) <= 0.1f)
                    {
                        ExplodeSpiderProjectile(units, ref projectile);
                    }
                }
                else if (projectile.phase == SpiderProjectilePhase.Expanding)
                {
                    projectile.expandTimer += dt;
                    var t = Mathf.Clamp01(projectile.expandTimer / SpiderWebExpandDuration);
                    var scale = Mathf.Lerp(0.28f, SpiderWebRadius * 2f, t);

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

        private static void ExplodeSpiderProjectile(List<UnitRuntimeData> units, ref SpiderWebProjectileState projectile)
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

            for (int i = 0; i < units.Count; i++)
            {
                var enemy = units[i];
                if (!enemy.alive || enemy.faction == projectile.attackerFaction) continue;

                if (Vector3.Distance(enemy.position, projectile.position) <= SpiderWebRadius)
                {
                    enemy.hp -= projectile.damage;
                    units[i] = enemy;
                }
            }
        }

        private static void EnsureEffectRoot()
        {
            if (_effectRoot != null) return;

            var go = GameObject.Find("AttackEffects");
            if (go == null)
            {
                go = new GameObject("AttackEffects");
                UnityEngine.Object.DontDestroyOnLoad(go);
            }

            _effectRoot = go.transform;
        }

        private static LineRenderer CreateTongueLineRenderer(int frogId)
        {
            var go = new GameObject($"FrogTongue_{frogId}");
            go.transform.SetParent(_effectRoot, false);

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

        private static void UpdateTongueLineRenderer(FrogTongueState state, Vector3 frogPosition)
        {
            if (state.lineRenderer == null) return;

            state.lineRenderer.SetPosition(0, frogPosition);
            state.lineRenderer.SetPosition(1, state.tongueTip);
        }

        private static GameObject CreateProjectileVisual(string texturePath, float size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "SpiderWebProjectile";
            go.transform.SetParent(_effectRoot, false);
            go.transform.localScale = Vector3.one * size;

            var collider = go.GetComponent<Collider>();
            if (collider != null)
            {
                UnityEngine.Object.Destroy(collider);
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

        private static void FinishFrogTongue(int frogId)
        {
            if (!FrogTongueStates.TryGetValue(frogId, out var state)) return;

            if (state.lineRenderer != null)
            {
                UnityEngine.Object.Destroy(state.lineRenderer.gameObject);
            }

            FrogTongueStates.Remove(frogId);
        }

        private static void CleanupFrogStates(List<UnitRuntimeData> units)
        {
            var deadFrogIds = new List<int>();
            foreach (var item in FrogTongueStates)
            {
                var frogAlive = false;
                for (int i = 0; i < units.Count; i++)
                {
                    if (units[i].id != item.Key) continue;
                    frogAlive = units[i].alive;
                    break;
                }

                if (!frogAlive)
                {
                    deadFrogIds.Add(item.Key);
                }
            }

            foreach (var deadFrogId in deadFrogIds)
            {
                FinishFrogTongue(deadFrogId);
            }
        }

        private static void CleanupSpiderProjectiles()
        {
            for (int i = SpiderProjectiles.Count - 1; i >= 0; i--)
            {
                if (SpiderProjectiles[i].phase != SpiderProjectilePhase.Done) continue;

                if (SpiderProjectiles[i].visual != null)
                {
                    UnityEngine.Object.Destroy(SpiderProjectiles[i].visual);
                }

                SpiderProjectiles.RemoveAt(i);
            }
        }

        private static void ApplyDamage(List<UnitRuntimeData> units, int targetIndex, float damage, string attackerName)
        {
            if (targetIndex < 0 || targetIndex >= units.Count) return;

            var target = units[targetIndex];
            if (!target.alive) return;

            target.hp -= damage;
            Debug.Log($"{attackerName}攻击了{target.name},造成了{damage}点伤害");
            units[targetIndex] = target;
        }

        private enum FrogTonguePhase
        {
            Extending,
            Retracting
        }

        private enum SpiderProjectilePhase
        {
            Flying,
            Expanding,
            Done
        }

        private struct FrogTongueState
        {
            public int frogId;
            public int targetIndex;
            public bool hasCapturedTarget;
            public FrogTonguePhase phase;
            public Vector3 tongueTip;
            public LineRenderer lineRenderer;
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
