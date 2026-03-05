using Manager;
using UnityEditor;

namespace Scripts.test
{
    public class TempTest
    {
        [MenuItem("测试/wave")]
        public static void GenerateAWave()
        {
            WaveSystem.GenerateMosquito(50);
        }
    }
}