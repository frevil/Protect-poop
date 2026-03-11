using System;
using System.Collections.Generic;
using UnityEngine;

namespace Manager
{
    [Serializable]
    public class LevelSpawnPlan
    {
        public string levelId;
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
