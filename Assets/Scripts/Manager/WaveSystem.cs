using System;
using System.Collections.Generic;
using UnityEngine;

namespace Manager
{
    public class WaveSystem
    {
        private static readonly Dictionary<string, LevelSpawnPlan> LevelPlanById = new();

        public static bool StartLevel(string levelId)
        {
            LoadLevelPlansIfNeeded();

            if (string.IsNullOrEmpty(levelId)) return false;

            if (LevelPlanById.TryGetValue(levelId, out var plan))
            {
                EncounterDirector.Initialize(plan);
                return true;
            }

            Debug.LogWarning($"未找到关卡配置: {levelId}");
            return false;
        }

        public static bool AreAllSpawnEventsFinished()
        {
            return EncounterDirector.AreAllSpawnEventsFinished();
        }


        public static LevelSpawnPlan GetLevelPlan(string levelId)
        {
            LoadLevelPlansIfNeeded();
            if (string.IsNullOrEmpty(levelId)) return null;
            return LevelPlanById.TryGetValue(levelId, out var plan) ? plan : null;
        }

        public static List<string> BuildTierPlaylist(int difficulty, int pickCount)
        {
            LoadLevelPlansIfNeeded();

            var pool = new List<string>();
            foreach (var pair in LevelPlanById)
            {
                if (pair.Value.difficulty == difficulty)
                {
                    pool.Add(pair.Key);
                }
            }

            Shuffle(pool);

            if (pool.Count > pickCount)
            {
                pool.RemoveRange(pickCount, pool.Count - pickCount);
            }

            return pool;
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

                plan.difficulty = Mathf.Clamp(plan.difficulty, 1, 8);
                plan.gridColumns = Mathf.Max(1, plan.gridColumns <= 0 ? 15 : plan.gridColumns);
                plan.gridRows = Mathf.Max(1, plan.gridRows <= 0 ? 8 : plan.gridRows);
                LevelPlanById[plan.levelId] = plan;
            }
        }

        private static void Shuffle<T>(IList<T> list)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var swapIndex = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[swapIndex]) = (list[swapIndex], list[i]);
            }
        }
    }
}
