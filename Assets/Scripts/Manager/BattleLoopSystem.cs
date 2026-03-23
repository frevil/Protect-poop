using System.Collections.Generic;
using Core;

namespace Manager
{
    public static class BattleLoopSystem
    {
        public static void Tick(List<UnitRuntimeData> units, float elapsedBattleTime, float deltaTime)
        {
            EncounterDirector.Tick(elapsedBattleTime);
            TargetingSystem.UpdateTargets(units);
            MovementSystem.MoveUnits(units, deltaTime);
            AttackSystem.HandleAttack(units, deltaTime);
            DeathSystem.HandleDeath(units);
        }
    }
}
