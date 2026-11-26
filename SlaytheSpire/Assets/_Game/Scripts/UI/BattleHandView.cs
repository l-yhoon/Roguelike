using System;
using System.Collections.Generic;
using Roguelike.Cards;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Roguelike.UI
{
    internal sealed class BattleHandView
    {
        private static readonly Vector2 BottomCenter = new Vector2(.5f, 0);
        private static readonly Vector2 CardSize = new Vector2(126, 164);
        private static readonly Color PanelColor = new Color(.15f, .12f, .10f, .98f);
        private static readonly Vector2 PreviewSize = new Vector2(244, 320);
        private readonly RectTransform root;
        private readonly Font font;
        private readonly Func<string, CardDefinition> findCard;
        private readonly List<Image> slots = new List<Image>();
        private readonly Image preview;
        private readonly Text previewText;
        private readonly Image helpPanel;
        private readonly Text helpText;
        private IReadOnlyList<string> cards = Array.Empty<string>();
        private CanvasGroup liftedSlot;

        public BattleHandView(Transform parent, Font font, Func<string, CardDefinition> findCard)
        {
            this.font = font;
            this.findCard = findCard;
            root = CreateRect(parent, "Hand Area", Vector2.zero, Vector2.zero);
            preview = CreateImage(root, "Card Preview", Vector2.zero, PreviewSize);
            previewText = CreateLabel(preview.rectTransform, "Card Details", new Vector2(210, 275), 23);
            helpPanel = CreateImage(root, "Card Keyword Help", Vector2.zero, new Vector2(224, 114));
            helpPanel.color = PanelColor;
            helpText = CreateLabel(helpPanel.rectTransform, "Keyword Description", new Vector2(196, 92), 20);
            HidePreview();
        }

        public void Render(IReadOnlyList<string> hand)
        {
            HidePreview();
            cards = hand;
            while (slots.Count < cards.Count) AddSlot();

            // Keep the cards readable as the hand widens by compressing their spacing.
            float halfCount = Mathf.Max(0, (cards.Count - 1) * .5f);
            float step = Mathf.Min(88, 250 / Mathf.Max(1, halfCount));
            float edgeTilt = Mathf.Min(15, halfCount * 6);
            float edgeDrop = Mathf.Min(24, halfCount * 9);
            for (int i = 0; i < slots.Count; i++)
            {
                var slot = slots[i];
                bool visible = i < cards.Count;
                slot.gameObject.SetActive(visible);
                if (!visible) continue;

                float offset = i - halfCount;
                float arcPosition = halfCount > 0 ? offset / halfCount : 0;
                var rect = slot.rectTransform;
                rect.anchoredPosition = new Vector2(offset * step, 138 - edgeDrop * arcPosition * arcPosition);
                rect.localRotation = Quaternion.Euler(0, 0, -edgeTilt * arcPosition);
                var card = findCard(cards[i]);
                slot.sprite = card?.Image;
                slot.color = slot.sprite != null ? Color.white : PanelColor;
                var label = slot.GetComponentInChildren<Text>(true);
                label.text = card?.DisplayName ?? cards[i];
                label.enabled = slot.sprite == null;
            }
        }

        public void HidePreview()
        {
            if (liftedSlot != null)
            {
                liftedSlot.alpha = 1;
                liftedSlot = null;
            }
            preview.gameObject.SetActive(false);
            helpPanel.gameObject.SetActive(false);
        }

        private void AddSlot()
        {
            int index = slots.Count;
            var slot = CreateImage(root, $"Hand Card {index + 1}", Vector2.zero, CardSize);
            slot.rectTransform.pivot = new Vector2(.5f, .5f);
            slot.transform.SetSiblingIndex(index);
            slot.raycastTarget = true;
            // Keep the original hit area under the pointer while its artwork is lifted.
            slot.canvasRenderer.cullTransparentMesh = false;
            var visibility = slot.gameObject.AddComponent<CanvasGroup>();
            var label = CreateLabel(slot.rectTransform, "Card Name", CardSize - new Vector2(4, 4), 18);
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 1;
            label.resizeTextMaxSize = 18;

            var trigger = slot.gameObject.AddComponent<EventTrigger>();
            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => ShowPreview(index));
            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ =>
            {
                if (liftedSlot == visibility) HidePreview();
            });
            trigger.triggers.Add(enter);
            trigger.triggers.Add(exit);
            slots.Add(slot);
        }

        private void ShowPreview(int index)
        {
            if (index >= cards.Count) return;

            HidePreview();
            liftedSlot = slots[index].GetComponent<CanvasGroup>();
            liftedSlot.alpha = 0;
            var card = findCard(cards[index]);
            float x = slots[index].rectTransform.anchoredPosition.x;
            preview.rectTransform.anchoredPosition = new Vector2(x, 228);
            preview.sprite = card?.Image;
            preview.color = preview.sprite != null ? Color.white : PanelColor;
            previewText.enabled = preview.sprite == null;
            previewText.text = card == null ? cards[index] : $"{card.Cost}  {card.DisplayName}\n\n{card.Description}";
            preview.gameObject.SetActive(true);

            bool showHelp = !string.IsNullOrWhiteSpace(card?.HoverHelp);
            helpPanel.gameObject.SetActive(showHelp);
            if (!showHelp) return;

            float offset = PreviewSize.x * .5f + helpPanel.rectTransform.sizeDelta.x * .5f + 12;
            helpPanel.rectTransform.anchoredPosition = new Vector2(x + (x > 0 ? -offset : offset), 418);
            helpText.text = card.HoverHelp;
        }

        private static RectTransform CreateRect(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = BottomCenter;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        private static Image CreateImage(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var image = CreateRect(parent, name, position, size).gameObject.AddComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        private Text CreateLabel(RectTransform parent, string name, Vector2 size, int fontSize)
        {
            var rect = CreateRect(parent, name, Vector2.zero, size);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero;
            var label = rect.gameObject.AddComponent<Text>();
            label.font = font;
            label.fontSize = fontSize;
            label.color = new Color(.98f, .9f, .72f);
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            return label;
        }
    }
}
