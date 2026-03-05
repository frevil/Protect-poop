using Manager;
using Towers;
using UnityEditor;

namespace test
{
    public class TempTest
    {
        [MenuItem("测试/wave")]
        public static void GenerateAWave()
        {
            WaveSystem.GenerateMosquito(50);
        }
        
        [MenuItem("测试/创建呱呱")]
        public static void GenerateFrog()
        {
            Frog.Create();
        }
    }
}