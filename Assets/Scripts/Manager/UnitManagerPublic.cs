using System.Collections.Generic;
using Core;

namespace Manager
{
    public partial class UnitManager
    {
        public static List<UnitRuntimeData> GetUnits()
        {
            return _instance.units;
        }
        
        public static int SpawnUnit(UnitRuntimeData data)
        {
            data.id = _instance.units.Count;
            _instance.units.Add(data);
            return data.id;
        }
    }
}