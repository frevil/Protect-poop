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

        public static void ConfigureLevelBounds(LevelSpawnPlan levelPlan)
        {
            _hasLevelBounds = false;

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
            _levelMin = levelPlan.spawnBoundsMin;
            _levelMax = levelPlan.spawnBoundsMax;
        }

        public static void ClearLevelBounds()
        {
            _hasLevelBounds = false;
        }

        public static Vector3 ClampToPlayableArea(Vector3 position)
        {
            if (TryGetActiveBounds(out var min, out var max))
            {
                return new Vector3(
                    Mathf.Clamp(position.x, min.x, max.x),
                    Mathf.Clamp(position.y, min.y, max.y),
                    position.z);
            }

            return position;
        }

        private static bool TryGetActiveBounds(out Vector2 min, out Vector2 max)
        {
            if (_hasLevelBounds)
            {
                min = _levelMin;
                max = _levelMax;
                return true;
            }

            var camera = Camera.main;
            if (camera == null)
            {
                min = DefaultSpawnMin;
                max = DefaultSpawnMax;
                return true;
            }

            if (camera.orthographic)
            {
                var halfHeight = camera.orthographicSize;
                var halfWidth = halfHeight * camera.aspect;
                var center = (Vector2)camera.transform.position;
                min = center - new Vector2(halfWidth, halfHeight);
                max = center + new Vector2(halfWidth, halfHeight);
                return true;
            }

            var depth = Mathf.Abs(camera.transform.position.z);
            var bottomLeft = camera.ScreenToWorldPoint(new Vector3(0f, 0f, depth));
            var topRight = camera.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, depth));
            min = new Vector2(Mathf.Min(bottomLeft.x, topRight.x), Mathf.Min(bottomLeft.y, topRight.y));
            max = new Vector2(Mathf.Max(bottomLeft.x, topRight.x), Mathf.Max(bottomLeft.y, topRight.y));
            return true;
        }
    }
}
