using System.Collections.Generic;

namespace Manager.AttackBehaviors
{
    public static class AttackBehaviorRegistry
    {
        private static readonly Dictionary<string, IAttackBehavior> Behaviors = new()
        {
            { "Frog", new FrogAttackBehavior() },
            { "Spider", new SpiderAttackBehavior() }
        };

        public static bool TryGetBehavior(string unitType, out IAttackBehavior behavior)
        {
            return Behaviors.TryGetValue(unitType, out behavior);
        }

        public static IEnumerable<IAttackBehavior> GetAll()
        {
            return Behaviors.Values;
        }
    }
}
