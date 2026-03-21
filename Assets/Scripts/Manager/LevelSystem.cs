using Manager.Evolution;
using UnityEngine;

namespace Manager
{
    public class LevelSystem
    {
        private static int _exp;
        private static int _expNeed = 1;
        private static int _level = 1;


        public static void Reset()
        {
            _exp = 0;
            _expNeed = 1;
            _level = 1;
        }

        public static void GotExp(int value)
        {
            _exp += value;
            CheckLevelUp();
        }

        private static void CheckLevelUp()
        {
            while (_exp >= _expNeed)
            {
                _exp -= _expNeed;
                LevelUp();
            }
        }

        private static void LevelUp()
        {
            var levelM1 = _level - 1;
            _expNeed = 10 + 6 * levelM1 + 3 * levelM1 * levelM1;
            _level += 1;
            Debug.Log($"升级了，当前等级{_level}");

            EvolutionaryMomentSystem.EnterEvolutionaryMoment();
        }
    }
}
