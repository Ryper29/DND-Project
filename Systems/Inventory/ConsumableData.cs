namespace DnD.Systems.Inventory
{
    public enum ConsumableEffect
    {
        Heal,
        RestoreMana
    }

    public class ConsumableData
    {
        public string Name { get; }
        public ConsumableEffect Effect { get; }
        public int Amount { get; }

        public ConsumableData(string name, ConsumableEffect effect, int amount)
        {
            if (string.IsNullOrWhiteSpace(name)) throw new System.ArgumentException("Name is required.", nameof(name));
            if (amount <= 0) throw new System.ArgumentOutOfRangeException(nameof(amount));
            Name = name;
            Effect = effect;
            Amount = amount;
        }
    }
}