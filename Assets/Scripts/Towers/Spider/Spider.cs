using Core;
using Manager;
using Scripts.Core;
using UnityEngine;

namespace Towers
{
    public class Spider
    {
        private static readonly UnitRuntimeData DefaultData = new()
        {
            name = "东方明蛛",
            unitType = "Spider",
            hp = 50,
            attack = 5,
            attackRange = 25f,
            attackInterval = 3f,
            attackIntervalScale = 1f,
            attackTimer = 0,
            moveSpeed = 0f,
            alive = true,
            isTargetable = true,
            position = new Vector3(-5f, 3.5f, 0f),
            faction = Faction.Player,
            targetIndex = -1
        };
        
        public static void Create()
        {
            UnitManager.SpawnUnit(DefaultData);
        }
    }
}
