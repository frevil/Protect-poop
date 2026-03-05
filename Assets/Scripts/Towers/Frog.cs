using Core;
using Manager;
using Scripts.Core;
using UnityEngine;

namespace Towers
{
    public class Frog
    {
        private static readonly UnitRuntimeData DefaultData = new()
        {
            name = "青蛙",
            unitType = "Frog",
            hp = 100,
            attack = 10,
            attackRange = 25f,
            attackInterval = 1f,
            attackTimer = 0,
            moveSpeed = 0f,
            alive = true,
            position = new Vector3(-12,-7,0),
            faction = Faction.Player,
            targetIndex = -1
        };
        
        public static void Create()
        {
            UnitManager.SpawnUnit(DefaultData);
        }
    }
}