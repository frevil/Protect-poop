using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Manager.AttackBehaviors
{
    public static class AttackBehaviorRegistry
    {
        private static readonly Dictionary<string, IAttackBehavior> Behaviors = BuildBehaviorMap();

        public static bool TryGetBehavior(string unitType, out IAttackBehavior behavior)
        {
            return Behaviors.TryGetValue(unitType, out behavior);
        }

        public static IEnumerable<IAttackBehavior> GetAll()
        {
            return Behaviors.Values;
        }

        private static Dictionary<string, IAttackBehavior> BuildBehaviorMap()
        {
            var result = new Dictionary<string, IAttackBehavior>();
            var behaviorType = typeof(IAttackBehavior);

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    types = e.Types.Where(t => t != null).ToArray();
                }

                foreach (var type in types)
                {
                    if (type == null || type.IsAbstract || type.IsInterface) continue;
                    if (!behaviorType.IsAssignableFrom(type)) continue;
                    if (type.GetConstructor(Type.EmptyTypes) == null) continue;

                    var behavior = (IAttackBehavior)Activator.CreateInstance(type);
                    result[behavior.UnitType] = behavior;
                }
            }

            return result;
        }
    }
}
