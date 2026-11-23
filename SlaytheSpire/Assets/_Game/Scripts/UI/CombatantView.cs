using Roguelike.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace Roguelike.UI
{
    public sealed class CombatantView : MonoBehaviour
    {
        [SerializeField] private Image portrait;
        [SerializeField] private Text nameLabel;
        [SerializeField] private Text healthLabel;
        [SerializeField] private Image healthFill;
        [SerializeField] private Text blockLabel;
        private CombatantState state;

        public void Bind(string displayName, Sprite sprite, CombatantState nextState)
        {
            Unbind();
            state = nextState;
            nameLabel.text = displayName;
            portrait.sprite = sprite;
            portrait.enabled = sprite != null;
            state.Changed += Refresh;
            Refresh();
        }

        private void Refresh()
        {
            healthLabel.text = $"{state.CurrentHealth} / {state.MaxHealth}";
            healthFill.fillAmount = (float)state.CurrentHealth / state.MaxHealth;
            blockLabel.text = state.Block > 0 ? $"BLOCK {state.Block}" : "";
        }

        public void Unbind()
        {
            if (state != null) state.Changed -= Refresh;
            state = null;
        }

        private void OnDestroy() => Unbind();
    }
}
