using System;
using System.Collections.Generic;
using Core;
using Manager.Evolution;
using Scripts.Core;
using UnityEngine;

namespace Manager
{
    public enum InitialCompanionType
    {
        Frog,
        Spider,
        Lizard
    }

    public partial class UnitManager
    {
        public static event Action<bool, string> GameEnded;

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

        public static void PrepareNewGame()
        {
            EnsureInstance();
            Time.timeScale = 1f;
            _isGameRunning = false;
            _instance.units.Clear();
            EvolutionaryMomentSystem.ResetForNewRun();
            EncounterDirector.Reset();
            _elapsedBattleTime = 0f;
            LevelSystem.Reset();
        }

        public static void StartNewGameWithInitialCompanion(InitialCompanionType companionType)
        {
            PrepareNewGame();

            SpawnUnit(UnitRuntimeData.Player);
            SpawnUnit(CreateInitialCompanion(companionType));
            var hasConfiguredLevel = WaveSystem.StartLevel();
            if (!hasConfiguredLevel)
            {
                Debug.LogWarning("未加载到关卡配置，使用默认生成30只蚊子作为兜底。");
                for (var i = 0; i < 30; i++)
                {
                    var mosquito = Enemies.EnemiesFactor.CreateByTypeId("Mosquito");
                    mosquito.name = $"蚊子_{i}";
                    mosquito.position = new Vector3(10f, 5f, 0f) + new Vector3(UnityEngine.Random.Range(0f, 3f), UnityEngine.Random.Range(0f, 3f), 0f);
                    SpawnUnit(mosquito);
                }
            }

            _isGameRunning = true;
        }

        public static void StartNewGame()
        {
            StartNewGameWithInitialCompanion(InitialCompanionType.Frog);
        }

        public static void ShutdownGame()
        {
            EnsureInstance();
            Time.timeScale = 1f;
            _isGameRunning = false;
            _instance.units.Clear();
            EvolutionaryMomentSystem.ExitEvolutionaryMoment();
            EncounterDirector.Reset();
            _elapsedBattleTime = 0f;
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
            data.attackIntervalScale = 1f;
            EvolutionaryMomentSystem.OnUnitSpawned(ref data);
            _instance.units.Add(data);

            if (data.faction == Faction.Player && data.unitType != "PlayerBase")
            {
                EvolutionaryMomentSystem.RegisterCompanion(data.unitType);
            }

            return data.id;
        }

        public static void ApplyEvolutionaryMomentOption(EvolutionaryMomentOption option)
        {
            EnsureInstance();
            EvolutionaryMomentSystem.ApplyOptionToExistingUnits(option, _instance.units);
        }

        private static UnitRuntimeData CreateInitialCompanion(InitialCompanionType companionType)
        {
            return companionType switch
            {
                InitialCompanionType.Frog => new UnitRuntimeData
                {
                    name = "青蛙",
                    unitType = "Frog",
                    hp = 100,
                    maxHp = 100,
                    attack = 10,
                    attackRange = 5f,
                    attackInterval = 1f,
                    attackIntervalScale = 1f,
                    attackTimer = 0,
                    moveSpeed = 0f,
                    alive = true,
                    position = new Vector3(-6f, -3.5f, 0),
                    faction = Faction.Player,
                    targetIndex = -1
                },
                InitialCompanionType.Spider => new UnitRuntimeData
                {
                    name = "东方明蛛",
                    unitType = "Spider",
                    hp = 50,
                    maxHp = 50,
                    attack = 5,
                    attackRange = 25f,
                    attackInterval = 3f,
                    attackIntervalScale = 1f,
                    attackTimer = 0,
                    moveSpeed = 0f,
                    alive = true,
                    position = new Vector3(-5f, 7f, 0),
                    faction = Faction.Player,
                    targetIndex = -1
                },
                InitialCompanionType.Lizard => new UnitRuntimeData
                {
                    name = "独立游蜴",
                    unitType = "Lizard",
                    hp = 85,
                    maxHp = 85,
                    attack = 15,
                    attackRange = 3f,
                    attackInterval = 2f,
                    attackIntervalScale = 1f,
                    attackTimer = 0,
                    moveSpeed = 3f,
                    alive = true,
                    position = new Vector3(-2f, -1f, 0),
                    faction = Faction.Player,
                    targetIndex = -1
                },
                _ => throw new ArgumentOutOfRangeException(nameof(companionType), companionType, null)
            };
        }

        private static void EndGame(bool isVictory)
        {
            if (!_isGameRunning) return;

            _isGameRunning = false;
            EvolutionaryMomentSystem.ExitEvolutionaryMoment();
            EncounterDirector.Reset();
            _elapsedBattleTime = 0f;

            var message = isVictory
                ? "伙伴们成功保护了便便"
                : "你不再向这个世界散发臭臭了";
            GameEnded?.Invoke(isVictory, message);
        }
    }
}
