using UnityEngine;

namespace Manager
{
    public static class SpawnPositionResolver
    {
        private static readonly Vector2 DefaultSpawnMin = new(-8f, -4.5f);
        private static readonly Vector2 DefaultSpawnMax = new(8f, 4.5f);

        private static bool _hasLevelBounds;
        private static Vector2 _levelMin;
        private static Vector2 _levelMax;
        private static bool _useNormalizedCoordinates;

        public static void ConfigureLevelBounds(LevelSpawnPlan levelPlan)
        {
            _hasLevelBounds = false;
            _useNormalizedCoordinates = false;

            if (levelPlan == null || !levelPlan.useSpawnBounds)
            {
                return;
            }

            if (levelPlan.spawnBoundsMax.x <= levelPlan.spawnBoundsMin.x ||
                levelPlan.spawnBoundsMax.y <= levelPlan.spawnBoundsMin.y)
            {
                return;
            }

            _hasLevelBounds = true;
            if (levelPlan.useNormalizedCoordinates)
            {
                _levelMin = NormalizedToWorld(levelPlan.spawnBoundsMin);
                _levelMax = NormalizedToWorld(levelPlan.spawnBoundsMax);
            }
            else
            {
                _levelMin = levelPlan.spawnBoundsMin;
                _levelMax = levelPlan.spawnBoundsMax;
            }

            _useNormalizedCoordinates = levelPlan.useNormalizedCoordinates;
        }

        public static void ClearLevelBounds()
        {
            _hasLevelBounds = false;
            _useNormalizedCoordinates = false;
        }

        public static Vector3 ResolveSpawnPosition(Vector3 configuredPosition)
        {
            var world = _useNormalizedCoordinates
                ? BattleViewBounds.NormalizedToWorld(configuredPosition)
                : configuredPosition;
            return BattleViewBounds.EnsurePlaneZ(world);
        }

        public static Vector3 ResolveSpawnOffset(Vector3 configuredRandomRange)
        {
            var range = ResolveConfiguredOffset(new Vector2(configuredRandomRange.x, configuredRandomRange.y));

            return new Vector3(
                Random.Range(-range.x, range.x),
                Random.Range(-range.y, range.y),
                0f);
        }

        public static Vector2 ResolveConfiguredOffset(Vector2 configuredOffset)
        {
            return _useNormalizedCoordinates
                ? BattleViewBounds.NormalizedToWorldOffset(configuredOffset)
                : configuredOffset;
        }

        public static Vector3 ClampToPlayableArea(Vector3 position)
        {
            if (TryGetActiveBounds(out var min, out var max))
            {
                return new Vector3(
                    Mathf.Clamp(position.x, min.x, max.x),
                    Mathf.Clamp(position.y, min.y, max.y),
                    0f);
            }

            position.z = 0f;
            return position;
        }

        public static bool TryGetPlayableBounds(out Vector2 min, out Vector2 max)
        {
            return TryGetActiveBounds(out min, out max);
        }

        private static bool TryGetActiveBounds(out Vector2 min, out Vector2 max)
        {
            if (_hasLevelBounds)
            {
                min = _levelMin;
                max = _levelMax;
                return true;
            }

            if (BattleViewBounds.TryGetViewRectOnBattlePlane(out var center, out var halfSize))
            {
                min = center - halfSize;
                max = center + halfSize;
                return true;
            }

            min = DefaultSpawnMin;
            max = DefaultSpawnMax;
            return true;
        }

        private static Vector2 NormalizedToWorld(Vector2 normalized)
        {
            var world = BattleViewBounds.NormalizedToWorld(new Vector3(normalized.x, normalized.y, 0f));
            return new Vector2(world.x, world.y);
        }
    }
}
