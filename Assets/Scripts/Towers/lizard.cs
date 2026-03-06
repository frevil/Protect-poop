using Core;
using Manager;
using Scripts.Core;
using UnityEngine;

namespace Towers
{
    public class lizard
    {
        private static readonly UnitRuntimeData DefaultData = new()
        {
            name = "独立游蜴",
            unitType = "lizard",
            hp = 85,
            attack = 15,
            attackRange = 3f,
            attackInterval = 2f,
            attackTimer = 0,
            moveSpeed = 3f,
            alive = true,
            position = new Vector3(-11,-4,0),
            faction = Faction.Player,
            targetIndex = -1
        };
        
        public static void Create()
        {
            UnitManager.SpawnUnit(DefaultData);
        }
    }
}