using System;

namespace Core
{
    [Flags]
    public enum UnitControlState
    {
        Normal = 0,
        CannotMove = 1 << 0,
        Suppressed = 1 << 1,
        Slowed = 1 << 2,
        Confused = 1 << 3,
    }
}
