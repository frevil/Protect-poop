using System.Collections.Generic;
using Core;
using UnityEngine;

namespace Manager
{
    public partial class UnitManager : MonoBehaviour
    {
        public List<UnitRuntimeData> units = new();
        private static UnitManager _instance;

        private void Awake()
        {
            _instance = this;
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
