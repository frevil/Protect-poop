using System.Collections.Generic;
using Core;
using Manager.AttackBehaviors;
using UnityEngine;

namespace Manager
{
    public static class AttackSystem
    {
        private static Transform _effectRoot;

        public static void HandleAttack(List<UnitRuntimeData> units, float dt)
        {
            EnsureEffectRoot();

            var context = new AttackContext(
                units,
                dt,
                _effectRoot,
                CreateProjectileVisual,
                CreateTongueLineRenderer,
                (targetIndex, damage, attackerName) => ApplyDamage(units, targetIndex, damage, attackerName));

            for (int i = 0; i < units.Count; i++)
            {
                var unit = units[i];
                if (!unit.alive) continue;
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
                    unit.attackTimer >= unit.attackInterval)
                {
                    ApplyDamage(units, unit.targetIndex, unit.attack, unit.name);
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

        private static GameObject CreateProjectileVisual(string texturePath, float size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "SpiderWebProjectile";
            go.transform.SetParent(_effectRoot, false);
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

        private static void ApplyDamage(List<UnitRuntimeData> units, int targetIndex, float damage, string attackerName)
        {
            if (targetIndex < 0 || targetIndex >= units.Count) return;

            var target = units[targetIndex];
            if (!target.alive) return;

            target.hp -= damage;
            Debug.Log($"{attackerName}攻击了{target.name},造成了{damage}点伤害");
            units[targetIndex] = target;
        }
    }
}
