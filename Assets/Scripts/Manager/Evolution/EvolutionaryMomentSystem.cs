using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Manager.Evolution
{
    public static class EvolutionaryMomentSystem
    {
        private const string ConfigPath = "Configs/EvolutionaryMomentOptions";
        private static readonly List<EvolutionaryMomentOption> AllOptions = new();
        private static readonly List<EvolutionaryMomentOption> CurrentOptions = new();
        private static bool _loaded;
        public static bool IsInEvolutionaryMoment { get; private set; }

        public static event Action<IReadOnlyList<EvolutionaryMomentOption>> EvolutionaryMomentStarted;
        public static event Action EvolutionaryMomentEnded;

        public static IReadOnlyList<EvolutionaryMomentOption> GetCurrentOptions()
        {
            return CurrentOptions;
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

            if (AllOptions.Count == 0)
            {
                Debug.LogWarning("[EvolutionaryMoment] 没有可用配置项，请检查 Configs/EvolutionaryMomentOptions.json");
                return;
            }

            var count = Mathf.Min(optionCount, AllOptions.Count);
            var pickedIndices = new HashSet<int>();

            while (CurrentOptions.Count < count)
            {
                var index = Random.Range(0, AllOptions.Count);
                if (!pickedIndices.Add(index)) continue;
                CurrentOptions.Add(AllOptions[index]);
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
            UnitManager.ApplyEvolutionaryMomentOption(chosenOption);
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
        }
    }
}
