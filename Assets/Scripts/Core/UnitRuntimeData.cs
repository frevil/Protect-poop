using UnityEngine;

namespace Core
{
    public struct UnitRuntimeData
    {
        public int id;
        public string name;

        public Vector3 position;
        public float moveSpeed;

        public float hp;
        public float maxHp;

        public float attack;
        public float attackRange;

        public float attackInterval;
        public float attackTimer;

        public int faction;        // 0 = Player, 1 = Enemy
        public int targetIndex;    // -1 表示无目标
        
        public int exp;    // 经验值
        public int killExp;    // 被击杀时掉落的经验值

        public bool alive;

        public static UnitRuntimeData Empty = new()
        {
            id = -1
        };
        public static UnitRuntimeData Player = new()
        {
            id = 0,
            name = "便便",
            hp = 100,
            attack = 0,
            attackRange = 0,
            attackInterval = 1,
            attackTimer = 0,
            maxHp = 100,
            faction = 0,
            targetIndex = -1,
            alive = true,
            position = new Vector3(-10,-5,0)
        };

        public bool IsEmpty()
        {
            return id == -1;
        }
    }
}