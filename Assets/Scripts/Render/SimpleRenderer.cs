using System.Collections.Generic;
using Manager;
using UnityEngine;

namespace Render
{
    public class SimpleRenderer : MonoBehaviour
    {
        private readonly Dictionary<int, GameObject> _unitsGameObjects = new();
        private readonly Dictionary<int, string> _renderedUnitTypeById = new();
        private readonly Dictionary<string, UnitVisualConfig> _visualConfigByType = new();
        private readonly Dictionary<string, Texture2D> _textureCacheByPath = new();

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
            if (!UnitManager.IsGameRunning())
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
                }
                else
                {
                    var go = CreateUnitGameObject(unitRuntimeData);
                    _unitsGameObjects[unitRuntimeData.id] = go;
                    _renderedUnitTypeById[unitRuntimeData.id] = unitRuntimeData.unitType;
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

        private void HideAllUnits()
        {
            foreach (var unitGameObject in _unitsGameObjects.Values)
            {
                if (unitGameObject == null) continue;
                unitGameObject.SetActive(false);
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
    }
}
