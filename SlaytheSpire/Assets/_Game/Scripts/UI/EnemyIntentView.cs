using Roguelike.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace Roguelike.UI
{
    public sealed class EnemyIntentView : MonoBehaviour
    {
        [SerializeField] private Text label;
        public void Show(EnemyAction action)
        {
            label.text = $"{action.Type.ToString().ToUpperInvariant()}  {action.Amount}";
            label.color = action.Type == EnemyActionType.Attack
                ? new Color(1f, .66f, .44f) : new Color(.5f, .8f, 1f);
        }
        public void Clear() => label.text = "";
    }
}
