using System;
using Roguelike.Combat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Roguelike.Core;
using Roguelike.UI;
using Object = UnityEngine.Object;

namespace Roguelike.Editor
{
    // Run with -executeMethod Roguelike.Editor.BattleSetupChecks.RunPlayMode
    // without -quit: this runner exits after the Play Mode checks complete.
    [InitializeOnLoad]
    public static class BattleSetupChecks
    {
        private const string Running = "Roguelike.BattleChecks.Running";
        private static double deadline;
        private static int frames;

        static BattleSetupChecks()
        {
            if (SessionState.GetBool(Running, false)) Attach();
        }

        public static void RunPlayMode()
        {
            try
            {
                CheckData();
                EditorSceneManager.OpenScene(BattleSceneTools.ScenePath);
                SessionState.SetBool(Running, true);
                Attach();
                EditorApplication.isPlaying = true;
            }
            catch (Exception exception) { Finish(exception); }
        }

        private static void Attach()
        {
            deadline = EditorApplication.timeSinceStartup + 90;
            frames = 0;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            if (EditorApplication.timeSinceStartup > deadline)
            {
                Finish(new Exception("Timed out entering Play Mode."));
                return;
            }
            if (!EditorApplication.isPlaying || EditorApplication.isCompiling || ++frames < 30) return;
            try
            {
                var setup = Object.FindFirstObjectByType<BattleSetup>();
                Require(setup != null && setup.Player != null && setup.Enemy != null, "Scene failed to initialize.");
                Require(setup.Player.Combatant.CurrentHealth == 80 && setup.Player.CurrentEnergy == 3, "Incorrect Ironclad state.");
                Require(setup.SelectedEnemy.Id == "training-slime", "Single-candidate pool selected wrong enemy.");
                Require(setup.CurrentEnemyAction == setup.SelectedEnemy.GetAction(0), "Intent must use the selected action.");
                var originalEnemy = setup.SelectedEnemy;
                var oldPlayer = setup.Player;
                setup.Player.Combatant.SetHealth(10);
                setup.Player.SetEnergy(1);
                var serialized = new SerializedObject(setup);
                var energy = (Text)serialized.FindProperty("energyLabel").objectReferenceValue;
                string format = serialized.FindProperty("energyFormat").stringValue;
                Require(energy.text == string.Format(format, 1, 3), "Energy UI did not refresh.");
                setup.InitializeBattle();
                Require(!ReferenceEquals(oldPlayer, setup.Player) && setup.Player.Combatant.CurrentHealth == 80, "Reset reused runtime state.");
                Require(originalEnemy.MaxHealth == 30, "Runtime changed the definition.");
                oldPlayer.SetEnergy(0);
                Require(energy.text == string.Format(format, 3, 3), "Old state retained a UI subscription.");
                CheckHud(setup);
                CheckIntentFollow();
                Finish(null);
            }
            catch (Exception exception) { Finish(exception); }
        }

        private static void CheckData()
        {
            var first = ScriptableObject.CreateInstance<EnemyDefinition>();
            var second = ScriptableObject.CreateInstance<EnemyDefinition>();
            var invalid = ScriptableObject.CreateInstance<EnemyDefinition>();
            var pool = ScriptableObject.CreateInstance<EnemyPool>();
            try
            {
                var serialized = new SerializedObject(invalid);
                serialized.FindProperty("maxHealth").intValue = 0;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                Require(!pool.TryPick(out _), "Empty pool should fail.");
                BattleSceneTools.SetCandidates(pool, null, invalid);
                Require(!pool.TryPick(out _), "Invalid candidates should be rejected.");
                BattleSceneTools.SetCandidates(pool, first);
                Require(pool.TryPick(out var only) && only == first, "Single candidate should work.");
                BattleSceneTools.SetCandidates(pool, first, second, first, null, invalid);
                var rng = new System.Random(42);
                bool sawFirst = false, sawSecond = false;
                for (int i = 0; i < 100; i++)
                {
                    Require(pool.TryPick(out var chosen, rng), "Valid pool failed.");
                    Require(chosen == first || chosen == second, "Selected an invalid candidate.");
                    sawFirst |= chosen == first;
                    sawSecond |= chosen == second;
                }
                Require(sawFirst && sawSecond, "Both candidates must be reachable.");
                var state = new CombatantState(first.MaxHealth);
                state.SetHealth(-10);
                state.SetBlock(-5);
                Require(state.CurrentHealth == 0 && state.Block == 0 && first.MaxHealth == 30, "State isolation or bounds failed.");
                state.SetHealth(999);
                Require(state.CurrentHealth == 30, "Health exceeded maximum.");
            }
            finally
            {
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(second);
                Object.DestroyImmediate(invalid);
                Object.DestroyImmediate(pool);
            }
        }

