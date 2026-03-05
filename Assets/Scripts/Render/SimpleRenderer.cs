using System.Collections.Generic;
using Manager;
using UnityEngine;

namespace Render
{
    public class SimpleRenderer : MonoBehaviour
    {
        private readonly Dictionary<int, GameObject> _unitsGameObjects = new();

        private void Update()
        {
            foreach (var unitRuntimeData in UnitManager.GetUnits())
            {
                if (_unitsGameObjects.ContainsKey(unitRuntimeData.id))
                {
                    _unitsGameObjects.TryGetValue(unitRuntimeData.id, out var unitGo);
                    unitGo!.transform.position = unitRuntimeData.position;
                }
                else
                {
                    var go = new GameObject($"{unitRuntimeData.id}");
                    var meshRenderer = go.AddComponent<MeshRenderer>();
                    meshRenderer.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));

                    var meshFilter = go.AddComponent<MeshFilter>();
                    meshFilter.sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");

                    _unitsGameObjects[unitRuntimeData.id] = go;
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying) return;
            foreach (var unitRuntimeData in UnitManager.GetUnits())
            {
                Gizmos.DrawWireSphere(unitRuntimeData.position, unitRuntimeData.attackRange);
            }
        }
    }
}