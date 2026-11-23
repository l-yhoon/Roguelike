using System.Collections.Generic;
using UnityEngine;

namespace Roguelike.Combat
{
    [CreateAssetMenu(menuName = "Roguelike/Enemy Pool")]
    public sealed class EnemyPool : ScriptableObject
    {
        [SerializeField] private EnemyDefinition[] candidates = new EnemyDefinition[0];

        public bool TryPick(out EnemyDefinition enemy, System.Random random = null)
        {
            // Ignore missing/invalid entries and duplicate references. Each distinct
            // valid enemy has equal probability, including a pool with one entry.
            var valid = new List<EnemyDefinition>();
            if (candidates != null)
                foreach (var candidate in candidates)
                    if (candidate != null && candidate.IsValid && !valid.Contains(candidate))
                        valid.Add(candidate);

            enemy = null;
            if (valid.Count == 0) return false;
            int index = random == null ? Random.Range(0, valid.Count) : random.Next(valid.Count);
            enemy = valid[index];
            return true;
        }
    }
}
