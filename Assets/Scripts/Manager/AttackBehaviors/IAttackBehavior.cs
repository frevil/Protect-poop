using Core;

namespace Manager.AttackBehaviors
{
    public interface IAttackBehavior
    {
        string UnitType { get; }
        void Handle(ref UnitRuntimeData attacker, AttackContext context);
        void Tick(AttackContext context);
        void Cleanup(AttackContext context);
    }
}
