using System.IO;
using Roguelike.Combat;
using Roguelike.Core;
using Roguelike.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Roguelike.Editor
{
    public static class BattleHudSceneTools
    {
        private const string Root = "Assets/_Game/";
        private static Font font;
        private static readonly Color Cream = new Color(.94f, .91f, .79f);

        public static void ApplyAndCapture()
        {
            Apply();
            BattlePreviewCapture.Capture();
        }

        public static void VerifyAndCapture()
        {
            ApplyAndCapture();
            BattleSetupChecks.RunPlayMode();
        }

        [MenuItem("Roguelike/Apply Reference Battle HUD")]
        public static void Apply()
        {
            AssetDatabase.Refresh();
            font = AssetDatabase.LoadAssetAtPath<Font>(Root + "Art/Fonts/NotoSansCJKkr-Regular.otf");
            if (font == null) throw new System.InvalidOperationException("Noto Korean font is missing.");
            var hero = SpriteAsset("Ironclad", Root + "Art/Characters/Ironclad.png");
            var background = SpriteAsset("BattleBackground", Root + "Art/Backgrounds/Act1Dungeon.png", false);
            // Import everything before opening the scene; resolve assets again afterwards.
            string[] icons = { "Energy", "DrawPile", "DiscardPile", "EndTurn", "Heart", "Gold", "PotionSlot", "Floor", "Map", "Deck", "Settings" };
            foreach (string icon in icons) SpriteAsset(icon, Root + "Art/UI/Reference/" + icon + ".png");
            var character = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(Root + "Data/Characters/Ironclad.asset");
            BattleSceneTools.Set(character, "portrait", hero);
            StringValue(character, "displayName", "아이언클래드");
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(BattleSceneTools.ScenePath);
            font = AssetDatabase.LoadAssetAtPath<Font>(Root + "Art/Fonts/NotoSansCJKkr-Regular.otf");
            var canvas = Object.FindFirstObjectByType<Canvas>();
            var content = canvas.transform.Find(BattleResponsiveLayout.ViewportName) ?? canvas.transform;
            var setup = Object.FindFirstObjectByType<BattleSetup>();
            var serializedSetup = new SerializedObject(setup);
            var player = (CombatantView)serializedSetup.FindProperty("playerView").objectReferenceValue;
            var enemy = (CombatantView)serializedSetup.FindProperty("enemyView").objectReferenceValue;
            var intent = (EnemyIntentView)serializedSetup.FindProperty("intentView").objectReferenceValue;
            // Remove the previous prototype decorations, keeping combatant instances.
            foreach (string name in new[] { "Ground", "Top Bar", "Title", "Energy", "Hand Area", "Hand Placeholder", "Status", "New Encounter", "End Turn Placeholder", "Reference HUD", "Dungeon Background", "HUD Popup" })
            {
                var child = content.Find(name);
                if (child != null) Object.DestroyImmediate(child.gameObject);
            }
            var bg = Image(content, "Dungeon Background", Vector2.zero, new Vector2(1280, 720), Color.white,
                AssetDatabase.LoadAssetAtPath<Sprite>(Root + "Art/UI/Reference/BattleBackground.sprite.asset"));
            Stretch(bg.rectTransform);
            bg.transform.SetAsFirstSibling();
            StyleCombatant(player, new Vector2(-350, -82), true);
            StyleCombatant(enemy, new Vector2(350, -82), false);
            intent.GetComponent<RectTransform>().anchoredPosition = new Vector2(350, 94);
            foreach (var text in intent.GetComponentsInChildren<Text>()) { text.font = font; text.fontSize = 20; }
            var hudRoot = Rect(content, "Reference HUD", Vector2.zero, new Vector2(1280, 720));
            Stretch(hudRoot);
            var hud = hudRoot.gameObject.AddComponent<BattleHud>();
            var run = setup.GetComponent<RunState>();
            if (run == null) run = setup.gameObject.AddComponent<RunState>();
            BattleSceneTools.Set(setup, "runState", run);
            StringValue(setup, "energyFormat", "{0}/{1}");
            BoolValue(setup, "showReadyMessage", false);
            BattleSceneTools.Set(hud, "battle", setup);
            BattleSceneTools.Set(hud, "run", run);

            var bar = Image(hudRoot, "Top Status Bar", new Vector2(0, 333), new Vector2(1280, 54), new Color(.19f, .23f, .25f, .97f));
            bar.rectTransform.anchorMin = new Vector2(0, 1);
            bar.rectTransform.anchorMax = new Vector2(1, 1);
            bar.rectTransform.sizeDelta = new Vector2(0, 54);
            bar.rectTransform.anchoredPosition = new Vector2(0, -27);
            Image(bar.transform, "Upper Highlight", new Vector2(0, 25), new Vector2(1280, 3), new Color(.48f, .52f, .49f, .6f));
            Image(bar.transform, "Lower Edge", new Vector2(0, -26), new Vector2(1280, 3), new Color(.06f, .08f, .09f, .9f));
            LinkText(hud, "playerName", Label(bar.transform, "Player Name", "naminal", new Vector2(-559, 0), new Vector2(140, 45), 22));
            LinkText(hud, "characterName", Label(bar.transform, "Character Class", "아이언클래드", new Vector2(-430, 0), new Vector2(122, 45), 16));
            Icon(bar.transform, "Heart", new Vector2(-343, 0), new Vector2(29, 29));
            var hp = Label(bar.transform, "Player Health", "80/80", new Vector2(-291, 0), new Vector2(76, 40), 20);
            hp.color = new Color(1, .47f, .36f);
            LinkText(hud, "health", hp);
            Icon(bar.transform, "Gold", new Vector2(-218, 0), new Vector2(32, 32));
            var gold = Label(bar.transform, "Gold Count", "99", new Vector2(-181, 0), new Vector2(48, 40), 20);
            gold.color = new Color(1, .82f, .32f);
            LinkText(hud, "gold", gold);
            var potionList = new SerializedObject(hud);
            var potionProperty = potionList.FindProperty("potionButtons");
            potionProperty.arraySize = 3;
            for (int i = 0; i < 3; i++)
            {
                int slot = i;
                var potion = IconButton(bar.transform, "PotionSlot", new Vector2(-112 + i * 43, 0), new Vector2(32, 32), null);
                UnityEventTools.AddIntPersistentListener(potion.onClick, hud.ShowPotion, slot);
                Label(potion.transform, "Contents", "", Vector2.zero, new Vector2(28, 28), 20);
                potionProperty.GetArrayElementAtIndex(i).objectReferenceValue = potion;
            }
            potionList.ApplyModifiedPropertiesWithoutUndo();
            Icon(bar.transform, "Floor", new Vector2(71, 0), new Vector2(28, 28));
            LinkText(hud, "floor", Label(bar.transform, "Floor Count", "1", new Vector2(104, 0), new Vector2(40, 40), 19));
            IconButton(bar.transform, "Map", new Vector2(493, 0), new Vector2(35, 38), hud.ShowMap);
            var deck = IconButton(bar.transform, "Deck", new Vector2(545, 0), new Vector2(38, 38), hud.ShowDeck);
            var total = Label(deck.transform, "Total Cards", "10", new Vector2(9, -12), new Vector2(42, 27), 17);
            LinkText(hud, "deckCount", total);
            IconButton(bar.transform, "Settings", new Vector2(601, 0), new Vector2(36, 36), hud.ShowSettings);

            var energy = Icon(hudRoot, "Energy", new Vector2(-514, -226), new Vector2(100, 100));
            var energyText = Label(energy.transform, "Energy Text", "3/3", Vector2.zero, new Vector2(86, 45), 28);
            BattleSceneTools.Set(setup, "energyLabel", energyText);
            var draw = IconButton(hudRoot, "DrawPile", new Vector2(-594, -311), new Vector2(66, 82), hud.ShowDraw);
            var discard = IconButton(hudRoot, "DiscardPile", new Vector2(594, -311), new Vector2(66, 82), hud.ShowDiscard);
            CountBadge(hud, "drawCount", draw.transform, new Vector2(24, -28), "10");
            CountBadge(hud, "discardCount", discard.transform, new Vector2(-24, -28), "0");
            var end = IconButton(hudRoot, "EndTurn", new Vector2(520, -235), new Vector2(166, 65), null);
            Label(end.transform, "End Turn Label", "턴 종료", Vector2.zero, new Vector2(136, 40), 21);
            end.interactable = false;
            // Hidden status is retained for actionable initialization errors only.
            var status = Label(hudRoot, "Status", "", new Vector2(0, -347), new Vector2(1100, 25), 15);
            BattleSceneTools.Set(setup, "statusLabel", status);
            CreatePopup(content, hud);
            BattleResponsiveLayout.Configure(canvas);
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(canvas.gameObject.scene, BattleSceneTools.ScenePath);
            Debug.Log("REFERENCE_HUD_APPLIED");
        }

        private static void StyleCombatant(CombatantView view, Vector2 position, bool player)
        {
            var rect = view.GetComponent<RectTransform>();
            rect.anchoredPosition = position;
            var portrait = view.transform.Find("Portrait").GetComponent<Image>();
            portrait.rectTransform.sizeDelta = player ? new Vector2(300, 225) : new Vector2(230, 240);
            portrait.rectTransform.anchoredPosition = new Vector2(0, -5);
            if (player) portrait.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "Art/UI/Reference/Ironclad.sprite.asset");
            var name = view.transform.Find("Name").GetComponent<Text>();
            name.font = font;
            name.fontSize = 15;
            name.enabled = !player;
            name.rectTransform.anchoredPosition = new Vector2(0, -159);
            var bar = view.transform.Find("Health Background").GetComponent<Image>();
            bar.rectTransform.sizeDelta = new Vector2(160, 10);
            bar.rectTransform.anchoredPosition = new Vector2(player ? 70 : 0, -135);
            bar.color = new Color(.18f, .07f, .05f);
            var fill = bar.transform.Find("Health Fill").GetComponent<Image>();
            fill.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            fill.type = UnityEngine.UI.Image.Type.Filled;
            fill.rectTransform.sizeDelta = new Vector2(160, 10);
            fill.color = new Color(.75f, .13f, .11f);
            var text = bar.transform.Find("Health Text").GetComponent<Text>();
            text.font = font;
            text.fontSize = 15;
            text.rectTransform.sizeDelta = new Vector2(180, 26);
            foreach (var label in view.GetComponentsInChildren<Text>()) label.font = font;
            foreach (var component in view.GetComponentsInChildren<Component>(true))
                PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        }

        private static void CreatePopup(Transform parent, BattleHud hud)
        {
            var overlay = Image(parent, "HUD Popup", Vector2.zero, new Vector2(1280, 720), new Color(0, 0, 0, .72f));
            Stretch(overlay.rectTransform);
            overlay.raycastTarget = true;
            var panel = Image(overlay.transform, "Panel", Vector2.zero, new Vector2(640, 480), new Color(.13f, .17f, .18f));
            panel.raycastTarget = true;
            Image(panel.transform, "Gold Line", new Vector2(0, 177), new Vector2(540, 2), new Color(.65f, .54f, .3f));
            var title = Label(panel.transform, "Title", "", new Vector2(0, 205), new Vector2(480, 50), 27);
            var body = Label(panel.transform, "Body", "", new Vector2(0, 0), new Vector2(540, 315), 22);
            body.alignment = TextAnchor.UpperCenter;
            var close = Image(panel.transform, "Close", new Vector2(270, 205), new Vector2(45, 40), new Color(.28f, .32f, .32f));
            AddButton(close, hud.ClosePopup);
            Label(close.transform, "Label", "×", Vector2.zero, new Vector2(45, 40), 28);
            var settings = Rect(panel.transform, "Settings Controls", Vector2.zero, new Vector2(540, 300));
            var full = Rect(settings, "Fullscreen", new Vector2(-100, 90), new Vector2(250, 50));
            var toggle = full.gameObject.AddComponent<Toggle>();
            var box = Image(full, "Box", new Vector2(-80, 0), new Vector2(30, 30), new Color(.3f, .35f, .35f));
            var mark = Image(box.transform, "Mark", Vector2.zero, new Vector2(20, 20), new Color(.86f, .72f, .38f));
            toggle.targetGraphic = box;
            toggle.graphic = mark;
            box.raycastTarget = true;
            Label(full, "Label", "전체 화면", new Vector2(35, 0), new Vector2(160, 45), 22);
            UnityEventTools.AddPersistentListener(toggle.onValueChanged, hud.SetFullscreen);
            Label(settings, "Volume Label", "전체 음량", new Vector2(0, 15), new Vector2(240, 40), 22);
            var sliderRect = Rect(settings, "Volume", new Vector2(0, -40), new Vector2(380, 35));
            var track = Image(sliderRect, "Track", Vector2.zero, new Vector2(380, 8), new Color(.35f, .4f, .4f));
            track.raycastTarget = true;
            var area = Rect(sliderRect, "Handle Area", Vector2.zero, new Vector2(360, 35));
            var handle = Image(area, "Handle", Vector2.zero, new Vector2(20, 30), new Color(.85f, .72f, .43f));
            handle.raycastTarget = true;
            var slider = sliderRect.gameObject.AddComponent<Slider>();
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.minValue = 0;
            slider.maxValue = 1;
            UnityEventTools.AddPersistentListener(slider.onValueChanged, hud.SetVolume);
            Link(hud, "popup", overlay.gameObject);
            LinkText(hud, "popupTitle", title);
            LinkText(hud, "popupBody", body);
            Link(hud, "settings", settings.gameObject);
            Link(hud, "fullscreen", toggle);
            Link(hud, "volume", slider);
            settings.gameObject.SetActive(false);
            overlay.gameObject.SetActive(false);
        }

        private static Sprite SpriteAsset(string name, string png, bool trim = true)
        {
            string assetPath = Root + "Art/UI/Reference/" + name + ".sprite.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (existing != null) return existing;
            AssetDatabase.ImportAsset(png);
            var importer = AssetImporter.GetAtPath(png) as TextureImporter;
            if (importer == null) throw new FileNotFoundException("Missing artwork: " + png);
            importer.textureType = TextureImporterType.Sprite;
            importer.isReadable = true;
            importer.alphaIsTransparency = trim;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(png);
            var bounds = new Rect(0, 0, texture.width, texture.height);
            if (trim)
            {
                int x0 = texture.width, y0 = texture.height, x1 = 0, y1 = 0;
                var pixels = texture.GetPixels32();
                for (int y = 0; y < texture.height; y++)
                    for (int x = 0; x < texture.width; x++)
                        if (pixels[y * texture.width + x].a > 12)
                        { x0 = Mathf.Min(x0, x); y0 = Mathf.Min(y0, y); x1 = Mathf.Max(x1, x); y1 = Mathf.Max(y1, y); }
                if (x0 <= x1 && y0 <= y1) bounds = new Rect(x0, y0, x1 - x0 + 1, y1 - y0 + 1);
            }
            var sprite = Sprite.Create(texture, bounds, new Vector2(.5f, .5f), 100, 0, SpriteMeshType.FullRect);
            sprite.name = name;
            AssetDatabase.CreateAsset(sprite, assetPath);
            return sprite;
        }
        private static Image Icon(Transform parent, string key, Vector2 pos, Vector2 size)
        {
            var image = Image(parent, key, pos, size, Color.white, AssetDatabase.LoadAssetAtPath<Sprite>(Root + "Art/UI/Reference/" + key + ".sprite.asset"));
            image.preserveAspect = true;
            return image;
        }
        private static Button IconButton(Transform parent, string key, Vector2 pos, Vector2 size, UnityAction callback)
        {
            var icon = Icon(parent, key, pos, size);
            return AddButton(icon, callback);
        }
        private static Button AddButton(Image image, UnityAction callback)
        {
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.highlightedColor = new Color(1.15f, 1.12f, 1.03f);
            colors.disabledColor = new Color(.82f, .82f, .82f, 1);
            button.colors = colors;
            if (callback != null) UnityEventTools.AddPersistentListener(button.onClick, callback);
            return button;
        }
        private static void CountBadge(BattleHud hud, string field, Transform parent, Vector2 pos, string value)
        {
            var badge = Image(parent, "Count Badge", pos, new Vector2(32, 28), new Color(.55f, .2f, .13f));
            badge.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            var label = Label(badge.transform, "Count", value, Vector2.zero, new Vector2(40, 30), 19);
            LinkText(hud, field, label);
        }
        private static RectTransform Rect(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = pos;
            return rect;
        }
        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
        private static Image Image(Transform parent, string name, Vector2 pos, Vector2 size, Color color, Sprite sprite = null)
        {
            var image = Rect(parent, name, pos, size).gameObject.AddComponent<Image>();
            image.color = color;
            image.sprite = sprite;
            image.raycastTarget = false;
            return image;
        }
        private static Text Label(Transform parent, string name, string value, Vector2 pos, Vector2 size, int sizeFont)
        {
            var label = Rect(parent, name, pos, size).gameObject.AddComponent<Text>();
            label.font = font;
            label.fontSize = sizeFont;
            label.text = value;
            label.color = Cream;
            label.alignment = TextAnchor.MiddleCenter;
            label.raycastTarget = false;
            var shadow = label.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, .8f);
            shadow.effectDistance = new Vector2(1, -1);
            return label;
        }
        private static void LinkText(Object target, string property, Text text) => Link(target, property, text);
        private static void Link(Object target, string property, Object value) => BattleSceneTools.Set(target, property, value);
        private static void StringValue(Object target, string name, string value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(name).stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void BoolValue(Object target, string name, bool value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(name).boolValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
