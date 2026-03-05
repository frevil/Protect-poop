using UnityEngine;

namespace Manager
{
    public class LevelSystem
    {
        private static int _exp = 0;
        private static int _expNeed = 1;
        private static int _level = 1;
        public static void GotExp(int value)
        {
            _exp += value;
            CheckLevelUp();
        }

        private static void CheckLevelUp()
        {
            if (_exp >= _expNeed)
            {
                _exp -= _expNeed;
                LevelUp();
            }
        }

        private static void LevelUp()
        {
            _expNeed *= 2;
            _level += 1;
            Debug.Log($"升级了，当前等级{_level}");
        }
    }
}