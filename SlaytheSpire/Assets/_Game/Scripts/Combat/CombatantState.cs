using System;
using UnityEngine;

namespace Roguelike.Combat
{
    // Runtime values only. Never write combat changes back to a definition asset.
    public sealed class CombatantState
    {
        public int MaxHealth { get; }
        public int CurrentHealth { get; private set; }
        public int Block { get; private set; }
        public event Action Changed;

        public CombatantState(int maxHealth)
        {
            if (maxHealth <= 0) throw new ArgumentOutOfRangeException(nameof(maxHealth));
            MaxHealth = maxHealth;
            CurrentHealth = maxHealth;
        }

        public void SetHealth(int value)
        {
            CurrentHealth = Mathf.Clamp(value, 0, MaxHealth);
            Changed?.Invoke();
        }

        public void SetBlock(int value)
        {
            Block = Mathf.Max(0, value);
            Changed?.Invoke();
        }

        public void TakeDamage(int amount)
        {
            if (amount <= 0 || CurrentHealth == 0) return;

            int absorbed = Mathf.Min(Block, amount);
            Block -= absorbed;
            CurrentHealth = Mathf.Max(0, CurrentHealth - (amount - absorbed));
            Changed?.Invoke();
        }
    }

    public sealed class PlayerBattleState
    {
        public CombatantState Combatant { get; }
        public int BaseEnergy { get; }
        public int CurrentEnergy { get; private set; }
        public event Action Changed;

        public PlayerBattleState(CharacterDefinition definition)
        {
            if (definition == null || !definition.IsValid)
                throw new ArgumentException("A valid character definition is required.");
            Combatant = new CombatantState(definition.MaxHealth);
            BaseEnergy = definition.BaseEnergy;
            CurrentEnergy = BaseEnergy;
        }

        public void SetEnergy(int value)
        {
            CurrentEnergy = Mathf.Max(0, value);
            Changed?.Invoke();
        }
    }
}
