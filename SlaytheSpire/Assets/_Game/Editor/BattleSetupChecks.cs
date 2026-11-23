using System;
using Roguelike.Combat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
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
                Require(energy.text == "ENERGY  1 / 3", "Energy UI did not refresh.");
                setup.InitializeBattle();
                Require(!ReferenceEquals(oldPlayer, setup.Player) && setup.Player.Combatant.CurrentHealth == 80, "Reset reused runtime state.");
                Require(originalEnemy.MaxHealth == 30, "Runtime changed the definition.");
                oldPlayer.SetEnergy(0);
                Require(energy.text == "ENERGY  3 / 3", "Old state retained a UI subscription.");
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

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }
        private static void Finish(Exception exception)
        {
            SessionState.SetBool(Running, false);
            EditorApplication.update -= Tick;
            if (exception != null) Debug.LogException(exception);
            else Debug.Log("BATTLE_SETUP_CHECKS_PASSED: pools, state isolation, scene startup, UI updates, reset.");
            EditorApplication.Exit(exception == null ? 0 : 1);
        }
    }
}
