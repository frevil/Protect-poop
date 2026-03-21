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

    public readonly struct StageProgressInfo
    {
        public readonly int Tier;
        public readonly int StageInTier;
        public readonly int TotalStageInTier;
        public readonly int Nutrition;

        public StageProgressInfo(int tier, int stageInTier, int totalStageInTier, int nutrition)
        {
            Tier = tier;
            StageInTier = stageInTier;
            TotalStageInTier = totalStageInTier;
            Nutrition = nutrition;
        }
    }

    public readonly struct StageSettlementInfo
    {
        public readonly int Tier;
        public readonly int ClearedStageInTier;
        public readonly int TotalStageInTier;
        public readonly int Nutrition;
        public readonly bool CanPurchaseCompanion;

        public StageSettlementInfo(int tier, int clearedStageInTier, int totalStageInTier, int nutrition, bool canPurchaseCompanion)
        {
            Tier = tier;
            ClearedStageInTier = clearedStageInTier;
            TotalStageInTier = totalStageInTier;
            Nutrition = nutrition;
            CanPurchaseCompanion = canPurchaseCompanion;
        }
    }

    public readonly struct BattlePreparationInfo
    {
        public readonly int GridColumns;
        public readonly int GridRows;

        public BattlePreparationInfo(int gridColumns, int gridRows)
        {
            GridColumns = gridColumns;
            GridRows = gridRows;
        }
    }

    public partial class UnitManager
    {
        private const int MinTier = 1;
        private const int MaxTier = 8;
        private const int StagesPerTier = 3;
        private const int CompanionCost = 3;

        private static readonly Dictionary<int, List<string>> _campaignLevelsByTier = new();

        public static event Action<bool, string> GameEnded;
        public static event Action<int> NutritionChanged;
        public static event Action<StageProgressInfo> StageProgressChanged;
        public static event Action<StageSettlementInfo> StageSettled;
        public static event Action<BattlePreparationInfo> BattlePreparationStarted;
        public static event Action BattlePreparationEnded;

        private static int _nutrition;
        private static int _currentTier;
        private static int _currentStageIndex;
        private static bool _awaitingSettlementChoice;
        private static bool _isBattlePreparing;
        private static LevelSpawnPlan _currentPreparingLevelPlan;

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

        public static bool IsBattlePreparing()
        {
            EnsureInstance();
            return _isBattlePreparing;
        }

        public static int GetNutrition()
        {
            return _nutrition;
        }

        public static void PrepareNewGame()
        {
            EnsureInstance();
            Time.timeScale = 1f;
            _isGameRunning = false;
            _instance.units.Clear();
            AttackSystem.ResetState();
            EvolutionaryMomentSystem.ResetForNewRun();
            EncounterDirector.Reset();
            _elapsedBattleTime = 0f;
            LevelSystem.Reset();

            _nutrition = 0;
            _currentTier = MinTier;
            _currentStageIndex = 0;
            _awaitingSettlementChoice = false;
            _isBattlePreparing = false;
            _currentPreparingLevelPlan = null;
            _campaignLevelsByTier.Clear();
            BattlePreparationEnded?.Invoke();

            NutritionChanged?.Invoke(_nutrition);
        }

        public static void StartNewGameWithInitialCompanion(InitialCompanionType companionType)
        {
            PrepareNewGame();
            BuildCampaign();

            SpawnUnit(UnitRuntimeData.Player);
            SpawnUnit(CreateInitialCompanion(companionType));

            if (!StartCurrentStage())
            {
                Debug.LogWarning("未加载到有效关卡配置，使用默认生成30只蚊子作为兜底。");
                for (var i = 0; i < 30; i++)
                {
                    var mosquito = Enemies.EnemiesFactor.CreateByTypeId("Mosquito");
                    mosquito.name = $"蚊子_{i}";
                    mosquito.position = new Vector3(10f, 5f, 0f) + new Vector3(UnityEngine.Random.Range(0f, 3f), UnityEngine.Random.Range(0f, 3f), 0f);
                    SpawnUnit(mosquito);
                }

                _isGameRunning = true;
                return;
            }

            EnterBattlePreparation();
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
            AttackSystem.ResetState();
            EvolutionaryMomentSystem.ExitEvolutionaryMoment();
            EncounterDirector.Reset();
            _elapsedBattleTime = 0f;
            _awaitingSettlementChoice = false;
            _isBattlePreparing = false;
            _currentPreparingLevelPlan = null;
            BattlePreparationEnded?.Invoke();
        }

        public static void HandleStageCleared()
        {
            if (!_isGameRunning || _awaitingSettlementChoice) return;

            _awaitingSettlementChoice = true;
            _nutrition += 1;
            NutritionChanged?.Invoke(_nutrition);

            var totalInTier = GetCurrentTierLevels().Count;
            StageSettled?.Invoke(new StageSettlementInfo(
                _currentTier,
                _currentStageIndex + 1,
                totalInTier,
                _nutrition,
                _nutrition >= CompanionCost));
        }

        public static void ContinueAfterSettlement()
        {
            if (!_awaitingSettlementChoice) return;

            _awaitingSettlementChoice = false;

            if (!MoveToNextStage())
            {
                EndGame(true);
                return;
            }

            ResetCompanionStateForNewStage();
            EvolutionaryMomentSystem.ResetForNewStage();

            if (!StartCurrentStage())
            {
                EndGame(true);
                return;
            }

            EnterBattlePreparation();
        }

        public static void ConfirmBattlePreparation()
        {
            if (!_isBattlePreparing) return;

            _isBattlePreparing = false;
            _isGameRunning = true;
            BattlePreparationEnded?.Invoke();
        }

        public static bool BuyCompanionDuringSettlement(InitialCompanionType companionType)
        {
            if (!_awaitingSettlementChoice) return false;
            if (_nutrition < CompanionCost) return false;

            _nutrition -= CompanionCost;
            NutritionChanged?.Invoke(_nutrition);
            SpawnUnit(CreatePurchasedCompanion(companionType));
            ContinueAfterSettlement();
            return true;
        }

        public static List<UnitRuntimeData> GetUnits()
        {
            EnsureInstance();
            return _instance.units;
        }

        public static int SpawnUnit(UnitRuntimeData data)
        {
            EnsureInstance();
            data.attackSpeed = data.attackSpeed <= 0f ? 1f : data.attackSpeed;
            data.attackIntervalScale = 1f;
            if (!data.alive)
            {
                data.isTargetable = false;
            }
            else if (!data.isTargetable)
            {
                // 保留配置显式不可被索敌（如苍蝇卵）。
            }
            else
            {
                data.isTargetable = true;
            }

            data.position = SpawnPositionResolver.ClampToPlayableArea(data.position);
            EvolutionaryMomentSystem.OnUnitSpawned(ref data);

            var reusableIndex = FindReusableUnitSlot(data);
            if (reusableIndex >= 0)
            {
                data.id = reusableIndex;
                _instance.units[reusableIndex] = data;
            }
            else
            {
                data.id = _instance.units.Count;
                _instance.units.Add(data);
            }

            if (data.faction == Faction.Player && data.unitType != "PlayerBase")
            {
                EvolutionaryMomentSystem.RegisterCompanion(data.unitType);
            }

            return data.id;
        }

        private static int FindReusableUnitSlot(UnitRuntimeData data)
        {
            // 仅复用阵亡敌人的槽位，避免覆盖玩家阵营与永久单位。
            if (data.faction != Faction.Enemy)
            {
                return -1;
            }

            for (var i = 0; i < _instance.units.Count; i++)
            {
                var candidate = _instance.units[i];
                if (candidate.alive) continue;
                if (candidate.faction != Faction.Enemy) continue;
                return i;
            }

            return -1;
        }

        public static void ApplyEvolutionaryMomentOption(EvolutionaryMomentOption option)
        {
            EnsureInstance();
            EvolutionaryMomentSystem.ApplyOptionToExistingUnits(option, _instance.units);
        }

        private static bool StartCurrentStage()
        {
            _elapsedBattleTime = 0f;
            var levels = GetCurrentTierLevels();
            if (_currentStageIndex < 0 || _currentStageIndex >= levels.Count) return false;

            var levelId = levels[_currentStageIndex];
            var started = WaveSystem.StartLevel(levelId);
            if (!started) return false;

            _currentPreparingLevelPlan = WaveSystem.GetLevelPlan(levelId);
            StageProgressChanged?.Invoke(new StageProgressInfo(_currentTier, _currentStageIndex + 1, levels.Count, _nutrition));
            return true;
        }

        private static void EnterBattlePreparation()
        {
            _isGameRunning = false;
            _isBattlePreparing = true;

            var columns = _currentPreparingLevelPlan?.gridColumns ?? 15;
            var rows = _currentPreparingLevelPlan?.gridRows ?? 8;
            BattlePreparationStarted?.Invoke(new BattlePreparationInfo(Mathf.Max(1, columns), Mathf.Max(1, rows)));
        }

        private static bool MoveToNextStage()
        {
            _currentStageIndex += 1;
            var levels = GetCurrentTierLevels();
            if (_currentStageIndex < levels.Count)
            {
                return true;
            }

            _currentTier += 1;
            _currentStageIndex = 0;
            return _currentTier <= MaxTier;
        }

        private static void BuildCampaign()
        {
            for (var tier = MinTier; tier <= MaxTier; tier++)
            {
                var levels = WaveSystem.BuildTierPlaylist(tier, StagesPerTier);
                if (levels.Count == 0)
                {
                    Debug.LogWarning($"难度{tier}没有找到关卡，运行将提前结束。");
                }

                _campaignLevelsByTier[tier] = levels;
            }
        }

        private static List<string> GetCurrentTierLevels()
        {
            if (_campaignLevelsByTier.TryGetValue(_currentTier, out var levels))
            {
                return levels;
            }

            return new List<string>();
        }

        private static UnitRuntimeData CreatePurchasedCompanion(InitialCompanionType companionType)
        {
            var companion = CreateInitialCompanion(companionType);
            companion.position += new Vector3(UnityEngine.Random.Range(-1.5f, 1.5f), UnityEngine.Random.Range(-1f, 1f), 0f);
            companion.name = $"{companion.name}+";
            return companion;
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
                    attackSpeed = 1f,
                    attackIntervalScale = 1f,
                    attackTimer = 0,
                    projectileCount = 1,
                    moveSpeed = 0f,
                    alive = true,
                    isTargetable = true,
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
                    attackSpeed = 1f,
                    attackIntervalScale = 1f,
                    attackTimer = 0,
                    projectileCount = 1,
                    moveSpeed = 0f,
                    alive = true,
                    isTargetable = true,
                    position = new Vector3(-5f, 3.5f, 0),
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
                    attackSpeed = 1f,
                    attackIntervalScale = 1f,
                    attackTimer = 0,
                    projectileCount = 1,
                    moveSpeed = 3f,
                    alive = true,
                    isTargetable = true,
                    position = new Vector3(-2f, -1f, 0),
                    faction = Faction.Player,
                    targetIndex = -1
                },
                _ => throw new ArgumentOutOfRangeException(nameof(companionType), companionType, null)
            };
        }

        private static void ResetCompanionStateForNewStage()
        {
            for (var i = 0; i < _instance.units.Count; i++)
            {
                var unit = _instance.units[i];
                if (!unit.alive) continue;
                if (unit.faction != Faction.Player || unit.unitType == "PlayerBase") continue;
                if (!TryGetBaseCompanionState(unit.unitType, out var baseCompanion)) continue;

                unit.hp = baseCompanion.hp;
                unit.maxHp = baseCompanion.maxHp;
                // 跨关卡继承进化后的成长：保留攻击、射程、攻速与弹道数量等常驻属性。
                unit.attackIntervalScale = 1f;
                unit.attackTimer = 0f;

                _instance.units[i] = unit;
            }
        }

        private static bool TryGetBaseCompanionState(string unitType, out UnitRuntimeData baseCompanion)
        {
            switch (unitType)
            {
                case "Frog":
                    baseCompanion = CreateInitialCompanion(InitialCompanionType.Frog);
                    return true;
                case "Spider":
                    baseCompanion = CreateInitialCompanion(InitialCompanionType.Spider);
                    return true;
                case "Lizard":
                    baseCompanion = CreateInitialCompanion(InitialCompanionType.Lizard);
                    return true;
                default:
                    baseCompanion = UnitRuntimeData.Empty;
                    return false;
            }
        }

        private static void EndGame(bool isVictory)
        {
            if (!_isGameRunning && !_isBattlePreparing) return;

            _isGameRunning = false;
            _awaitingSettlementChoice = false;
            _isBattlePreparing = false;
            _currentPreparingLevelPlan = null;
            AttackSystem.ResetState();
            EvolutionaryMomentSystem.ExitEvolutionaryMoment();
            EncounterDirector.Reset();
            _elapsedBattleTime = 0f;
            BattlePreparationEnded?.Invoke();

            var message = isVictory
                ? "恭喜通关全部8个难度，伙伴们守住了便便王国！"
                : "你不再向这个世界散发臭臭了";
            GameEnded?.Invoke(isVictory, message);
        }
    }
}
