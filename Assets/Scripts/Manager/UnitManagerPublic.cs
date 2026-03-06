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
            EvolutionaryMomentSystem.ExitEvolutionaryMoment();
            LevelSystem.Reset();
        }

        public static void StartNewGameWithInitialCompanion(InitialCompanionType companionType)
        {
            PrepareNewGame();

            SpawnUnit(UnitRuntimeData.Player);
            SpawnUnit(CreateInitialCompanion(companionType));
            WaveSystem.GenerateMosquito();

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
                if (unit.faction != Faction.Player) continue;
                if (unit.unitType == "PlayerBase") continue;

                unit.attackInterval += option.attackIntervalDelta;
                unit.attackInterval = unit.attackInterval < 0.1f ? 0.1f : unit.attackInterval;

                unit.attackRange += option.attackRangeDelta;
                unit.attack += option.attackDelta;

                _instance.units[i] = unit;
            }
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

            var message = isVictory
                ? "伙伴们成功保护了便便"
                : "你不再向这个世界散发臭臭了";
            GameEnded?.Invoke(isVictory, message);
        }
    }
}
