using System;
using System.Collections;
using System.Linq;
using Roguelike.Cards;
using Roguelike.Combat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
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
        private static IEnumerator turnChecks;
        private static int nextCheckFrame;

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
                if (turnChecks != null)
                {
                    if (Time.frameCount < nextCheckFrame) return;
                    if (turnChecks.MoveNext()) nextCheckFrame = Time.frameCount + 2;
                    else Finish(null);
                    return;
                }

                var setup = Object.FindFirstObjectByType<BattleSetup>();
                Require(setup != null && setup.Player != null && setup.Enemy != null, "Scene failed to initialize.");
                Require(setup.Piles.Hand.Count == 5 && setup.Piles.DrawPile.Count == 5 && setup.Piles.DiscardPile.Count == 0,
                    "Scene Start must draw exactly one opening hand.");
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
                turnChecks = CheckTurns(setup);
            }
            catch (Exception exception) { Finish(exception); }
        }

        private static void CheckData()
        {
            CheckCardData();
            CheckPileTransitions();
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
                state.SetBlock(9);
                state.TakeDamage(-4);
                Require(state.CurrentHealth == 30 && state.Block == 9, "Negative damage changed state.");
                state.TakeDamage(6);
                Require(state.CurrentHealth == 30 && state.Block == 3, "Block failed to absorb a hit.");
                state.TakeDamage(3);
                Require(state.CurrentHealth == 30 && state.Block == 0, "Exact block should prevent all damage.");
                state.SetBlock(2);
                state.TakeDamage(int.MaxValue);
                Require(state.CurrentHealth == 0 && state.Block == 0, "Overkill should clamp health and block to zero.");
            }
            finally
            {
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(second);
                Object.DestroyImmediate(invalid);
                Object.DestroyImmediate(pool);
            }
        }

        private static void CheckCardData()
        {
            CheckCard("Strike", "타격", 1, (CardEffectType.Damage, 6));
            CheckCard("Defend", "수비", 1, (CardEffectType.Block, 5));
            var bash = CheckCard("Bash", "강타", 2, (CardEffectType.Damage, 8), (CardEffectType.Vulnerable, 2));
            Require(bash.HoverHelp.Contains("50%"), "Bash is missing its Vulnerable explanation.");
        }

        private static CardDefinition CheckCard(string name, string displayName, int cost,
            params (CardEffectType type, int amount)[] effects)
        {
            var card = AssetDatabase.LoadAssetAtPath<CardDefinition>($"Assets/_Game/Data/Cards/{name}.asset");
            Require(card != null && card.Id == name.ToLowerInvariant() && card.Image != null
                && card.DisplayName == displayName && card.Cost == cost && !string.IsNullOrWhiteSpace(card.Description),
                $"Missing or incorrect {name} card data.");
            Require(card.EffectCount == effects.Length, $"Incorrect effect count for {name}.");
            for (int i = 0; i < effects.Length; i++)
            {
                var effect = card.GetEffect(i);
                Require(effect.Type == effects[i].type && effect.Amount == effects[i].amount, $"Incorrect effect on {name}.");
            }
            return card;
        }

        private static void CheckPileTransitions()
        {
            var deck = new[] { "a", "b", "c", "d", "e", "f", "g" };
            var originalDeck = deck.ToArray();
            var piles = new CardPileState(deck, new System.Random(42));
            Require(!piles.DrawPile.SequenceEqual(deck) && piles.DrawPile.OrderBy(id => id).SequenceEqual(deck),
                "Initial shuffle must reorder the deck and preserve every card.");
            piles.Draw(5);
            var firstHand = piles.Hand.ToArray();
            var remaining = piles.DrawPile.Reverse().ToArray();
            piles.DiscardHand();
            Require(piles.Hand.Count == 0 && piles.DiscardPile.SequenceEqual(firstHand), "Entire hand must be discarded in order.");
            piles.Draw(5);
            Require(piles.Hand.Take(2).SequenceEqual(remaining) && piles.DrawPile.Count == 2
                && piles.DiscardPile.Count == 0, "Draw must use the remaining pile before recycling the discard.");
            Require(!piles.Hand.Skip(2).Concat(piles.DrawPile.Reverse()).SequenceEqual(firstHand.Reverse()),
                "Recycled discard pile was not shuffled.");
            piles.Draw(10);
            Require(piles.Hand.OrderBy(id => id).SequenceEqual(deck) && piles.DrawPile.Count == 0 && piles.DiscardPile.Count == 0,
                "Drawing with both piles empty must stop without losing or duplicating cards.");
            Require(deck.SequenceEqual(originalDeck), "Combat mutated its source deck.");
            var empty = new CardPileState(Array.Empty<string>());
            empty.Draw(5);
            empty.DiscardHand();
            Require(empty.Hand.Count == 0, "An empty deck must remain empty.");
            Debug.Log("CARD_PILE_CHECKS_PASSED: definitions, initial shuffle, draw across both piles, recycle shuffle, exhaustion, source isolation.");
        }

        private static IEnumerator CheckTurns(BattleSetup setup)
        {
            var serialized = new SerializedObject(setup);
            var endTurn = (Button)serialized.FindProperty("endTurnButton").objectReferenceValue;
            var status = (Text)serialized.FindProperty("statusLabel").objectReferenceValue;
            var energy = (Text)serialized.FindProperty("energyLabel").objectReferenceValue;
            var intent = ((EnemyIntentView)serialized.FindProperty("intentView").objectReferenceValue).GetComponent<Text>();
            var click = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            var run = Object.FindFirstObjectByType<RunState>();
            var originalDeck = run.Deck.ToArray();
            var hand = Object.FindFirstObjectByType<BattleHud>().transform.Find("Hand Area");

            Require(setup.Phase == BattlePhase.PlayerTurn && endTurn.IsInteractable(), "Battle must start on a playable player turn.");
            BattleLayoutChecks.AssertHandInput(hand);
            var firstAction = setup.CurrentEnemyAction;
            Require(intent.text == "ATTACK  6", "First action was not displayed.");
            endTurn.OnPointerClick(click);
            Require(setup.Phase == BattlePhase.EnemyTurn && !endTurn.IsInteractable(), "Enemy turn must lock the button immediately.");
            Require(setup.Piles.Hand.Count == 0 && setup.Piles.DrawPile.Count == 5 && setup.Piles.DiscardPile.Count == 5,
                "End turn must immediately discard the hand.");
            Require(setup.Player.Combatant.CurrentHealth == 80, "Enemy acted before entering its turn.");
            endTurn.OnPointerClick(click);
            endTurn.onClick.Invoke();
            yield return null;
            Require(setup.Phase == BattlePhase.PlayerTurn && endTurn.IsInteractable(), "Player turn did not resume.");
            Require(setup.Player.Combatant.CurrentHealth == 74, "Single-action enemy attack was skipped or duplicated.");
            Require(setup.CurrentEnemyAction == firstAction && intent.text == "ATTACK  6", "Single action did not repeat.");
            Require(setup.Piles.Hand.Count == 5 && setup.Piles.DrawPile.Count == 0 && setup.Piles.DiscardPile.Count == 5,
                "Second turn must draw the remaining five cards.");
            endTurn.OnPointerClick(click);
            yield return null;
            Require(setup.Piles.Hand.Count == 5 && setup.Piles.DrawPile.Count == 5 && setup.Piles.DiscardPile.Count == 0,
                "Third turn must draw five from the recycled discard.");
            Require(run.Deck.SequenceEqual(originalDeck), "Combat turns modified the original start deck.");

            var originalPool = (EnemyPool)serialized.FindProperty("enemyPool").objectReferenceValue;
            var enemy = Object.Instantiate(setup.SelectedEnemy);
            var pool = ScriptableObject.CreateInstance<EnemyPool>();
            try
            {
                var serializedEnemy = new SerializedObject(enemy);
                var actions = serializedEnemy.FindProperty("actions");
                actions.arraySize = 2;
                var blockAction = actions.GetArrayElementAtIndex(1);
                blockAction.FindPropertyRelative("type").enumValueIndex = (int)EnemyActionType.Block;
                blockAction.FindPropertyRelative("amount").intValue = 5;
                serializedEnemy.ApplyModifiedPropertiesWithoutUndo();
                BattleSceneTools.SetCandidates(pool, enemy);
                BattleSceneTools.Set(setup, "enemyPool", pool);
                setup.InitializeBattle();

                var player = setup.Player.Combatant;
                player.SetBlock(4);
                setup.Player.SetEnergy(1);
                bool attackResolvedWhileLocked = false;
                player.Changed += () =>
                {
                    if (player.CurrentHealth != 78 || setup.Player.CurrentEnergy != 1) return;
                    attackResolvedWhileLocked = setup.Phase == BattlePhase.EnemyTurn && !endTurn.IsInteractable() && player.Block == 0;
                    setup.EndTurn();
                };
                endTurn.OnPointerClick(click);
                yield return null;
                Require(attackResolvedWhileLocked && player.CurrentHealth == 78 && setup.Phase == BattlePhase.PlayerTurn,
                    "Attack did not consume block before HP during the locked enemy turn.");
                Require(setup.Player.CurrentEnergy == 3 && energy.text == "3/3", "Player energy did not recover.");
                Require(setup.CurrentEnemyAction == enemy.GetAction(1) && intent.text == "BLOCK  5", "Block intent did not advance.");

                setup.Enemy.SetBlock(9);
                player.SetBlock(7);
                endTurn.OnPointerClick(click);
                Require(setup.Enemy.Block == 9 && player.Block == 7, "Block reset before the owning turn started.");
                yield return null;
                Require(setup.Enemy.Block == 5 && player.CurrentHealth == 78 && player.Block == 0,
                    "Block action, enemy block replacement, or player block reset failed.");
                Require(setup.CurrentEnemyAction == enemy.GetAction(0) && intent.text == "ATTACK  6", "Attack intent did not wrap.");
                BattleLayoutChecks.AssertIntentAlignment(intent.GetComponent<EnemyIntentView>());

                player.SetHealth(2);
                setup.Player.SetEnergy(1);
                endTurn.OnPointerClick(click);
                yield return null;
                Require(setup.Phase == BattlePhase.Defeat && player.CurrentHealth == 0 && setup.Enemy.Block == 0,
                    "Lethal attack did not end the battle or clear expired enemy block.");
                Require(!endTurn.IsInteractable() && status.text == "패배" && setup.Player.CurrentEnergy == 1,
                    "Defeat did not lock input or incorrectly started another player turn.");
                endTurn.onClick.Invoke();
                setup.EndTurn();
                Require(setup.Phase == BattlePhase.Defeat, "Turn input bypassed defeat.");

                setup.InitializeBattle();
                endTurn.OnPointerClick(click);
                setup.Enemy.TakeDamage(setup.Enemy.MaxHealth);
                Require(setup.Phase == BattlePhase.Victory && !endTurn.IsInteractable() && status.text == "승리",
                    "Enemy death must declare victory and lock input immediately.");
                Require(setup.CurrentEnemyAction == null && !intent.enabled, "Finished battles should clear pending intent.");
                endTurn.OnPointerClick(click);
                endTurn.onClick.Invoke();
                setup.EndTurn();
                yield return null;
                Require(setup.Phase == BattlePhase.Victory && setup.Player.Combatant.CurrentHealth == 80,
                    "A pending enemy action or duplicate input ran after victory.");

                setup.InitializeBattle();
                setup.Piles.Draw(5);
                yield return null;
                BattleLayoutChecks.AssertHandInput(hand);
                Debug.Log("BATTLE_HAND_INPUT_PASSED: each card can be hovered in five- and ten-card fans.");
                Debug.Log("BATTLE_TURN_CHECKS_PASSED: player/enemy order, damage and block, energy, intent cycle, victory, defeat, duplicate input.");
            }
            finally
            {
                BattleSceneTools.Set(setup, "enemyPool", originalPool);
                setup.InitializeBattle();
                Object.DestroyImmediate(pool);
                Object.DestroyImmediate(enemy);
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
            Require(Label("drawCount").text == "5" && Label("discardCount").text == "0", "Initial pile counts incorrect.");
            Require(setup.Piles.Hand.Count == 5 && hud.transform.Find("Hand Area").GetComponentsInChildren<EventTrigger>().Length == 5,
                "Hand view did not show five cards.");
            Require(run.Deck.Count(id => id == "strike") == 5 && run.Deck.Count(id => id == "defend") == 4
                && run.Deck.Count(id => id == "bash") == 1, "Incorrect start deck composition.");
            setup.Piles.Draw(3);
            Require(Label("drawCount").text == "2" && setup.Piles.Hand.Count == 8, "Draw count did not update.");
            setup.Piles.DiscardFromHand(0);
            Require(Label("discardCount").text == "1" && setup.Piles.Hand.Count == 7, "Discard count did not update.");
            Require(run.Deck.Count == 10 && Label("deckCount").text == "10", "Combat changed the run deck.");
            run.SetGold(125);
            run.SetFloor(2);
            Require(Label("gold").text == "125" && Label("floor").text == "2", "Run values did not refresh.");
            var popup = (GameObject)serialized.FindProperty("popup").objectReferenceValue;
            var bar = hud.transform.Find("Top Status Bar");
            bar.Find("Deck").GetComponent<Button>().onClick.Invoke();
            Require(popup.activeSelf && Label("popupBody").text.Contains("타격"), "Deck button failed.");
            hud.ClosePopup();
            hud.transform.Find("DrawPile").GetComponent<Button>().onClick.Invoke();
            Require(popup.activeSelf && Label("popupBody").text.Contains("총 2장"), "Draw pile button shows stale cards.");
            hud.ClosePopup();
            hud.transform.Find("DiscardPile").GetComponent<Button>().onClick.Invoke();
            Require(popup.activeSelf && Label("popupBody").text.Contains("총 1장"), "Discard pile button shows stale cards.");
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
            Require(Label("health").text == "80/80" && Label("drawCount").text == "5", "HUD reset failed.");
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
            (turnChecks as IDisposable)?.Dispose();
            turnChecks = null;
            if (exception != null) Debug.LogException(exception);
            else Debug.Log("BATTLE_SETUP_CHECKS_PASSED: pools, state isolation, HUD, reset, intent follows enemy movement, resizing and sprite changes.");
            EditorApplication.Exit(exception == null ? 0 : 1);
        }
    }
}
