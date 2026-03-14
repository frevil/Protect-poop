using System;
using System.Collections.Generic;
using UnityEngine;

namespace Manager
{
    [Serializable]
    public class LevelSpawnPlan
    {
        public string levelId;
        public int difficulty = 1;
        public bool useSpawnBounds;
        public Vector2 spawnBoundsMin;
        public Vector2 spawnBoundsMax;
        public List<SpawnEventConfig> spawns = new();
    }

    [Serializable]
    public class SpawnEventConfig
    {
        public float time;
        public Vector3 position;
        public Vector3 positionRandomRange;
        public string enemyTypeId;
        public int count = 1;
        public float interval;
    }
}
