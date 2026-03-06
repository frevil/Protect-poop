using System.Collections.Generic;
using Core;
using UnityEngine;

namespace Manager
{
    public partial class UnitManager : MonoBehaviour
    {
        public List<UnitRuntimeData> units = new();
        private static UnitManager _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            EnsureInstance();
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

            if (units.Count > 0) return;
            SpawnUnit(UnitRuntimeData.Player);
        }

        void Update()
        {
            float deltaTime = Time.deltaTime;

            TargetingSystem.UpdateTargets(units);
            MovementSystem.MoveUnits(units, deltaTime);
            AttackSystem.HandleAttack(units, deltaTime);
            DeathSystem.HandleDeath(units);
        }
    }
}
