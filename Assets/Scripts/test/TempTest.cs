using Manager;
using Manager.Evolution;
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

        [MenuItem("测试/进化时刻/触发")]
        public static void TriggerEvolutionaryMoment()
        {
            EvolutionaryMomentSystem.EnterEvolutionaryMoment();
        }

        [MenuItem("测试/进化时刻/选择1")]
        public static void PickEvolutionaryOption1()
        {
            EvolutionaryMomentSystem.ChooseOption(0);
        }

        [MenuItem("测试/进化时刻/选择2")]
        public static void PickEvolutionaryOption2()
        {
            EvolutionaryMomentSystem.ChooseOption(1);
        }

        [MenuItem("测试/进化时刻/选择3")]
        public static void PickEvolutionaryOption3()
        {
            EvolutionaryMomentSystem.ChooseOption(2);
        }
    }
}
