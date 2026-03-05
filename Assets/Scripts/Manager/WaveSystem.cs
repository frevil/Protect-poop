using Enemies;
using UnityEngine;

namespace Manager
{
    public class WaveSystem
    {
        private static readonly Vector3 BasePosition = new(10,5);

        public static void GenerateMosquito(int count = 30)
        {
            for (var i = 0; i < count; i++)
            {
                var newMos = EnemiesFactor.CreateMosquito();
                newMos.name = $"蚊子_{i}";
                newMos.position = BasePosition + new Vector3(Random.Range(0,3f),Random.Range(0,3f),Random.Range(0,3f));
                UnitManager.SpawnUnit(newMos);
            }
        }
    }
}