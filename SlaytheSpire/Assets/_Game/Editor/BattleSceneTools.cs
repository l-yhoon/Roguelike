using System;
using System.IO;
using Roguelike.Combat;
using Roguelike.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Roguelike.Editor
{
    public static class BattleSceneTools
    {
        private const string Root = "Assets/_Game/";
        public const string ScenePath = Root + "Scenes/Battle.unity";
        private static Font Font => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        [MenuItem("Roguelike/Create Initial Battle Assets")]
        public static void Generate()
        {
            if (File.Exists(ScenePath))
                throw new InvalidOperationException("Battle scene already exists; generation will not overwrite it.");
            Directory.CreateDirectory(Root + "Data/Characters");
            var playerSprite = CreatePortrait("IroncladPlaceholder", true);
            var enemySprite = CreatePortrait("SlimePlaceholder", false);
            var character = ScriptableObject.CreateInstance<CharacterDefinition>();
            Set(character, "portrait", playerSprite);
            AssetDatabase.CreateAsset(character, Root + "Data/Characters/Ironclad.asset");
            var enemy = ScriptableObject.CreateInstance<EnemyDefinition>();
            Set(enemy, "portrait", enemySprite);
            AssetDatabase.CreateAsset(enemy, Root + "Data/Enemies/TrainingSlime.asset");
            var pool = ScriptableObject.CreateInstance<EnemyPool>();
            SetCandidates(pool, enemy);
            AssetDatabase.CreateAsset(pool, Root + "Data/Enemies/Act1NormalEnemyPool.asset");

            var playerPrefab = CreateCombatantPrefab("Player", character.DisplayName, playerSprite, character.MaxHealth);
            var enemyPrefab = CreateCombatantPrefab("Enemy", enemy.DisplayName, enemySprite, enemy.MaxHealth);
            AssetDatabase.SaveAssets();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            // NewScene may unload assets not referenced by a scene. Resolve persistent
            // assets again instead of retaining stale native-object references.
            character = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(Root + "Data/Characters/Ironclad.asset");
            pool = AssetDatabase.LoadAssetAtPath<EnemyPool>(Root + "Data/Enemies/Act1NormalEnemyPool.asset");
            var camera = new GameObject("Main Camera", typeof(Camera));
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0, 0, -10);
            var cam = camera.GetComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(.055f, .09f, .075f);

            var canvas = new GameObject("Battle Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = .5f;
            Panel(canvas.transform, "Ground", new Vector2(0, -80), new Vector2(1280, 320), new Color(.10f, .15f, .12f));
            Panel(canvas.transform, "Top Bar", new Vector2(0, 328), new Vector2(1280, 64), new Color(.035f, .05f, .05f));
            Label(canvas.transform, "Title", "ACT 1  /  BATTLE PROTOTYPE", new Vector2(-370, 328), new Vector2(470, 40), 23);
            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, canvas.transform);
            player.GetComponent<RectTransform>().anchoredPosition = new Vector2(-330, 0);
            var foe = (GameObject)PrefabUtility.InstantiatePrefab(enemyPrefab, canvas.transform);
            foe.GetComponent<RectTransform>().anchoredPosition = new Vector2(330, 0);
            var intentLabel = Label(canvas.transform, "Enemy Intent", "ATTACK  6", new Vector2(330, 200), new Vector2(300, 50), 26);
            var intent = intentLabel.gameObject.AddComponent<EnemyIntentView>();
            Set(intent, "label", intentLabel);
            var energy = Label(canvas.transform, "Energy", "ENERGY  3 / 3", new Vector2(-480, -260), new Vector2(270, 60), 26);
            energy.color = new Color(1f, .77f, .35f);
            Panel(canvas.transform, "Hand Area", new Vector2(0, -265), new Vector2(610, 125), new Color(.06f, .09f, .08f));
            Label(canvas.transform, "Hand Placeholder", "HAND AREA\nCards will be connected in the next phase", new Vector2(0, -265), new Vector2(590, 100), 21);
            var status = Label(canvas.transform, "Status", "Ready", new Vector2(0, -345), new Vector2(1230, 28), 17);
            var setup = new GameObject("Battle Setup").AddComponent<BattleSetup>();
            Set(setup, "character", character);
            Set(setup, "enemyPool", pool);
            Set(setup, "playerView", player.GetComponent<CombatantView>());
            Set(setup, "enemyView", foe.GetComponent<CombatantView>());
            Set(setup, "intentView", intent);
            Set(setup, "energyLabel", energy);
            Set(setup, "statusLabel", status);

            var reset = Panel(canvas.transform, "New Encounter", new Vector2(475, 328), new Vector2(230, 42), new Color(.24f, .32f, .25f));
            var button = reset.gameObject.AddComponent<Button>();
            reset.raycastTarget = true;
            button.targetGraphic = reset;
            UnityEventTools.AddPersistentListener(button.onClick, setup.InitializeBattle);
            Label(reset.transform, "Label", "NEW ENCOUNTER", Vector2.zero, new Vector2(220, 40), 20);
            var end = Panel(canvas.transform, "End Turn Placeholder", new Vector2(485, -260), new Vector2(230, 70), new Color(.16f, .21f, .20f));
            Label(end.transform, "Label", "END TURN\nComing next", Vector2.zero, new Vector2(220, 65), 20);
            var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("Battle assets generated: " + ScenePath);
        }

        private static GameObject CreateCombatantPrefab(string name, string displayName, Sprite sprite, int health)
        {
            var go = Rect(null, name, Vector2.zero, new Vector2(300, 350)).gameObject;
            var portrait = Panel(go.transform, "Portrait", new Vector2(0, 35), new Vector2(230, 270), Color.white);
            portrait.sprite = sprite;
            portrait.preserveAspect = true;
            var nameLabel = Label(go.transform, "Name", displayName, new Vector2(0, -138), new Vector2(300, 32), 24);
            var bar = Panel(go.transform, "Health Background", new Vector2(0, -102), new Vector2(220, 22), new Color(.18f, .06f, .06f));
            var fill = Panel(bar.transform, "Health Fill", Vector2.zero, new Vector2(220, 22), new Color(.78f, .22f, .19f));
            // A built-in white sprite lets Image render a proper filled health bar.
            fill.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            var hp = Label(bar.transform, "Health Text", $"{health} / {health}", Vector2.zero, new Vector2(220, 26), 18);
            var block = Label(go.transform, "Block", "", new Vector2(0, -171), new Vector2(300, 26), 20);
            var view = go.AddComponent<CombatantView>();
            Set(view, "portrait", portrait);
            Set(view, "nameLabel", nameLabel);
            Set(view, "healthLabel", hp);
            Set(view, "healthFill", fill);
            Set(view, "blockLabel", block);
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, Root + "Prefabs/Combat/" + name + ".prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        private static RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }
        private static Image Panel(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
        {
            var image = Rect(parent, name, pos, size).gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }
        private static Text Label(Transform parent, string name, string value, Vector2 pos, Vector2 size, int fontSize)
        {
            var text = Rect(parent, name, pos, size).gameObject.AddComponent<Text>();
            text.font = Font;
            text.fontSize = fontSize;
            text.text = value;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = new Color(.91f, .88f, .76f);
            text.raycastTarget = false;
            return text;
        }
        public static void Set(Object target, string property, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(property).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        public static void SetCandidates(EnemyPool pool, params EnemyDefinition[] entries)
        {
            var serialized = new SerializedObject(pool);
            var list = serialized.FindProperty("candidates");
            list.arraySize = entries.Length;
            for (int i = 0; i < entries.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = entries[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Sprite CreatePortrait(string name, bool player)
        {
            string path = Root + "Art/Characters/" + name + ".png";
            var texture = new Texture2D(128, 192, TextureFormat.RGBA32, false);
            for (int y = 0; y < 192; y++)
                for (int x = 0; x < 128; x++)
                {
                    Color color = Color.clear;
                    if (player)
                    {
                        if (y > 18 && y < 80 && ((x > 40 && x < 56) || (x > 70 && x < 86))) color = new Color(.28f, .22f, .18f);
                        if (y >= 75 && y < 135 && x > 34 && x < 94) color = new Color(.67f, .28f, .16f);
                        if ((x - 64) * (x - 64) + (y - 153) * (y - 153) < 400) color = new Color(.8f, .67f, .44f);
                        if (x > 14 && x < 22 && y > 70 && y < 176) color = new Color(.73f, .78f, .76f);
                        if (x > 7 && x < 30 && y > 67 && y < 74) color = new Color(.6f, .43f, .17f);
                    }
                    else
                    {
                        float dx = (x - 64f) / 57f, dy = (y - 72f) / 52f;
                        if (dx * dx + dy * dy < 1f && y > 24) color = new Color(.35f, .66f, .39f);
                        if (y > 76 && y < 88 && ((x > 36 && x < 45) || (x > 80 && x < 89))) color = new Color(.07f, .13f, .09f);
                    }
                    texture.SetPixel(x, y, color);
                }
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
