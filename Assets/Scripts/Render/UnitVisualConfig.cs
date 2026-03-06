using System;
using System.Collections.Generic;
using UnityEngine;

namespace Render
{
    [Serializable]
    public class UnitVisualConfigList
    {
        public List<UnitVisualConfig> visuals = new();
    }

    [Serializable]
    public class UnitVisualConfig
    {
        public string unitType;
        public string textureResourcePath;
        public float scale = 1f;
        public float zOffset;
        public int sortingOrder;
    }
}
