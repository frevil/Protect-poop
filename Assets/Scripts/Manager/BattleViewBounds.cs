using UnityEngine;

namespace Manager
{
    public static class BattleViewBounds
    {
        private const float PlaneZ = 0f;
        private static readonly Vector2 FallbackHalfSize = new(8f, 4.5f);

        public static Vector3 EnsurePlaneZ(Vector3 worldPosition)
        {
            worldPosition.z = PlaneZ;
            return worldPosition;
        }

        public static Vector3 NormalizedToWorld(Vector3 normalizedPosition)
        {
            if (!TryGetViewRectOnBattlePlane(out var center, out var halfSize))
            {
                center = Vector2.zero;
                halfSize = FallbackHalfSize;
            }

            var min = center - halfSize;
            var size = halfSize * 2f;

            return new Vector3(
                min.x + normalizedPosition.x * size.x,
                min.y + normalizedPosition.y * size.y,
                PlaneZ);
        }

        public static Vector2 NormalizedToWorldOffset(Vector2 normalizedOffset)
        {
            if (!TryGetViewRectOnBattlePlane(out _, out var halfSize))
            {
                halfSize = FallbackHalfSize;
            }

            var size = halfSize * 2f;

            return new Vector2(normalizedOffset.x * size.x, normalizedOffset.y * size.y);
        }

        public static Vector3 WorldToNormalized(Vector3 worldPosition)
        {
            if (!TryGetViewRectOnBattlePlane(out var center, out var halfSize))
            {
                center = Vector2.zero;
                halfSize = FallbackHalfSize;
            }

            var safeHalfWidth = Mathf.Max(halfSize.x, 0.0001f);
            var safeHalfHeight = Mathf.Max(halfSize.y, 0.0001f);
            var min = center - halfSize;
            return new Vector3(
                (worldPosition.x - min.x) / (safeHalfWidth * 2f),
                (worldPosition.y - min.y) / (safeHalfHeight * 2f),
                0f);
        }

        public static bool TryGetViewRectOnBattlePlane(out Vector2 center, out Vector2 halfSize)
        {
            var camera = Camera.main;
            if (camera == null)
            {
                center = Vector2.zero;
                halfSize = FallbackHalfSize;
                return false;
            }

            if (camera.orthographic)
            {
                var h = camera.orthographicSize;
                var w = h * camera.aspect;
                center = new Vector2(camera.transform.position.x, camera.transform.position.y);
                halfSize = new Vector2(w, h);
                return true;
            }

            if (!TryScreenPointToPlane(camera, Vector3.zero, out var bl) ||
                !TryScreenPointToPlane(camera, new Vector3(Screen.width, Screen.height, 0f), out var tr))
            {
                center = Vector2.zero;
                halfSize = FallbackHalfSize;
                return false;
            }

            var min = new Vector2(Mathf.Min(bl.x, tr.x), Mathf.Min(bl.y, tr.y));
            var max = new Vector2(Mathf.Max(bl.x, tr.x), Mathf.Max(bl.y, tr.y));
            center = (min + max) * 0.5f;
            halfSize = (max - min) * 0.5f;
            return true;
        }

        public static bool TryGetMouseWorldPositionOnBattlePlane(out Vector3 world)
        {
            var camera = Camera.main;
            if (camera == null)
            {
                world = Vector3.zero;
                return false;
            }

            return TryScreenPointToPlane(camera, Input.mousePosition, out world);
        }

        private static bool TryScreenPointToPlane(Camera camera, Vector3 screenPoint, out Vector3 worldPoint)
        {
            var ray = camera.ScreenPointToRay(screenPoint);
            var delta = ray.direction.z;
            if (Mathf.Abs(delta) < 0.0001f)
            {
                worldPoint = Vector3.zero;
                return false;
            }

            var t = (PlaneZ - ray.origin.z) / delta;
            if (t < 0f)
            {
                worldPoint = Vector3.zero;
                return false;
            }

            worldPoint = ray.origin + ray.direction * t;
            worldPoint.z = PlaneZ;
            return true;
        }
    }
}
