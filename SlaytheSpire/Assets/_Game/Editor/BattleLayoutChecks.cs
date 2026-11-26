using System;
using System.Collections.Generic;
using System.Linq;
using Roguelike.Combat;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;
using Roguelike.UI;

namespace Roguelike.Editor
{
    public static class BattleLayoutChecks
    {
        public static void ApplyCaptureAndVerify()
        {
            BattleResponsiveLayout.Apply();
            CaptureAndVerify();
        }

        public static void CaptureAndVerify()
        {
            var sizes = new[]
            {
                new Vector2Int(1920, 1080), new Vector2Int(1280, 720),
                new Vector2Int(1920, 1200), new Vector2Int(2560, 1080),
                new Vector2Int(1024, 768), new Vector2Int(800, 450),
                new Vector2Int(480, 800)
            };
            foreach (var size in sizes)
            {
                BattlePreviewCapture.CaptureAtSize(size.x, size.y,
                    $"BattleHand-{size.x}x{size.y}.png", Validate);
                Debug.Log($"BATTLE_LAYOUT_PASSED {size.x}x{size.y}");
            }
            BattlePreviewCapture.CaptureAtSize(1280, 720, "BattleHand-Overview.png");
            BattlePreviewCapture.CaptureAtSize(1280, 720, "BattleHand-TenCards.png",
                _ => UnityEngine.Object.FindFirstObjectByType<BattleSetup>().Piles.Draw(5));
            BattleSetupChecks.RunPlayMode();
        }

        private static void Validate(Canvas canvas)
        {
            var stage = (RectTransform)canvas.transform.Find(BattleResponsiveLayout.ViewportName);
            Require(stage != null, "Missing battle viewport.");
            Require(Mathf.Abs(stage.rect.width - 1280) < 1 && Mathf.Abs(stage.rect.height - 720) < 1,
                $"Unexpected stage dimensions: {stage.rect.size}");
            AssertInside((RectTransform)canvas.transform, stage, 0);
            var hud = stage.Find("Reference HUD");
            foreach (string name in new[] { "DrawPile", "DiscardPile", "Energy", "EndTurn" })
            {
                var element = (RectTransform)hud.Find(name);
                AssertInside(stage, element, 16);
                // Include badge labels and borders, which caused the original clipping.
                foreach (var graphic in element.GetComponentsInChildren<Graphic>())
                    if (graphic.isActiveAndEnabled) AssertInside(stage, graphic.rectTransform, 16);
            }
            var top = (RectTransform)hud.Find("Top Status Bar");
            foreach (Transform child in top)
                if (child.name != "Upper Highlight" && child.name != "Lower Edge")
                    AssertInside(stage, (RectTransform)child, 8, 0);
            AssertInside(stage, (RectTransform)stage.Find("Player"), 8);
            AssertInside(stage, (RectTransform)stage.Find("Enemy"), 8);
            AssertIntentAlignment(stage.GetComponentInChildren<EnemyIntentView>(true));
            Require(((RectTransform)hud.Find("DrawPile")).anchorMin == Vector2.zero,
                "Draw pile must use a bottom-left anchor.");
            Require(((RectTransform)hud.Find("DiscardPile")).anchorMin == Vector2.right,
                "Discard pile must use a bottom-right anchor.");
            ValidateHand(stage, hud);
        }

