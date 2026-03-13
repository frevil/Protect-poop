using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Manager.Evolution.Skills
{
    public static class EvolutionSkillBehaviorRegistry
    {
        private static readonly Dictionary<string, IEvolutionSkillBehavior> Behaviors = BuildBehaviorMap();

        public static bool TryGetBehavior(string skillId, out IEvolutionSkillBehavior behavior)
        {
            if (string.IsNullOrEmpty(skillId))
            {
                behavior = null;
                return false;
            }

            return Behaviors.TryGetValue(skillId, out behavior);
        }

        private static Dictionary<string, IEvolutionSkillBehavior> BuildBehaviorMap()
        {
            var result = new Dictionary<string, IEvolutionSkillBehavior>();
            var behaviorType = typeof(IEvolutionSkillBehavior);

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

                    var behavior = (IEvolutionSkillBehavior)Activator.CreateInstance(type);
                    result[behavior.SkillId] = behavior;
                }
            }

            return result;
        }
    }
}
