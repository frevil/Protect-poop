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
                "BlowFly" => CreateBlowFly(),
                "Sarcophagidae" => CreateSarcophagidae(),
                "FlyEgg" => CreateFlyEgg(),
                "Maggot" => CreateMaggot(),
                _ => CreateInvalidEnemy(enemyTypeId)
            };
        }

        public static UnitRuntimeData CreateMosquito()
        {
            return new UnitRuntimeData
            {
                alive = true,
                isTargetable = true,
                unitType = "Mosquito",
                moveSpeed = 1f,
                maxHp = 3,
                hp = 3,
                attack = 1,
                attackRange = 1,
                attackInterval = 3,
                attackSpeed = 1,
                attackIntervalScale = 1,
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
                isTargetable = true,
                unitType = "Fly",
                moveSpeed = 1f,
                maxHp = 10,
                hp = 10,
                attack = 2,
                attackRange = 1,
                attackInterval = 4,
                attackSpeed = 1,
                attackIntervalScale = 1,
                attackTimer = 0,
                faction = 1,
                targetIndex = -1,
                killExp = 2
            };
        }

        public static UnitRuntimeData CreateBlowFly()
        {
            return new UnitRuntimeData
            {
                alive = true,
                isTargetable = true,
                unitType = "BlowFly",
                moveSpeed = 2.8f,
                maxHp = 24,
                hp = 24,
                attack = 14,
                attackRange = 1,
                attackInterval = 5,
                attackSpeed = 1,
                attackIntervalScale = 1,
                attackTimer = 0,
                faction = 1,
                targetIndex = -1,
                killExp = 5
            };
        }

        public static UnitRuntimeData CreateSarcophagidae()
        {
            return new UnitRuntimeData
            {
                alive = true,
                isTargetable = true,
                unitType = "Sarcophagidae",
                moveSpeed = 1.4f,
                maxHp = 18,
                hp = 18,
                attack = 6,
                attackRange = 1.15f,
                attackInterval = 3,
                attackSpeed = 1,
                attackIntervalScale = 1,
                attackTimer = 0,
                faction = 1,
                targetIndex = -1,
                killExp = 4
            };
        }

        public static UnitRuntimeData CreateFlyEgg()
        {
            return new UnitRuntimeData
            {
                alive = true,
                isTargetable = false,
                unitType = "FlyEgg",
                moveSpeed = 0,
                maxHp = 1,
                hp = 1,
                attack = 0,
                attackRange = 0,
                attackInterval = 0,
                attackSpeed = 1,
                attackIntervalScale = 1,
                attackTimer = 0,
                faction = 1,
                targetIndex = -1,
                killExp = 0,
                controlState = UnitControlState.Suppressed | UnitControlState.CannotMove
            };
        }

        public static UnitRuntimeData CreateMaggot()
        {
            return new UnitRuntimeData
            {
                alive = true,
                isTargetable = false,
                unitType = "Maggot",
                moveSpeed = 0,
                maxHp = 1,
                hp = 1,
                attack = 0,
                attackRange = 0,
                attackInterval = 0,
                attackSpeed = 1,
                attackIntervalScale = 1,
                attackTimer = 0,
                faction = 1,
                targetIndex = -1,
                killExp = 0,
                controlState = UnitControlState.Suppressed | UnitControlState.CannotMove
            };
        }

        private static UnitRuntimeData CreateInvalidEnemy(string enemyTypeId)
        {
            Debug.LogWarning($"未知敌人类型: {enemyTypeId}");
            return new UnitRuntimeData
            {
                alive = false,
                isTargetable = false,
                unitType = enemyTypeId,
                faction = 1
            };
        }
    }
}
