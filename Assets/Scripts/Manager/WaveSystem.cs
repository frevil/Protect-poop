using System.Collections.Generic;
using Enemies;
using UnityEngine;

namespace Manager
{
    public class WaveSystem
    {
        private const string DefaultLevelId = "关卡001";

        public static void GenerateMosquito(int count = 30)
        {
            for (var i = 0; i < count; i++)
            {
                var mosquito = EnemiesFactor.CreateByTypeId("Mosquito");
                mosquito.name = $"蚊子_{i}";
                mosquito.position = new Vector3(10f, 5f, 0f) + new Vector3(Random.Range(0f, 3f), Random.Range(0f, 3f), 0f);
                UnitManager.SpawnUnit(mosquito);
            }
        }
        private static readonly Dictionary<string, LevelSpawnPlan> LevelPlanById = new();

        public static bool StartLevel(string levelId = DefaultLevelId)
        {
            LoadLevelPlansIfNeeded();

            if (LevelPlanById.TryGetValue(levelId, out var plan))
            {
                EncounterDirector.Initialize(plan);
                return true;
            }

            Debug.LogWarning($"未找到关卡配置: {levelId}");
            return false;
        }

        private static void LoadLevelPlansIfNeeded()
        {
            if (LevelPlanById.Count > 0) return;

            var levelFiles = Resources.LoadAll<TextAsset>("Configs/Levels");
            for (var i = 0; i < levelFiles.Length; i++)
            {
                var levelFile = levelFiles[i];
                if (levelFile == null || string.IsNullOrEmpty(levelFile.text)) continue;

                var plan = JsonUtility.FromJson<LevelSpawnPlan>(levelFile.text);
                if (plan == null || string.IsNullOrEmpty(plan.levelId))
                {
                    Debug.LogWarning($"关卡配置解析失败: {levelFile.name}");
                    continue;
                }

                LevelPlanById[plan.levelId] = plan;
            }
        }
    }
}
