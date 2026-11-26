using System;
using UnityEngine;

namespace Roguelike.Cards
{
    public enum CardEffectType { Damage, Block, Vulnerable }

    [Serializable]
    public sealed class CardEffect
    {
        [SerializeField] private CardEffectType type;
        [SerializeField, Min(0)] private int amount;

        public CardEffectType Type => type;
        public int Amount => amount;
    }

    [CreateAssetMenu(menuName = "Roguelike/Card")]
    public sealed class CardDefinition : ScriptableObject
    {
        [SerializeField] private string cardId;
        [SerializeField] private string displayName;
        [SerializeField, Min(0)] private int cost;
        [SerializeField, TextArea] private string description;
        [SerializeField] private Sprite image;
        [SerializeField] private CardEffect[] effects = Array.Empty<CardEffect>();
        [SerializeField, TextArea] private string hoverHelp;

        public string Id => cardId;
        public string DisplayName => displayName;
        public int Cost => cost;
        public string Description => description;
        public Sprite Image => image;
        public int EffectCount => effects.Length;
        public CardEffect GetEffect(int index) => effects[index];
        public string HoverHelp => hoverHelp;
    }
}
