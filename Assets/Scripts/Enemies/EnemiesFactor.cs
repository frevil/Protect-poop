using Core;

namespace Enemies
{
    public class EnemiesFactor
    {
        public static UnitRuntimeData CreateMosquito()
        {
            return new UnitRuntimeData()
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
    }
}