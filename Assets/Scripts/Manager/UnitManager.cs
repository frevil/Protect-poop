using System.Collections.Generic;
using Core;
using UnityEngine;

namespace Manager
{
    public partial class UnitManager : MonoBehaviour
    {
        public List<UnitRuntimeData> units = new();
        private static UnitManager _instance;
        private static bool _isGameRunning;

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
        }

        void Update()
        {
            if (!_isGameRunning) return;

            float deltaTime = Time.deltaTime;

            TargetingSystem.UpdateTargets(units);
            MovementSystem.MoveUnits(units, deltaTime);
            AttackSystem.HandleAttack(units, deltaTime);
            DeathSystem.HandleDeath(units);
        }
    }
}
