using Roguelike.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace Roguelike.UI
{
    public sealed class EnemyIntentView : MonoBehaviour
    {
        [SerializeField] private Text label;
        [SerializeField] private CombatantView target;
        [SerializeField, Min(0)] private float headGap = 10;
        public float HeadGap => headGap;

        public void BindTarget(CombatantView enemy)
        {
            target = enemy;
            if (target == null) return;
            var rect = (RectTransform)transform;
            rect.SetParent(target.transform, false);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, 0);
            rect.sizeDelta = new Vector2(220, 40);
            rect.localScale = Vector3.one;
            label.alignment = TextAnchor.LowerCenter;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            RefreshPosition();
        }

        public void RefreshPosition()
        {
            if (target == null || label == null) return;
            bool visible = target.TryGetHeadPosition(out Vector3 head);
            label.enabled = visible && !string.IsNullOrEmpty(label.text);
            if (visible)
                transform.localPosition = target.transform.InverseTransformPoint(head) + Vector3.up * headGap;
        }

        private void LateUpdate() => RefreshPosition();
        public void Show(EnemyAction action)
        {
            label.text = $"{action.Type.ToString().ToUpperInvariant()}  {action.Amount}";
            label.color = action.Type == EnemyActionType.Attack
                ? new Color(1f, .66f, .44f) : new Color(.5f, .8f, 1f);
            RefreshPosition();
        }
        public void Clear() { label.text = ""; RefreshPosition(); }
    }
}
