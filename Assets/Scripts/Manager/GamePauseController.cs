using System.Collections.Generic;
using UnityEngine;

namespace Manager
{
    public enum PauseSource
    {
        Settlement,
        EvolutionaryMoment
    }

    public static class GamePauseController
    {
        private static readonly HashSet<PauseSource> ActiveSources = new();

        public static bool IsPaused => ActiveSources.Count > 0;

        public static void RequestPause(PauseSource source)
        {
            if (!ActiveSources.Add(source))
            {
                return;
            }

            ApplyTimeScale();
        }

        public static void ReleasePause(PauseSource source)
        {
            if (!ActiveSources.Remove(source))
            {
                return;
            }

            ApplyTimeScale();
        }

        public static void Reset()
        {
            ActiveSources.Clear();
            ApplyTimeScale();
        }

        private static void ApplyTimeScale()
        {
            Time.timeScale = IsPaused ? 0f : 1f;
        }
    }
}
