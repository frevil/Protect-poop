using System.Collections.Generic;
using Manager;
using Manager.Evolution;
using UnityEngine;

namespace Render
{
    public class SimpleRenderer : MonoBehaviour
    {
        private readonly Dictionary<int, GameObject> _unitsGameObjects = new();
        private readonly Dictionary<int, string> _renderedUnitTypeById = new();
        private readonly Dictionary<string, UnitVisualConfig> _visualConfigByType = new();
        private readonly Dictionary<string, Texture2D> _textureCacheByPath = new();
        private readonly Dictionary<int, Dictionary<string, SkillIconView>> _skillIconsByUnitId = new();
        private readonly List<EvolutionSkillRuntime> _skillRuntimeBuffer = new();

        private static readonly Dictionary<string, string> SkillIconPathBySkillId = new()
        {
            // 先占位，后续可直接在 Resources 下补齐对应贴图资源。
            { "frog_frenzy_1", "SkillIcons/frog_frenzy_1" }
        };

        private Texture2D _placeholderSkillIcon;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            var existing = FindObjectOfType<SimpleRenderer>();
            if (existing != null) return;

            var rendererObject = new GameObject("SimpleRenderer");
            DontDestroyOnLoad(rendererObject);
            rendererObject.AddComponent<SimpleRenderer>();
        }

        private void Awake()
        {
            LoadVisualConfigs();
        }

        private void Update()
        {
            if (!UnitManager.IsGameRunning() && !UnitManager.IsBattlePreparing())
            {
                HideAllUnits();
                return;
            }

            foreach (var unitRuntimeData in UnitManager.GetUnits())
            {
                if (_unitsGameObjects.ContainsKey(unitRuntimeData.id))
                {
                    _unitsGameObjects.TryGetValue(unitRuntimeData.id, out var unitGo);
                    if (unitGo == null)
                    {
                        unitGo = CreateUnitGameObject(unitRuntimeData);
                        _unitsGameObjects[unitRuntimeData.id] = unitGo;
                        _renderedUnitTypeById[unitRuntimeData.id] = unitRuntimeData.unitType;
                    }

                    if (_renderedUnitTypeById.TryGetValue(unitRuntimeData.id, out var renderedUnitType) &&
                        renderedUnitType != unitRuntimeData.unitType)
                    {
                        Destroy(unitGo);
                        unitGo = CreateUnitGameObject(unitRuntimeData);
                        _unitsGameObjects[unitRuntimeData.id] = unitGo;
                        _renderedUnitTypeById[unitRuntimeData.id] = unitRuntimeData.unitType;
                    }

                    UpdatePosition(unitGo, unitRuntimeData);
                    UpdateSkillIcons(unitGo, unitRuntimeData);
                }
                else
                {
                    var go = CreateUnitGameObject(unitRuntimeData);
                    _unitsGameObjects[unitRuntimeData.id] = go;
                    _renderedUnitTypeById[unitRuntimeData.id] = unitRuntimeData.unitType;
                    UpdateSkillIcons(go, unitRuntimeData);
                }

                _unitsGameObjects[unitRuntimeData.id].SetActive(unitRuntimeData.alive);
            }
        }

        private void LoadVisualConfigs()
        {
            _visualConfigByType.Clear();

            var configText = Resources.Load<TextAsset>("Configs/UnitVisualConfigs");
            if (configText == null)
            {
                Debug.LogWarning("未找到 Units 形象配置文件: Resources/Configs/UnitVisualConfigs.json");
                return;
            }

            var configList = JsonUtility.FromJson<UnitVisualConfigList>(configText.text);
            if (configList?.visuals == null) return;

            foreach (var visual in configList.visuals)
            {
                if (string.IsNullOrEmpty(visual.unitType)) continue;
                _visualConfigByType[visual.unitType] = visual;
            }
        }

        private GameObject CreateUnitGameObject(Core.UnitRuntimeData unitRuntimeData)
        {
            var go = new GameObject($"{unitRuntimeData.id}_{unitRuntimeData.unitType}");
            go.transform.SetParent(transform, false);

            var meshFilter = go.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");

            var meshRenderer = go.AddComponent<MeshRenderer>();
            var material = new Material(Shader.Find("Unlit/Transparent"));

            if (_visualConfigByType.TryGetValue(unitRuntimeData.unitType, out var visualConfig))
            {
                var texture = GetTexture(visualConfig.textureResourcePath);
                if (texture != null)
                {
                    material.mainTexture = texture;
                }
                else
                {
                    Debug.LogWarning($"Unit[{unitRuntimeData.unitType}] 贴图加载失败: {visualConfig.textureResourcePath}");
                    material.color = Color.white;
                }

                go.transform.localScale = Vector3.one * (visualConfig.scale <= 0f ? 1f : visualConfig.scale);
                meshRenderer.sortingOrder = visualConfig.sortingOrder;
            }
            else
            {
                material.color = unitRuntimeData.faction == Scripts.Core.Faction.Player ? Color.green : Color.red;
                go.transform.localScale = Vector3.one;
            }

            meshRenderer.material = material;
            UpdatePosition(go, unitRuntimeData);
            return go;
        }