        private static void ValidateHand(RectTransform stage, Transform hud)
        {
            var hand = (RectTransform)hud.Find("Hand Area");
            var piles = UnityEngine.Object.FindFirstObjectByType<BattleSetup>().Piles;
            var preview = hand.Find("Card Preview").GetComponent<Image>();
            var help = hand.Find("Card Keyword Help").GetComponent<Image>();
            var pointer = new PointerEventData(EventSystem.current);

            var fiveCards = ValidateHandFan(stage, hand, 5);
            foreach (var slot in fiveCards)
            {
                slot.OnPointerEnter(pointer);
                Require(slot.GetComponent<CanvasGroup>().alpha == 0
                    && fiveCards.Count(card => card.GetComponent<CanvasGroup>().alpha == 0) == 1,
                    "Only the lifted card should be hidden in the hand.");
                Require(preview.isActiveAndEnabled && preview.sprite == slot.GetComponent<Image>().sprite
                    && preview.rectTransform.rect.height > ((RectTransform)slot.transform).rect.height,
                    "Hover did not enlarge the correct card.");
                AssertInside(stage, preview.rectTransform, 16);
                Require(!preview.raycastTarget && !help.raycastTarget, "Hover UI must not block pointer events.");
                slot.OnPointerExit(pointer);
                Require(!preview.gameObject.activeSelf && !help.gameObject.activeSelf, "Pointer exit did not hide hover UI.");
                Require(slot.GetComponent<CanvasGroup>().alpha == 1, "The lifted card did not return to the hand.");
            }

            float Spacing(EventTrigger[] slots) => ((RectTransform)slots[1].transform).anchoredPosition.x
                - ((RectTransform)slots[0].transform).anchoredPosition.x;
            float fiveCardSpacing = Spacing(fiveCards);
            piles.Draw(5);
            var tenCards = ValidateHandFan(stage, hand, 10);
            Require(Spacing(tenCards) < fiveCardSpacing, "Large hands must compress their card spacing.");

            piles.DiscardHand();
            ValidateHandFan(stage, hand, 0);
            piles.Draw(1);
            ValidateHandFan(stage, hand, 1);
            piles.Draw(1);
            ValidateHandFan(stage, hand, 2);
            piles.Draw(3);

            // Show the opposite half of the deck when the hand has no Bash.
            if (!piles.Hand.Contains("bash"))
            {
                piles.DiscardHand();
                piles.Draw(5);
            }
            for (int i = 0; i < piles.Hand.Count; i++)
            {
                if (piles.Hand[i] == "bash") continue;
                piles.DiscardFromHand(i);
                break;
            }
            ValidateHandFan(stage, hand, 4);
            piles.Draw(1);
            var slots = ValidateHandFan(stage, hand, 5);
            int bashIndex = piles.Hand.ToList().IndexOf("bash");
            slots[bashIndex].OnPointerEnter(pointer);
            var helpText = help.GetComponentInChildren<Text>();
            Require(help.isActiveAndEnabled && helpText.text.Contains("취약") && helpText.text.Contains("50%"),
                "Bash hover explanation is missing.");
            Require(helpText.preferredHeight <= helpText.rectTransform.rect.height,
                "Bash hover explanation is clipped.");
            AssertInside(stage, preview.rectTransform, 16);
            AssertInside(stage, help.rectTransform, 16);
        }

        private static EventTrigger[] ValidateHandFan(RectTransform stage, RectTransform hand, int count)
        {
            var slots = hand.GetComponentsInChildren<EventTrigger>();
            Require(slots.Length == count, $"Expected {count} visible hand cards.");
            if (count == 0) return slots;

            for (int i = 0; i < count; i++)
            {
                var slot = slots[i];
                var rect = (RectTransform)slot.transform;
                AssertInside(stage, rect, 16);
                Require(slot.GetComponent<Image>().sprite != null, "Starter card artwork is missing.");
                var mirror = (RectTransform)slots[count - 1 - i].transform;
                Require(Mathf.Abs(rect.anchoredPosition.x + mirror.anchoredPosition.x) < .5f
                    && Mathf.Abs(rect.anchoredPosition.y - mirror.anchoredPosition.y) < .5f
                    && Mathf.Abs(Mathf.DeltaAngle(rect.localEulerAngles.z, -mirror.localEulerAngles.z)) < .5f,
                    "The hand fan must be symmetric around the center.");

                Vector3 exposedPoint = ExposedCardPoint(rect);
                foreach (var foreground in slots.Skip(i + 1))
                {
                    var foregroundRect = (RectTransform)foreground.transform;
                    Require(!foregroundRect.rect.Contains(foregroundRect.InverseTransformPoint(exposedPoint)),
                        $"Card {i + 1} of {count} is covered by {foreground.name}.");
                }
            }

            if (count > 1)
            {
                var left = (RectTransform)slots[0].transform;
                var next = (RectTransform)slots[1].transform;
                var middle = (RectTransform)slots[count / 2].transform;
                Require(Mathf.DeltaAngle(0, left.localEulerAngles.z) > 0, "Outer cards must tilt outwards.");
                Require(next.anchoredPosition.x - left.anchoredPosition.x < left.rect.width, "Adjacent cards must overlap.");
                if (count > 2)
                    Require(middle.anchoredPosition.y > left.anchoredPosition.y, "The middle of the fan must sit higher.");
            }
            return slots;
        }

