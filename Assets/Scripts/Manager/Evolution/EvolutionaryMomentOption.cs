using System;

namespace Manager.Evolution
{
    [Serializable]
    public class EvolutionaryMomentOption
    {
        public string id;
        public string title;
        public string description;

        public float attackIntervalDelta;
        public float attackRangeDelta;
        public float attackDelta;
    }
}
