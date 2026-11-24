using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Roguelike.Editor
{
    public static class BattleResponsiveLayout
    {
        public const string ViewportName = "Battle Viewport";

        [MenuItem("Roguelike/Fix Battle Layout Anchors")]
        public static void Apply()
        {
            EditorSceneManager.OpenScene(BattleSceneTools.ScenePath);
            Configure(UnityEngine.Object.FindFirstObjectByType<Canvas>());
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("BATTLE_RESPONSIVE_LAYOUT_SAVED");
        }

        public static void Configure(Canvas canvas)
        {
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            var stage = canvas.transform.Find(ViewportName) as RectTransform;
            if (stage == null)
            {
                stage = new GameObject(ViewportName, typeof(RectTransform)).GetComponent<RectTransform>();
                stage.SetParent(canvas.transform, false);
            }
            Anchor(stage, new Vector2(.5f, .5f), Vector2.zero);
            stage.pivot = new Vector2(.5f, .5f);
            stage.localScale = Vector3.one;
            var fitter = stage.GetComponent<AspectRatioFitter>();
            if (fitter == null) fitter = stage.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            // In Edit Mode, AspectMode.None derives the ratio from the current rect.
            // Set the mode first so it cannot overwrite the requested 16:9 ratio.
            fitter.aspectRatio = 16f / 9f;
            // Keep existing objects, sprites and serialized callbacks. Reparent only.
            var children = new Transform[canvas.transform.childCount];
            for (int i = 0; i < children.Length; i++) children[i] = canvas.transform.GetChild(i);
            foreach (var child in children)
            {
                if (child != stage) child.SetParent(stage, false);
            }
            if (Camera.main != null) Camera.main.backgroundColor = new Color(.015f, .018f, .02f);
            var hud = Required(stage, "Reference HUD");
            Stretch(hud);
            hud.SetAsLastSibling();
            var background = Required(stage, "Dungeon Background");
            Stretch(background);
            background.SetAsFirstSibling();
            var popup = stage.Find("HUD Popup") as RectTransform;
            if (popup != null) { Stretch(popup); popup.SetAsLastSibling(); }

            // Insets include the whole count badge and its label, not just the icon.
            Place(hud, "DrawPile", Vector2.zero, new Vector2(48, 64), new Vector2(54, 70));
            Place(hud, "DiscardPile", Vector2.right, new Vector2(-48, 64), new Vector2(54, 70));
            Place(hud, "Energy", Vector2.zero, new Vector2(108, 140), new Vector2(96, 96));
            Place(hud, "EndTurn", Vector2.right, new Vector2(-136, 124), new Vector2(150, 59));
            Place(hud, "Status", new Vector2(.5f, 0), new Vector2(0, 15), new Vector2(1100, 25));
            Place(Required(hud, "DrawPile"), "Count Badge", new Vector2(.5f, .5f), new Vector2(18, -24), new Vector2(30, 28));
            Place(Required(hud, "DiscardPile"), "Count Badge", new Vector2(.5f, .5f), new Vector2(-18, -24), new Vector2(30, 28));

            var top = Required(hud, "Top Status Bar");
            top.anchorMin = new Vector2(0, 1);
            top.anchorMax = Vector2.one;
            top.pivot = new Vector2(.5f, 1);
            top.sizeDelta = new Vector2(0, 54);
            top.anchoredPosition = Vector2.zero;
            foreach (string name in new[] { "Player Name", "Character Class", "Heart", "Player Health", "Gold", "Gold Count" })
            {
                var rect = Required(top, name);
                float fromLeft = rect.anchoredPosition.x + 640;
                if (rect.anchorMin.x == 0) fromLeft = rect.anchoredPosition.x;
                Anchor(rect, new Vector2(0, .5f), new Vector2(fromLeft, 0));
            }
            int slot = 0;
            foreach (Transform child in top)
                if (child.name == "PotionSlot")
                    Anchor((RectTransform)child, new Vector2(0, .5f), new Vector2(528 + 43 * slot++, 0));
            Anchor(Required(top, "Floor"), new Vector2(.5f, .5f), new Vector2(71, 0));
            Anchor(Required(top, "Floor Count"), new Vector2(.5f, .5f), new Vector2(104, 0));
            Anchor(Required(top, "Map"), new Vector2(1, .5f), new Vector2(-147, 0));
            Anchor(Required(top, "Deck"), new Vector2(1, .5f), new Vector2(-95, 0));
            Anchor(Required(top, "Settings"), new Vector2(1, .5f), new Vector2(-39, 0));
            foreach (string name in new[] { "Upper Highlight", "Lower Edge" })
            {
                var edge = Required(top, name);
                bool upper = name == "Upper Highlight";
                edge.anchorMin = new Vector2(0, upper ? 1 : 0);
                edge.anchorMax = new Vector2(1, upper ? 1 : 0);
                edge.pivot = new Vector2(.5f, upper ? 1 : 0);
                edge.sizeDelta = new Vector2(0, 3);
                edge.anchoredPosition = Vector2.zero;
            }

            // The hero's long sword makes its image center differ from its body center.
            // Retain the user's +70 local health-bar offset under the hero's body.
            Anchor(Required(stage, "Player"), new Vector2(.20f, .48f), Vector2.zero);
            Anchor(Required(stage, "Enemy"), new Vector2(.75f, .48f), Vector2.zero);
            Anchor(Required(stage, "Enemy Intent"), new Vector2(.75f, .48f), new Vector2(0, 150));
            foreach (var component in stage.GetComponentsInChildren<Component>(true))
                if (PrefabUtility.IsPartOfPrefabInstance(component))
                    PrefabUtility.RecordPrefabInstancePropertyModifications(component);
            Canvas.ForceUpdateCanvases();
        }

        private static void Place(Transform parent, string name, Vector2 anchor, Vector2 offset, Vector2 size)
        {
            var rect = Required(parent, name);
            Anchor(rect, anchor, offset);
            rect.sizeDelta = size;
        }
        private static void Anchor(RectTransform rect, Vector2 anchor, Vector2 offset)
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.anchoredPosition = offset;
        }
        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
        private static RectTransform Required(Transform parent, string name)
        {
            var rect = parent.Find(name) as RectTransform;
            if (rect == null) throw new InvalidOperationException($"Missing layout object: {parent.name}/{name}");
            return rect;
        }
    }
}