        public static void AssertHandInput(Transform hand)
        {
            Canvas.ForceUpdateCanvases();
            var canvas = hand.GetComponentInParent<Canvas>();
            var raycaster = canvas.GetComponent<GraphicRaycaster>();
            var pointer = new PointerEventData(EventSystem.current);
            var hits = new List<RaycastResult>();
            foreach (var slot in hand.GetComponentsInChildren<EventTrigger>())
            {
                var point = ExposedCardPoint((RectTransform)slot.transform);
                pointer.position = RectTransformUtility.WorldToScreenPoint(raycaster.eventCamera, point);
                hits.Clear();
                raycaster.Raycast(pointer, hits);
                Require(hits.Count > 0 && hits[0].gameObject == slot.gameObject,
                    $"Canvas raycast missed {slot.name} at {pointer.position}. First hit: {hits.FirstOrDefault().gameObject?.name ?? "none"}.");
                slot.OnPointerEnter(pointer);
                Canvas.ForceUpdateCanvases();
                hits.Clear();
                raycaster.Raycast(pointer, hits);
                Require(slot.GetComponent<CanvasGroup>().alpha == 0 && hits.Count > 0 && hits[0].gameObject == slot.gameObject,
                    $"Lifting {slot.name} lost its hover target.");
                slot.OnPointerExit(pointer);
                Require(slot.GetComponent<CanvasGroup>().alpha == 1, $"Leaving {slot.name} did not restore it.");
            }
        }

        private static Vector3 ExposedCardPoint(RectTransform rect) =>
            rect.TransformPoint(new Vector3(rect.rect.xMin * .72f, rect.rect.yMax * .68f));

        public static void AssertIntentAlignment(EnemyIntentView intent)
        {
            Require(intent != null && intent.transform.parent.GetComponent<CombatantView>() != null,
                "Intent must belong to its enemy, not the battle viewport.");
            var enemy = intent.transform.parent.GetComponent<CombatantView>();
            var label = intent.GetComponent<Text>();
            Require(label.isActiveAndEnabled && !string.IsNullOrEmpty(label.text), "Intent text is hidden.");
            label.cachedTextGenerator.Populate(label.text, label.GetGenerationSettings(label.rectTransform.rect.size));
            Require(label.cachedTextGenerator.vertexCount > 4, "Intent text has no visible glyph geometry.");
            Require(enemy.TryGetHeadPosition(out Vector3 head), "Enemy portrait has no visible head position.");
            var corners = new Vector3[4];
            ((RectTransform)intent.transform).GetWorldCorners(corners);
            Vector3 localBottom = enemy.transform.InverseTransformPoint((corners[0] + corners[3]) * .5f);
            Vector3 localHead = enemy.transform.InverseTransformPoint(head);
            float gap = localBottom.y - localHead.y;
            Require(Mathf.Abs(localBottom.x - localHead.x) < .5f,
                "Intent is not horizontally centered above its enemy.");
            Require(Mathf.Abs(gap - intent.HeadGap) < .5f && gap >= 6 && gap <= 16,
                $"Intent must have a small gap immediately above the head, got {gap}.");
        }

        private static void AssertInside(RectTransform parent, RectTransform element, float margin, float verticalMargin = -1)
        {
            if (verticalMargin < 0) verticalMargin = margin;
            var corners = new Vector3[4];
            element.GetWorldCorners(corners);
            foreach (var world in corners)
            {
                Vector3 local = parent.InverseTransformPoint(world);
                Rect rect = parent.rect;
                Require(local.x >= rect.xMin + margin - .5f && local.x <= rect.xMax - margin + .5f
                    && local.y >= rect.yMin + verticalMargin - .5f && local.y <= rect.yMax - verticalMargin + .5f,
                    $"{element.name} outside {parent.name} or its {margin}px safe inset: {local}, parent={rect}");
            }
        }
        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
    }
}
