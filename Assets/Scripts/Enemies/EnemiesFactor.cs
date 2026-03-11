using Core;
using UnityEngine;

namespace Enemies
{
    public class EnemiesFactor
    {
        public static UnitRuntimeData CreateByTypeId(string enemyTypeId)
        {
            return enemyTypeId switch
            {
                "Mosquito" => CreateMosquito(),
                "Fly" => CreateFly(),
                _ => CreateInvalidEnemy(enemyTypeId)
            };
        }

        public static UnitRuntimeData CreateMosquito()
        {
            return new UnitRuntimeData
            {
                alive = true,
                unitType = "Mosquito",
                moveSpeed = 1f,
                maxHp = 3,
                hp = 3,
                attack = 1,
                attackRange = 1,
                attackInterval = 3,
                attackTimer = 0,
                faction = 1,
                targetIndex = -1,
                killExp = 1
            };
        }

        public static UnitRuntimeData CreateFly()
        {
            return new UnitRuntimeData
            {
                alive = true,
                unitType = "fly",
                moveSpeed = 1f,
                maxHp = 10,
                hp = 10,
                attack = 2,
                attackRange = 1,
                attackInterval = 4,
                attackTimer = 0,
                faction = 1,
                targetIndex = -1,
                killExp = 2
            };
        }

        private static UnitRuntimeData CreateInvalidEnemy(string enemyTypeId)
        {
            Debug.LogWarning($"未知敌人类型: {enemyTypeId}");
            return new UnitRuntimeData
            {
                alive = false,
                unitType = enemyTypeId,
                faction = 1
            };
        }
    }
}
