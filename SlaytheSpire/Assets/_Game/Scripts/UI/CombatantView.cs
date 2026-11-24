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
        private Sprite boundsSprite;
        private Vector2 visibleMin;
        private Vector2 visibleMax;

        // Use the sprite mesh bounds, not the top of the Image's padded rectangle.
        // Tight sprites exclude transparent padding; trimmed full-rect sprites also work.
        public bool TryGetHeadPosition(out Vector3 worldPosition)
        {
            worldPosition = default;
            if (portrait == null || !portrait.isActiveAndEnabled || portrait.sprite == null) return false;
            Sprite sprite = portrait.sprite;
            if (boundsSprite != sprite)
            {
                boundsSprite = sprite;
                visibleMin = Vector2.one;
                visibleMax = Vector2.zero;
                Vector2 size = sprite.rect.size;
                foreach (Vector2 vertex in sprite.vertices)
                {
                    Vector2 pixel = vertex * sprite.pixelsPerUnit + sprite.pivot;
                    Vector2 normalized = new Vector2(pixel.x / size.x, pixel.y / size.y);
                    visibleMin = Vector2.Min(visibleMin, normalized);
                    visibleMax = Vector2.Max(visibleMax, normalized);
                }
            }
            RectTransform rectTransform = portrait.rectTransform;
            Rect rect = rectTransform.rect;
            Vector2 drawn = rect.size;
            if (portrait.preserveAspect)
            {
                float fit = Mathf.Min(rect.width / sprite.rect.width, rect.height / sprite.rect.height);
                drawn = sprite.rect.size * fit;
            }
            Vector2 origin = rect.min + Vector2.Scale(rect.size - drawn, rectTransform.pivot);
            Vector2 head = origin + Vector2.Scale(drawn,
                new Vector2((visibleMin.x + visibleMax.x) * .5f, visibleMax.y));
            worldPosition = rectTransform.TransformPoint(head);
            return true;
        }

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
