using System;
using System.Collections.Generic;
using Core;
using Manager.Evolution.Skills;
using Scripts.Core;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Manager.Evolution
{
    public static class EvolutionaryMomentSystem
    {
        private const string ConfigPath = "Configs/EvolutionaryMomentOptions";
        private const string OptionTypeSkill = "skill";
        private const string TargetSpecificUnitType = "specific_unit_type";
        private const string TargetAllCompanions = "all_companions";
        private const string TargetAllEnemies = "all_enemies";

        private static readonly List<EvolutionaryMomentOption> AllOptions = new();
        private static readonly Dictionary<string, EvolutionaryMomentOption> OptionById = new();
        private static readonly List<EvolutionaryMomentOption> CurrentOptions = new();
        private static readonly HashSet<string> OptionPool = new();
        private static readonly HashSet<string> SelectedOptionIds = new();
        private static readonly HashSet<string> OwnedCompanionUnitTypes = new();
        private static readonly List<EvolutionSkillRuntime> SkillRuntimes = new();

        private static bool _loaded;
        public static bool IsInEvolutionaryMoment { get; private set; }

        public static event Action<IReadOnlyList<EvolutionaryMomentOption>> EvolutionaryMomentStarted;
        public static event Action EvolutionaryMomentEnded;

        public static void ResetForNewRun()
        {
            EnsureLoaded();

            CurrentOptions.Clear();
            SelectedOptionIds.Clear();
            OwnedCompanionUnitTypes.Clear();
            SkillRuntimes.Clear();
            RebuildOptionPool();

            ExitEvolutionaryMoment();
        }

        public static void ResetForNewStage()
        {
            EnsureLoaded();

            CurrentOptions.Clear();
            // 跨关卡继承进化收益：仅重置当前面板状态，不清空已获得的天赋/技能与解锁池。

            ExitEvolutionaryMoment();
        }

        public static IReadOnlyList<EvolutionaryMomentOption> GetCurrentOptions()
        {
            return CurrentOptions;
        }

        public static void RegisterCompanion(string unitType)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(unitType)) return;
            if (!OwnedCompanionUnitTypes.Add(unitType)) return;

            for (var i = 0; i < AllOptions.Count; i++)
            {
                var option = AllOptions[i];
                if (option.requiredCompanionUnitType == unitType)
                {
                    OptionPool.Add(option.id);
                }
            }
        }

        public static void EnterEvolutionaryMoment(int optionCount = 3)
        {
            if (IsInEvolutionaryMoment)
            {
                Debug.LogWarning("[EvolutionaryMoment] 当前已经处于进化时刻中，忽略重复触发。");
                return;
            }

            EnsureLoaded();
            CurrentOptions.Clear();

            var availableOptions = new List<EvolutionaryMomentOption>();
            foreach (var optionId in OptionPool)
            {
                if (SelectedOptionIds.Contains(optionId)) continue;
                if (!OptionById.TryGetValue(optionId, out var option)) continue;
                availableOptions.Add(option);
            }

            if (availableOptions.Count == 0)
            {
                Debug.LogWarning("[EvolutionaryMoment] 当前池子没有可选项，自动跳过本次进化时刻。");
                return;
            }

            var count = Mathf.Min(optionCount, availableOptions.Count);
            var pickedIndices = new HashSet<int>();

            while (CurrentOptions.Count < count)
            {
                var index = Random.Range(0, availableOptions.Count);
                if (!pickedIndices.Add(index)) continue;
                CurrentOptions.Add(availableOptions[index]);
            }

            Debug.Log("=== 进化时刻（Evolutionary Moment）===");
            for (var i = 0; i < CurrentOptions.Count; i++)
            {
                var option = CurrentOptions[i];
                Debug.Log($"[{i + 1}] {option.title} - {option.description}");
            }

            IsInEvolutionaryMoment = true;
            Time.timeScale = 0f;
            EvolutionaryMomentStarted?.Invoke(CurrentOptions);
        }

        public static bool ChooseOption(int optionIndex)
        {
            if (optionIndex < 0 || optionIndex >= CurrentOptions.Count)
            {
                Debug.LogWarning($"[EvolutionaryMoment] 选项索引非法: {optionIndex}");
                return false;
            }

            var chosenOption = CurrentOptions[optionIndex];
            SelectedOptionIds.Add(chosenOption.id);
            UnitManager.ApplyEvolutionaryMomentOption(chosenOption);

            if (chosenOption.nextOptionIds != null)
            {
                for (var i = 0; i < chosenOption.nextOptionIds.Length; i++)
                {
                    var nextId = chosenOption.nextOptionIds[i];
                    if (string.IsNullOrEmpty(nextId)) continue;
                    OptionPool.Add(nextId);
                }
            }

            Debug.Log($"[EvolutionaryMoment] 已选择：{chosenOption.title}");

            CurrentOptions.Clear();
            ExitEvolutionaryMoment();
            return true;
        }

        public static void ExitEvolutionaryMoment()
        {
            if (!IsInEvolutionaryMoment) return;

            IsInEvolutionaryMoment = false;
            Time.timeScale = 1f;
            EvolutionaryMomentEnded?.Invoke();
        }

        public static void OnUnitSpawned(ref UnitRuntimeData unit)
        {
            unit.attackIntervalScale = unit.attackIntervalScale <= 0f ? 1f : unit.attackIntervalScale;
            unit.projectileCount = unit.projectileCount < 1 ? 1 : unit.projectileCount;

            foreach (var optionId in SelectedOptionIds)
            {
                if (!OptionById.TryGetValue(optionId, out var option)) continue;
                if (!ShouldAffectUnit(option, unit)) continue;

                if (option.optionType == OptionTypeSkill)
                {
                    AddSkillRuntime(option, unit.id);
                    continue;
                }

                ApplyStatDeltasToUnit(ref unit, option);
            }
        }


        public static void ApplyOptionToExistingUnits(EvolutionaryMomentOption option, List<UnitRuntimeData> units)
        {
            for (var i = 0; i < units.Count; i++)
            {
                var unit = units[i];
                if (!unit.alive) continue;
                if (!ShouldAffectUnit(option, unit)) continue;

                if (option.optionType == OptionTypeSkill)
                {
                    AddSkillRuntime(option, unit.id);
                }
                else
                {
                    ApplyStatDeltasToUnit(ref unit, option);
                    units[i] = unit;
                }
            }
        }

        public static void TickSkills(List<UnitRuntimeData> units, float dt)
        {
            var skillContext = new EvolutionSkillContext(units, dt);

            for (var i = 0; i < SkillRuntimes.Count; i++)
            {
                var runtime = SkillRuntimes[i];
                var ownerIndex = FindUnitIndexById(units, runtime.ownerUnitId);

                if (ownerIndex < 0)
                {
                    SkillRuntimes.RemoveAt(i);
                    i--;
                    continue;
                }

                var owner = units[ownerIndex];
                if (!runtime.IsValidFor(owner))
                {
                    owner.attackIntervalScale = 1f;
                    units[ownerIndex] = owner;
                    SkillRuntimes.RemoveAt(i);
                    i--;
                    continue;
                }

                if (EvolutionSkillBehaviorRegistry.TryGetBehavior(runtime.skillId, out var behavior))
                {
                    behavior.Tick(ref runtime, ref owner, skillContext);
                }
                else
                {
                    owner.attackIntervalScale = 1f;
                }

                units[ownerIndex] = owner;
                SkillRuntimes[i] = runtime;
            }
        }

        public static float GetEffectiveAttackInterval(UnitRuntimeData unit)
        {
            var baseInterval = unit.attackInterval < 0.1f ? 0.1f : unit.attackInterval;
            var attackSpeed = unit.attackSpeed <= 0f ? 1f : unit.attackSpeed;
            var scale = unit.attackIntervalScale <= 0f ? 1f : unit.attackIntervalScale;
            var value = baseInterval / attackSpeed * scale;
            return value < 0.05f ? 0.05f : value;
        }

        private static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;

            var configText = Resources.Load<TextAsset>(ConfigPath);
            if (configText == null)
            {
                Debug.LogWarning($"[EvolutionaryMoment] 未找到配置文件: Resources/{ConfigPath}.json");
                return;
            }

            var list = JsonUtility.FromJson<EvolutionaryMomentOptionList>(configText.text);
            if (list?.options == null || list.options.Length == 0)
            {
                Debug.LogWarning("[EvolutionaryMoment] 配置文件为空");
                return;
            }

            AllOptions.AddRange(list.options);
            for (var i = 0; i < AllOptions.Count; i++)
            {
                var option = AllOptions[i];
                if (string.IsNullOrEmpty(option.id)) continue;
                OptionById[option.id] = option;
            }
        }

        private static bool ShouldAffectUnit(EvolutionaryMomentOption option, UnitRuntimeData unit)
        {
            if (option.targetType == TargetSpecificUnitType)
            {
                return unit.unitType == option.targetUnitType;
            }

            if (option.targetType == TargetAllCompanions)
            {
                return unit.faction == Faction.Player && unit.unitType != "PlayerBase";
            }

            if (option.targetType == TargetAllEnemies)
            {
                return unit.faction == Faction.Enemy;
            }

            return false;
        }

        private static void ApplyStatDeltasToUnit(ref UnitRuntimeData unit, EvolutionaryMomentOption option)
        {
            unit.attackInterval += option.attackIntervalDelta;
            unit.attackInterval = unit.attackInterval < 0.1f ? 0.1f : unit.attackInterval;

            unit.attackRange += option.attackRangeDelta;
            unit.attack += option.attackDelta;
            unit.projectileCount += option.projectileCountDelta;
            unit.projectileCount = unit.projectileCount < 1 ? 1 : unit.projectileCount;
        }

        private static int FindUnitIndexById(List<UnitRuntimeData> units, int unitId)
        {
            for (var i = 0; i < units.Count; i++)
            {
                if (units[i].id == unitId) return i;
            }

            return -1;
        }

        private static void AddSkillRuntime(EvolutionaryMomentOption option, int ownerUnitId)
        {
            if (string.IsNullOrEmpty(option.skillId))
            {
                return;
            }

            for (var i = 0; i < SkillRuntimes.Count; i++)
            {
                var runtime = SkillRuntimes[i];
                if (runtime.ownerUnitId == ownerUnitId && runtime.skillId == option.skillId)
                {
                    return;
                }
            }

            SkillRuntimes.Add(new EvolutionSkillRuntime
            {
                sourceOptionId = option.id,
                skillId = option.skillId,
                ownerUnitId = ownerUnitId,
                cooldown = option.skillCooldown,
                duration = option.skillDuration,
                attackIntervalScale = option.skillAttackIntervalScale <= 0f ? 1f : option.skillAttackIntervalScale,
                // 新获得技能时立刻可触发一次：让下一帧 Tick 直接进入激活态。
                cooldownTimer = option.skillCooldown,
                durationTimer = 0f,
                isActive = false
            });
        }

        private static void RebuildOptionPool()
        {
            OptionPool.Clear();
            for (var i = 0; i < AllOptions.Count; i++)
            {
                var option = AllOptions[i];
                if (string.IsNullOrEmpty(option.id)) continue;

                if (option.unlockedByDefault)
                {
                    OptionPool.Add(option.id);
                    continue;
                }

                if (!string.IsNullOrEmpty(option.requiredCompanionUnitType) &&
                    OwnedCompanionUnitTypes.Contains(option.requiredCompanionUnitType))
                {
                    OptionPool.Add(option.id);
                }
            }
        }
    }
}
