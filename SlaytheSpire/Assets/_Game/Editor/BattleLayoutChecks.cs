using System;
using UnityEngine;
using UnityEngine.UI;

namespace Roguelike.Editor
{
    public static class BattleLayoutChecks
    {
        public static void ApplyCaptureAndVerify()
        {
            BattleResponsiveLayout.Apply();
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
                    $"BattleLayout-{size.x}x{size.y}.png", Validate);
                Debug.Log($"BATTLE_LAYOUT_PASSED {size.x}x{size.y}");
            }
            // Also exercise the unchanged HUD data and button behavior in Play Mode.
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
            Require(((RectTransform)hud.Find("DrawPile")).anchorMin == Vector2.zero,
                "Draw pile must use a bottom-left anchor.");
            Require(((RectTransform)hud.Find("DiscardPile")).anchorMin == Vector2.right,
                "Discard pile must use a bottom-right anchor.");
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
