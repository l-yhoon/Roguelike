using UnityEngine;

namespace Roguelike.Combat
{
    [CreateAssetMenu(menuName = "Roguelike/Character")]
    public sealed class CharacterDefinition : ScriptableObject
    {
        [SerializeField] private string characterId = "ironclad";
        [SerializeField] private string displayName = "Ironclad";
        [SerializeField] private Sprite portrait;
        [SerializeField, Min(1)] private int maxHealth = 80;
        [SerializeField, Min(0)] private int baseEnergy = 3;

        public string Id => characterId;
        public string DisplayName => displayName;
        public Sprite Portrait => portrait;
        public int MaxHealth => maxHealth;
        public int BaseEnergy => baseEnergy;
        public bool IsValid => !string.IsNullOrWhiteSpace(characterId)
            && !string.IsNullOrWhiteSpace(displayName) && maxHealth > 0 && baseEnergy >= 0;
    }
}
