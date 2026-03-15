using System.Collections.Generic;
using Core;
using Scripts.Core;
using UnityEngine;

namespace Manager
{
    public partial class UnitManager : MonoBehaviour
    {
        public List<UnitRuntimeData> units = new();
        private static UnitManager _instance;
        private static bool _isGameRunning;
        private static float _elapsedBattleTime;

        private static int _prepGridColumns = 15;
        private static int _prepGridRows = 8;
        private static int _draggingCompanionIndex = -1;

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

        private void OnEnable()
        {
            BattlePreparationStarted += OnBattlePreparationStarted;
            BattlePreparationEnded += OnBattlePreparationEnded;
        }

        private void OnDisable()
        {
            BattlePreparationStarted -= OnBattlePreparationStarted;
            BattlePreparationEnded -= OnBattlePreparationEnded;
        }

        void Update()
        {
            if (_isGameRunning)
            {
                float deltaTime = Time.deltaTime;
                _elapsedBattleTime += deltaTime;

                EncounterDirector.Tick(_elapsedBattleTime);
                TargetingSystem.UpdateTargets(units);
                MovementSystem.MoveUnits(units, deltaTime);
                AttackSystem.HandleAttack(units, deltaTime);
                DeathSystem.HandleDeath(units);
                EvaluateGameState();
                return;
            }

            if (IsBattlePreparing())
            {
                HandleBattlePreparationInput();
            }
        }

        private void HandleBattlePreparationInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                _draggingCompanionIndex = FindNearestDraggableCompanionIndex();
            }

            if (_draggingCompanionIndex >= 0 && Input.GetMouseButton(0) &&
                BattleViewBounds.TryGetMouseWorldPositionOnBattlePlane(out var world))
            {
                var gridSnapped = SnapWorldToGrid(world, _prepGridColumns, _prepGridRows);
                var unit = units[_draggingCompanionIndex];
                unit.position = SpawnPositionResolver.ClampToPlayableArea(gridSnapped);
                units[_draggingCompanionIndex] = unit;
            }

            if (Input.GetMouseButtonUp(0))
            {
                _draggingCompanionIndex = -1;
            }
        }

        private int FindNearestDraggableCompanionIndex()
        {
            if (!BattleViewBounds.TryGetMouseWorldPositionOnBattlePlane(out var world))
            {
                return -1;
            }

            var bestIndex = -1;
            var bestDistance = 1.2f;
            for (var i = 0; i < units.Count; i++)
            {
                var unit = units[i];
                if (!unit.alive || unit.faction != Faction.Player || unit.unitType == "PlayerBase")
                {
                    continue;
                }

                var distance = Vector2.Distance(world, unit.position);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        private static Vector3 SnapWorldToGrid(Vector3 world, int columns, int rows)
        {
            if (!BattleViewBounds.TryGetViewRectOnBattlePlane(out var center, out var halfSize))
            {
                return world;
            }

            var min = center - halfSize;
            var cellWidth = (halfSize.x * 2f) / Mathf.Max(1, columns);
            var cellHeight = (halfSize.y * 2f) / Mathf.Max(1, rows);

            var ix = Mathf.Clamp(Mathf.FloorToInt((world.x - min.x) / Mathf.Max(0.0001f, cellWidth)), 0, columns - 1);
            var iy = Mathf.Clamp(Mathf.FloorToInt((world.y - min.y) / Mathf.Max(0.0001f, cellHeight)), 0, rows - 1);

            return new Vector3(
                min.x + (ix + 0.5f) * cellWidth,
                min.y + (iy + 0.5f) * cellHeight,
                0f);
        }

        private static void OnBattlePreparationStarted(BattlePreparationInfo info)
        {
            _prepGridColumns = Mathf.Max(1, info.GridColumns);
            _prepGridRows = Mathf.Max(1, info.GridRows);
            _draggingCompanionIndex = -1;
        }

        private static void OnBattlePreparationEnded()
        {
            _draggingCompanionIndex = -1;
        }

        private void EvaluateGameState()
        {
            var playerBaseAlive = false;
            var enemyAlive = false;

            for (var i = 0; i < units.Count; i++)
            {
                if (!units[i].alive) continue;

                if (units[i].unitType == "PlayerBase")
                {
                    playerBaseAlive = true;
                }
                else if (units[i].faction == Faction.Enemy)
                {
                    enemyAlive = true;
                }
            }

            if (!playerBaseAlive)
            {
                EndGame(false);
                return;
            }

            if (!enemyAlive)
            {
                if (!WaveSystem.AreAllSpawnEventsFinished())
                {
                    return;
                }

                HandleStageCleared();
            }
        }
    }
}
