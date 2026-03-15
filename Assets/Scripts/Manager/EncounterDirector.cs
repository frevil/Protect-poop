using System.Collections.Generic;
using Enemies;
using UnityEngine;

namespace Manager
{
    public static class EncounterDirector
    {
        private class SpawnRuntimeState
        {
            public SpawnEventConfig config;
            public int spawnedCount;
            public float nextSpawnTime;
            public bool started;
        }

        private static readonly List<SpawnRuntimeState> RuntimeSpawns = new();

        public static void Initialize(LevelSpawnPlan levelPlan)
        {
            RuntimeSpawns.Clear();
            SpawnPositionResolver.ConfigureLevelBounds(levelPlan);
            if (levelPlan?.spawns == null) return;

            for (var i = 0; i < levelPlan.spawns.Count; i++)
            {
                var config = levelPlan.spawns[i];
                RuntimeSpawns.Add(new SpawnRuntimeState
                {
                    config = config,
                    spawnedCount = 0,
                    nextSpawnTime = config.time,
                    started = false
                });
            }
        }

        public static void Reset()
        {
            RuntimeSpawns.Clear();
            SpawnPositionResolver.ClearLevelBounds();
        }


        public static bool AreAllSpawnEventsFinished()
        {
            if (RuntimeSpawns.Count == 0) return true;

            for (var i = 0; i < RuntimeSpawns.Count; i++)
            {
                if (RuntimeSpawns[i].spawnedCount < RuntimeSpawns[i].config.count)
                {
                    return false;
                }
            }

            return true;
        }
        public static void Tick(float elapsed)
        {
            for (var i = 0; i < RuntimeSpawns.Count; i++)
            {
                var runtime = RuntimeSpawns[i];
                var config = runtime.config;

                if (runtime.spawnedCount >= config.count) continue;
                if (elapsed < config.time) continue;

                if (!runtime.started)
                {
                    runtime.started = true;
                    runtime.nextSpawnTime = config.time;
                }

                if (config.interval <= 0f)
                {
                    while (runtime.spawnedCount < config.count)
                    {
                        SpawnUnit(config);
                        runtime.spawnedCount++;
                    }

                    continue;
                }

                while (runtime.spawnedCount < config.count && elapsed >= runtime.nextSpawnTime)
                {
                    SpawnUnit(config);
                    runtime.spawnedCount++;
                    runtime.nextSpawnTime += config.interval;
                }
            }
        }

        private static void SpawnUnit(SpawnEventConfig config)
        {
            var newEnemy = EnemiesFactor.CreateByTypeId(config.enemyTypeId);
            if (!newEnemy.alive)
            {
                Debug.LogWarning($"敌人类型不存在: {config.enemyTypeId}");
                return;
            }

            var spawnPosition = SpawnPositionResolver.ResolveSpawnPosition(config.position);
            var offset = SpawnPositionResolver.ResolveSpawnOffset(config.positionRandomRange);

            newEnemy.name = $"{config.enemyTypeId}_{Time.frameCount}";
            newEnemy.position = spawnPosition + offset;
            UnitManager.SpawnUnit(newEnemy);
        }
    }
}
