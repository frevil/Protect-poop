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
        private static readonly Dictionary<int, float> SlowUntilByUnitId = new();
        private static readonly Dictionary<int, float> SlowRatioByUnitId = new();
        private static readonly Dictionary<int, PoisonRuntime> PoisonByUnitId = new();
        private static readonly Dictionary<int, int> KillCountByUnitId = new();
        private static readonly Dictionary<int, int> ProThresholdCountByUnitId = new();

        private static bool _loaded;
        private static float _elapsedTime;
        private static float _expGainMultiplier = 1f;
        private static float _frogNutritionChanceOnKill;
        private static float _frogHealOnKill;
        private static int _frogProThreshold = 100;
        private static float _frogPermanentAttackSpeedGainPerThreshold;
        private static float _spiderSlowPercent;
        private static float _spiderSlowDuration;
        private static float _spiderPoisonDamagePerSecond;
        private static float _spiderPoisonDuration;
        private static bool _spiderPoisonDamageDoublesOnSlowed;
        private static bool _spiderPoisonSpreadOnDeath;
        private static float _spiderBurstChance;
        private static int _spiderBurstProjectileCount;
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
            SlowUntilByUnitId.Clear();
            SlowRatioByUnitId.Clear();
            PoisonByUnitId.Clear();
            KillCountByUnitId.Clear();
            ProThresholdCountByUnitId.Clear();
            _elapsedTime = 0f;
            _expGainMultiplier = 1f;
            _frogNutritionChanceOnKill = 0f;
            _frogHealOnKill = 0f;
            _frogProThreshold = 100;
            _frogPermanentAttackSpeedGainPerThreshold = 0f;
            _spiderSlowPercent = 0f;
            _spiderSlowDuration = 0f;
            _spiderPoisonDamagePerSecond = 0f;
            _spiderPoisonDuration = 0f;
            _spiderPoisonDamageDoublesOnSlowed = false;
            _spiderPoisonSpreadOnDeath = false;
            _spiderBurstChance = 0f;
            _spiderBurstProjectileCount = 0;
            RebuildOptionPool();

            ExitEvolutionaryMoment();
        }

        public static void ResetForNewStage()
        {
            EnsureLoaded();

            CurrentOptions.Clear();
            SlowUntilByUnitId.Clear();
            SlowRatioByUnitId.Clear();
            PoisonByUnitId.Clear();
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
            ApplyGlobalModifiers(option);

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
            _elapsedTime += dt;
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

            TickPoison(units, dt);
            CleanupExpiredSlow(units);
        }

        public static int GetModifiedKillExp(int baseExp)
        {
            return Mathf.Max(0, Mathf.RoundToInt(baseExp * _expGainMultiplier));
        }

        public static float GetMovementSpeedScale(UnitRuntimeData unit)
        {
            if (!SlowUntilByUnitId.TryGetValue(unit.id, out var untilTime)) return 1f;
            if (untilTime <= _elapsedTime) return 1f;
            return SlowRatioByUnitId.TryGetValue(unit.id, out var ratio) ? ratio : 1f;
        }

        public static void OnEnemyKilled(List<UnitRuntimeData> units, UnitRuntimeData deadEnemy)
        {
            var killerIndex = FindUnitIndexById(units, deadEnemy.lastDamagerUnitId);
            if (killerIndex >= 0)
            {
                var killer = units[killerIndex];
                if (killer.alive && killer.faction == Faction.Player)
                {
                    OnPlayerUnitScoredKill(ref killer);
                    units[killerIndex] = killer;
                }
            }

            if (_spiderPoisonSpreadOnDeath && PoisonByUnitId.TryGetValue(deadEnemy.id, out var poison))
            {
                TrySpreadPoison(units, deadEnemy, poison);
            }

            SlowUntilByUnitId.Remove(deadEnemy.id);
            SlowRatioByUnitId.Remove(deadEnemy.id);
            PoisonByUnitId.Remove(deadEnemy.id);
        }

        public static void ApplySpiderWebHit(List<UnitRuntimeData> units, int targetIndex, int spiderId)
        {
            if (targetIndex < 0 || targetIndex >= units.Count) return;
            var enemy = units[targetIndex];
            if (!enemy.alive || enemy.faction != Faction.Enemy) return;

            if (_spiderSlowPercent > 0f && _spiderSlowDuration > 0f)
            {
                enemy.controlState |= UnitControlState.Slowed;
                units[targetIndex] = enemy;
                SlowUntilByUnitId[enemy.id] = _elapsedTime + _spiderSlowDuration;
                SlowRatioByUnitId[enemy.id] = Mathf.Clamp01(1f - _spiderSlowPercent);
            }

            if (_spiderPoisonDamagePerSecond > 0f && _spiderPoisonDuration > 0f)
            {
                PoisonByUnitId[enemy.id] = new PoisonRuntime
                {
                    sourceUnitId = spiderId,
                    damagePerSecond = _spiderPoisonDamagePerSecond,
                    endTime = _elapsedTime + _spiderPoisonDuration
                };
            }
        }

        public static int GetSpiderBurstProjectileCount()
        {
            if (_spiderBurstProjectileCount < 2) return 0;
            if (_spiderBurstChance <= 0f) return 0;
            return Random.value <= _spiderBurstChance ? _spiderBurstProjectileCount : 0;
        }

        public static float GetEffectiveAttackInterval(UnitRuntimeData unit)
        {
            var baseInterval = unit.attackInterval < 0.1f ? 0.1f : unit.attackInterval;
            var attackSpeed = unit.attackSpeed <= 0f ? 1f : unit.attackSpeed;
            var scale = unit.attackIntervalScale <= 0f ? 1f : unit.attackIntervalScale;
            var value = baseInterval / attackSpeed * scale;
            return value < 0.05f ? 0.05f : value;
        }

        public static int GetSkillRuntimesForOwner(int ownerUnitId, List<EvolutionSkillRuntime> buffer)
        {
            if (buffer == null) return 0;
            buffer.Clear();

            for (var i = 0; i < SkillRuntimes.Count; i++)
            {
                var runtime = SkillRuntimes[i];
                if (runtime.ownerUnitId != ownerUnitId) continue;
                buffer.Add(runtime);
            }

            return buffer.Count;
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

        private static void ApplyGlobalModifiers(EvolutionaryMomentOption option)
        {
            _expGainMultiplier += option.expGainMultiplierDelta;
            if (_expGainMultiplier < 0f) _expGainMultiplier = 0f;

            _frogNutritionChanceOnKill += option.nutritionDropChanceOnKill;
            _frogHealOnKill += option.healOnKill;
            if (option.killCountThreshold > 0)
            {
                _frogProThreshold = option.killCountThreshold;
            }

            _frogPermanentAttackSpeedGainPerThreshold += option.permanentAttackSpeedGainPerThreshold;

            if (option.slowPercent > 0f) _spiderSlowPercent = option.slowPercent;
            if (option.slowDuration > 0f) _spiderSlowDuration = option.slowDuration;
            if (option.poisonDamagePerSecond > 0f) _spiderPoisonDamagePerSecond = option.poisonDamagePerSecond;
            if (option.poisonDuration > 0f) _spiderPoisonDuration = option.poisonDuration;
            _spiderPoisonDamageDoublesOnSlowed |= option.poisonDamageDoublesOnSlowed;
            _spiderPoisonSpreadOnDeath |= option.poisonSpreadOnDeath;
            if (option.burstChance > 0f) _spiderBurstChance = option.burstChance;
            if (option.burstProjectileCount > 0) _spiderBurstProjectileCount = option.burstProjectileCount;
        }

        private static int FindUnitIndexById(List<UnitRuntimeData> units, int unitId)
        {
            for (var i = 0; i < units.Count; i++)
            {
                if (units[i].id == unitId) return i;
            }

            return -1;
        }

        private static void TickPoison(List<UnitRuntimeData> units, float dt)
        {
            if (PoisonByUnitId.Count == 0) return;

            var pendingRemove = new List<int>();
            foreach (var pair in PoisonByUnitId)
            {
                if (pair.Value.endTime <= _elapsedTime)
                {
                    pendingRemove.Add(pair.Key);
                    continue;
                }

                var index = FindUnitIndexById(units, pair.Key);
                if (index < 0)
                {
                    pendingRemove.Add(pair.Key);
                    continue;
                }

                var unit = units[index];
                if (!unit.alive)
                {
                    pendingRemove.Add(pair.Key);
                    continue;
                }

                var damage = pair.Value.damagePerSecond * dt;
                if (_spiderPoisonDamageDoublesOnSlowed &&
                    SlowUntilByUnitId.TryGetValue(unit.id, out var slowUntil) &&
                    slowUntil > _elapsedTime)
                {
                    damage *= 2f;
                }

                unit.hp -= damage;
                unit.lastDamagerUnitId = pair.Value.sourceUnitId;
                UnitManager.RecordDamagePopup(unit.id, damage);
                units[index] = unit;
            }

            for (var i = 0; i < pendingRemove.Count; i++)
            {
                PoisonByUnitId.Remove(pendingRemove[i]);
            }

        }

        private static void CleanupExpiredSlow(List<UnitRuntimeData> units)
        {
            if (SlowUntilByUnitId.Count == 0) return;
            var pendingRemove = new List<int>();
            foreach (var pair in SlowUntilByUnitId)
            {
                if (pair.Value > _elapsedTime) continue;
                pendingRemove.Add(pair.Key);
            }

            for (var i = 0; i < pendingRemove.Count; i++)
            {
                var unitId = pendingRemove[i];
                SlowUntilByUnitId.Remove(unitId);
                SlowRatioByUnitId.Remove(unitId);

                var index = FindUnitIndexById(units, unitId);
                if (index < 0) continue;
                var unit = units[index];
                unit.controlState &= ~UnitControlState.Slowed;
                units[index] = unit;
            }

        }

        private static void OnPlayerUnitScoredKill(ref UnitRuntimeData killer)
        {
            if (killer.unitType != "Frog") return;

            if (_frogHealOnKill > 0f)
            {
                var maxHp = killer.maxHp > 0f ? killer.maxHp : killer.hp;
                killer.hp = Mathf.Min(maxHp, killer.hp + _frogHealOnKill);
            }

            if (_frogNutritionChanceOnKill > 0f && Random.value <= _frogNutritionChanceOnKill)
            {
                UnitManager.GrantNutrition(1);
            }

            if (_frogPermanentAttackSpeedGainPerThreshold <= 0f || _frogProThreshold <= 0) return;

            var currentKillCount = 0;
            KillCountByUnitId.TryGetValue(killer.id, out currentKillCount);
            currentKillCount += 1;
            KillCountByUnitId[killer.id] = currentKillCount;

            var gainedThresholdCount = currentKillCount / _frogProThreshold;
            var appliedThresholdCount = 0;
            ProThresholdCountByUnitId.TryGetValue(killer.id, out appliedThresholdCount);
            if (gainedThresholdCount <= appliedThresholdCount) return;

            var additionalThresholdCount = gainedThresholdCount - appliedThresholdCount;
            killer.attackSpeed *= Mathf.Pow(1f + _frogPermanentAttackSpeedGainPerThreshold, additionalThresholdCount);
            ProThresholdCountByUnitId[killer.id] = gainedThresholdCount;
        }

        private static void TrySpreadPoison(List<UnitRuntimeData> units, UnitRuntimeData deadEnemy, PoisonRuntime sourcePoison)
        {
            var bestIndex = -1;
            var bestDistance = float.MaxValue;
            for (var i = 0; i < units.Count; i++)
            {
                var candidate = units[i];
                if (!candidate.alive || candidate.faction != Faction.Enemy) continue;
                if (candidate.id == deadEnemy.id) continue;
                if (PoisonByUnitId.ContainsKey(candidate.id)) continue;

                var distance = Vector3.Distance(candidate.position, deadEnemy.position);
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                bestIndex = i;
            }

            if (bestIndex < 0) return;
            var target = units[bestIndex];
            PoisonByUnitId[target.id] = new PoisonRuntime
            {
                sourceUnitId = sourcePoison.sourceUnitId,
                damagePerSecond = sourcePoison.damagePerSecond,
                endTime = _elapsedTime + _spiderPoisonDuration
            };
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
                    runtime.cooldown = option.skillCooldown;
                    runtime.duration = option.skillDuration;
                    runtime.attackIntervalScale = option.skillAttackIntervalScale <= 0f ? 1f : option.skillAttackIntervalScale;
                    SkillRuntimes[i] = runtime;
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

        private struct PoisonRuntime
        {
            public int sourceUnitId;
            public float damagePerSecond;
            public float endTime;
        }
    }
}