        private Texture2D GetTexture(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath)) return null;

            if (_textureCacheByPath.TryGetValue(resourcePath, out var cachedTexture))
            {
                return cachedTexture;
            }

            var texture = Resources.Load<Texture2D>(resourcePath);
            _textureCacheByPath[resourcePath] = texture;
            return texture;
        }

        private void UpdatePosition(GameObject go, Core.UnitRuntimeData unitRuntimeData)
        {
            var targetPosition = unitRuntimeData.position;
            if (_visualConfigByType.TryGetValue(unitRuntimeData.unitType, out var config))
            {
                targetPosition.z += config.zOffset;
            }

            go.transform.position = targetPosition;
        }

        private void UpdateSkillIcons(GameObject unitGo, Core.UnitRuntimeData unitRuntimeData)
        {
            if (unitRuntimeData.faction != Scripts.Core.Faction.Player || unitRuntimeData.unitType == "PlayerBase")
            {
                HideSkillIcons(unitRuntimeData.id);
                return;
            }

            EvolutionaryMomentSystem.GetSkillRuntimesForOwner(unitRuntimeData.id, _skillRuntimeBuffer);
            if (!_skillIconsByUnitId.TryGetValue(unitRuntimeData.id, out var viewsBySkillId))
            {
                viewsBySkillId = new Dictionary<string, SkillIconView>();
                _skillIconsByUnitId[unitRuntimeData.id] = viewsBySkillId;
            }

            var activeSkillIds = new HashSet<string>();
            for (var i = 0; i < _skillRuntimeBuffer.Count; i++)
            {
                var runtime = _skillRuntimeBuffer[i];
                if (string.IsNullOrEmpty(runtime.skillId)) continue;
                activeSkillIds.Add(runtime.skillId);

                if (!viewsBySkillId.TryGetValue(runtime.skillId, out var iconView) || iconView == null || iconView.root == null)
                {
                    iconView = CreateSkillIconView(unitGo.transform, runtime.skillId);
                    viewsBySkillId[runtime.skillId] = iconView;
                }

                iconView.root.SetActive(unitRuntimeData.alive);
                UpdateSkillIconState(iconView, runtime);
            }

            foreach (var pair in viewsBySkillId)
            {
                if (pair.Value?.root == null) continue;
                pair.Value.root.SetActive(activeSkillIds.Contains(pair.Key) && unitRuntimeData.alive);
            }

            LayoutSkillIcons(viewsBySkillId);
        }

        private void HideSkillIcons(int unitId)
        {
            if (!_skillIconsByUnitId.TryGetValue(unitId, out var viewsBySkillId)) return;
            foreach (var pair in viewsBySkillId)
            {
                if (pair.Value?.root == null) continue;
                pair.Value.root.SetActive(false);
            }
        }

        private SkillIconView CreateSkillIconView(Transform parent, string skillId)
        {
            var root = new GameObject($"SkillIcon_{skillId}");
            root.transform.SetParent(parent, false);

            var icon = GameObject.CreatePrimitive(PrimitiveType.Quad);
            icon.name = "Icon";
            icon.transform.SetParent(root.transform, false);
            icon.transform.localScale = Vector3.one * 0.45f;
            icon.transform.localPosition = Vector3.zero;
            var iconRenderer = icon.GetComponent<MeshRenderer>();
            var iconMaterial = new Material(Shader.Find("Unlit/Transparent"));
            iconMaterial.mainTexture = GetSkillIconTexture(skillId);
            iconMaterial.color = Color.white;
            iconRenderer.material = iconMaterial;
            iconRenderer.sortingOrder = 450;
            var iconCollider = icon.GetComponent<Collider>();
            if (iconCollider != null) Destroy(iconCollider);

            var ring = new GameObject("Ring");
            ring.transform.SetParent(root.transform, false);
            var lineRenderer = ring.AddComponent<LineRenderer>();
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.loop = false;
            lineRenderer.useWorldSpace = false;
            lineRenderer.widthMultiplier = 0.06f;
            lineRenderer.positionCount = 0;
            lineRenderer.numCapVertices = 2;
            lineRenderer.sortingOrder = 451;
            lineRenderer.startColor = new Color(0.3f, 0.9f, 1f, 0.95f);
            lineRenderer.endColor = lineRenderer.startColor;

            return new SkillIconView
            {
                root = root,
                iconRenderer = iconRenderer,
                ringRenderer = lineRenderer
            };
        }

        private static void UpdateSkillIconState(SkillIconView iconView, EvolutionSkillRuntime runtime)
        {
            if (iconView.iconRenderer != null)
            {
                iconView.iconRenderer.material.color = runtime.isActive ? new Color(1f, 0.35f, 0.35f, 1f) : Color.white;
            }

            if (iconView.ringRenderer == null) return;
            var progress = 1f;
            if (runtime.isActive)
            {
                if (runtime.duration > 0.01f)
                {
                    progress = 1f - Mathf.Clamp01(runtime.durationTimer / runtime.duration);
                }
                else
                {
                    progress = 0f;
                }
            }
            else if (runtime.cooldown > 0.01f)
            {
                progress = Mathf.Clamp01(runtime.cooldownTimer / runtime.cooldown);
            }

            DrawProgressRing(iconView.ringRenderer, progress);
        }

        private static void DrawProgressRing(LineRenderer lineRenderer, float progress)
        {
            var clamped = Mathf.Clamp01(progress);
            if (clamped <= 0.001f)
            {
                lineRenderer.positionCount = 0;
                return;
            }

            const int maxSegments = 48;
            const float radius = 0.31f;
            var segments = Mathf.Max(2, Mathf.CeilToInt(maxSegments * clamped));
            lineRenderer.positionCount = segments + 1;

            for (var i = 0; i <= segments; i++)
            {
                var t = (float)i / segments * clamped;
                var angle = (Mathf.PI * 2f * t) + (Mathf.PI / 2f);
                var pos = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, -0.02f);
                lineRenderer.SetPosition(i, pos);
            }
        }

        private static void LayoutSkillIcons(Dictionary<string, SkillIconView> viewsBySkillId)
        {
            var activeViews = new List<SkillIconView>();
            foreach (var pair in viewsBySkillId)
            {
                if (pair.Value?.root == null) continue;
                if (!pair.Value.root.activeSelf) continue;
                activeViews.Add(pair.Value);
            }

            if (activeViews.Count == 0) return;

            const float iconSpacing = 0.58f;
            const float startY = 1f;
            var totalWidth = (activeViews.Count - 1) * iconSpacing;
            var startX = -totalWidth * 0.5f;
            for (var i = 0; i < activeViews.Count; i++)
            {
                activeViews[i].root.transform.localPosition = new Vector3(startX + i * iconSpacing, startY, 0f);
            }
        }

        private Texture2D GetSkillIconTexture(string skillId)
        {
            if (string.IsNullOrEmpty(skillId))
            {
                return GetPlaceholderSkillIconTexture();
            }

            if (!SkillIconPathBySkillId.TryGetValue(skillId, out var path) || string.IsNullOrEmpty(path))
            {
                return GetPlaceholderSkillIconTexture();
            }

            var texture = GetTexture(path);
            return texture != null ? texture : GetPlaceholderSkillIconTexture();
        }

        private Texture2D GetPlaceholderSkillIconTexture()
        {
            if (_placeholderSkillIcon != null) return _placeholderSkillIcon;

            _placeholderSkillIcon = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            _placeholderSkillIcon.SetPixels(new[]
            {
                new Color(0.95f, 0.2f, 0.8f),
                new Color(0.2f, 0.2f, 0.2f),
                new Color(0.2f, 0.2f, 0.2f),
                new Color(0.95f, 0.2f, 0.8f)
            });
            _placeholderSkillIcon.filterMode = FilterMode.Point;
            _placeholderSkillIcon.wrapMode = TextureWrapMode.Clamp;
            _placeholderSkillIcon.Apply();
            return _placeholderSkillIcon;
        }

        private void HideAllUnits()
        {
            foreach (var unitGameObject in _unitsGameObjects.Values)
            {
                if (unitGameObject == null) continue;
                unitGameObject.SetActive(false);
            }

            foreach (var viewsBySkillId in _skillIconsByUnitId.Values)
            {
                foreach (var pair in viewsBySkillId)
                {
                    if (pair.Value?.root == null) continue;
                    pair.Value.root.SetActive(false);
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;
            foreach (var unitRuntimeData in UnitManager.GetUnits())
            {
                Gizmos.color = unitRuntimeData.alive ? Color.red : Color.gray;
                Gizmos.DrawWireSphere(unitRuntimeData.position, unitRuntimeData.attackRange);
            }
        }
        private class SkillIconView
        {
            public GameObject root;
            public MeshRenderer iconRenderer;
            public LineRenderer ringRenderer;
        }
    }
}
