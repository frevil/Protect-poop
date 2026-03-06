using System.Collections.Generic;
using Core;
using Manager.Evolution;

namespace Manager
{
    public partial class UnitManager
    {
        internal static void EnsureInstance()
        {
            if (_instance != null) return;

            var managerObj = new UnityEngine.GameObject("UnitManager");
            UnityEngine.Object.DontDestroyOnLoad(managerObj);
            managerObj.AddComponent<UnitManager>();
        }

        public static bool IsGameRunning()
        {
            EnsureInstance();
            return _isGameRunning;
        }

        public static void StartNewGame()
        {
            EnsureInstance();
            _instance.units.Clear();
            SpawnUnit(UnitRuntimeData.Player);
            _isGameRunning = true;
        }

        public static void ShutdownGame()
        {
            EnsureInstance();
            _isGameRunning = false;
            _instance.units.Clear();
        }

        public static List<UnitRuntimeData> GetUnits()
        {
            EnsureInstance();
            return _instance.units;
        }

        public static int SpawnUnit(UnitRuntimeData data)
        {
            EnsureInstance();
            data.id = _instance.units.Count;
            _instance.units.Add(data);
            return data.id;
        }

        public static void ApplyEvolutionaryMomentOption(EvolutionaryMomentOption option)
        {
            EnsureInstance();
            for (var i = 0; i < _instance.units.Count; i++)
            {
                var unit = _instance.units[i];
                if (!unit.alive) continue;
                if (unit.unitType != "Frog") continue;

                unit.attackInterval += option.attackIntervalDelta;
                unit.attackInterval = unit.attackInterval < 0.1f ? 0.1f : unit.attackInterval;

                unit.attackRange += option.attackRangeDelta;
                unit.attack += option.attackDelta;

                _instance.units[i] = unit;
            }
        }
    }
}