        private static void CheckHud(BattleSetup setup)
        {
            var hud = Object.FindFirstObjectByType<BattleHud>();
            if (hud == null) throw new Exception("Reference HUD is missing.");
            var run = Object.FindFirstObjectByType<RunState>();
            var serialized = new SerializedObject(hud);
            Text Label(string field) => (Text)serialized.FindProperty(field).objectReferenceValue;
            Require(Label("health").text == "80/80", "Top health did not bind.");
            setup.Player.Combatant.SetHealth(47);
            Require(Label("health").text == "47/80", "Top health did not update.");
            Require(Label("drawCount").text == "10" && Label("discardCount").text == "0", "Initial pile counts incorrect.");
            setup.Piles.Draw(3);
            Require(Label("drawCount").text == "7" && setup.Piles.Hand.Count == 3, "Draw count did not update.");
            setup.Piles.DiscardFromHand(0);
            Require(Label("discardCount").text == "1" && setup.Piles.Hand.Count == 2, "Discard count did not update.");
            Require(run.Deck.Count == 10 && Label("deckCount").text == "10", "Combat changed the run deck.");
            run.SetGold(125);
            run.SetFloor(2);
            Require(Label("gold").text == "125" && Label("floor").text == "2", "Run values did not refresh.");
            var popup = (GameObject)serialized.FindProperty("popup").objectReferenceValue;
            var bar = hud.transform.Find("Top Status Bar");
            bar.Find("Deck").GetComponent<Button>().onClick.Invoke();
            Require(popup.activeSelf && Label("popupBody").text.Contains("타격"), "Deck button failed.");
            hud.ClosePopup();
            bar.Find("Map").GetComponent<Button>().onClick.Invoke();
            Require(popup.activeSelf && Label("popupBody").text.Contains("2층"), "Map button failed.");
            bar.Find("Settings").GetComponent<Button>().onClick.Invoke();
            var settings = (GameObject)serialized.FindProperty("settings").objectReferenceValue;
            Require(settings.activeSelf, "Settings panel failed.");
            hud.SetVolume(.35f);
            Require(Mathf.Approximately(AudioListener.volume, .35f), "Volume setting failed.");
            hud.SetVolume(1);
            hud.ClosePopup();
            setup.InitializeBattle();
            Require(Label("health").text == "80/80" && Label("drawCount").text == "10", "HUD reset failed.");
            Require(run.Gold == 125 && run.Floor == 2, "Battle reset erased run state.");
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }

        private static void CheckIntentFollow()
        {
            var intent = Object.FindFirstObjectByType<EnemyIntentView>();
            BattleLayoutChecks.AssertIntentAlignment(intent);
            var enemy = intent.transform.parent.GetComponent<CombatantView>();
            var enemyRect = (RectTransform)enemy.transform;
            Vector2 originalPosition = enemyRect.anchoredPosition;
            Vector3 before = enemyRect.parent.InverseTransformPoint(intent.transform.position);
            enemyRect.anchoredPosition += new Vector2(70, 35);
            intent.RefreshPosition();
            Vector3 after = enemyRect.parent.InverseTransformPoint(intent.transform.position);
            Require(Vector2.Distance((Vector2)(after - before), new Vector2(70, 35)) < .5f,
                "Intent did not follow enemy movement.");
            BattleLayoutChecks.AssertIntentAlignment(intent);
            var serialized = new SerializedObject(enemy);
            var portrait = (Image)serialized.FindProperty("portrait").objectReferenceValue;
            Vector2 originalSize = portrait.rectTransform.sizeDelta;
            Sprite originalSprite = portrait.sprite;
            try
            {
                portrait.rectTransform.sizeDelta *= 1.4f;
                Canvas.ForceUpdateCanvases();
                intent.RefreshPosition();
                BattleLayoutChecks.AssertIntentAlignment(intent);
                Require(Mathf.Abs(intent.transform.localPosition.y - (after.y - enemyRect.localPosition.y)) > 1,
                    "Intent did not follow portrait resizing.");
                portrait.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Game/Art/UI/Reference/Ironclad.sprite.asset");
                intent.RefreshPosition();
                BattleLayoutChecks.AssertIntentAlignment(intent);
            }
            finally
            {
                portrait.sprite = originalSprite;
                portrait.rectTransform.sizeDelta = originalSize;
                enemyRect.anchoredPosition = originalPosition;
                intent.RefreshPosition();
            }
        }
        private static void Finish(Exception exception)
        {
            SessionState.SetBool(Running, false);
            EditorApplication.update -= Tick;
            if (exception != null) Debug.LogException(exception);
            else Debug.Log("BATTLE_SETUP_CHECKS_PASSED: pools, state isolation, HUD, reset, intent follows enemy movement, resizing and sprite changes.");
            EditorApplication.Exit(exception == null ? 0 : 1);
        }
    }
}
