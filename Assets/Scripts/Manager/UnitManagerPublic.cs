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
    }
}