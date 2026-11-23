using System;
using UnityEngine;

namespace Roguelike.Combat
{
    public enum EnemyActionType { Attack, Block }

    [Serializable]
    public sealed class EnemyAction
    {
        [SerializeField] private EnemyActionType type;
        [SerializeField, Min(0)] private int amount = 6;
        public EnemyActionType Type => type;
        public int Amount => amount;
        public bool IsValid => Enum.IsDefined(typeof(EnemyActionType), type) && amount >= 0;
    }

    [CreateAssetMenu(menuName = "Roguelike/Enemy")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        [SerializeField] private string enemyId = "training-slime";
        [SerializeField] private string displayName = "Training Slime";
        [SerializeField] private Sprite portrait;
        [SerializeField, Min(1)] private int maxHealth = 30;
        [SerializeField] private EnemyAction[] actions = { new EnemyAction() };

        public string Id => enemyId;
        public string DisplayName => displayName;
        public Sprite Portrait => portrait;
        public int MaxHealth => maxHealth;
        public int ActionCount => actions == null ? 0 : actions.Length;
        public EnemyAction GetAction(int index) => actions[index];
        public bool IsValid
        {
            get
            {
                if (string.IsNullOrWhiteSpace(enemyId) || string.IsNullOrWhiteSpace(displayName)
                    || maxHealth <= 0 || ActionCount == 0) return false;
                foreach (var action in actions)
                    if (action == null || !action.IsValid) return false;
                return true;
            }
        }
    }
}
