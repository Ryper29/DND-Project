namespace DnD.Entities
{
    public interface ICombatant
    {
        string Name { get; }
        Race Race { get; }
        bool IsAlive { get; }
        int CurrentHealth { get; }
        int MaxHealth { get; }
        int AttackPower { get; }
        int DefensePower { get; }
        void TakeDamage(int rawDamage);
        void PerformAttack(ICombatant target);
    }

    public interface IDamageable
    {
        void TakeDamage(int rawDamage);
        int CurrentHealth { get; }
        int MaxHealth { get; }
        bool IsAlive { get; }
    }

    public interface ILevelable
    {
        int Level { get; }
        int Experience { get; }
        int ExperienceToNextLevel { get; }
        void GainExperience(int amount);
    }
}
