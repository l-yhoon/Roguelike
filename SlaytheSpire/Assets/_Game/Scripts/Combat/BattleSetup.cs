using Roguelike.UI;
using UnityEngine;
using UnityEngine.UI;
using Roguelike.Core;
using Roguelike.Cards;

namespace Roguelike.Combat
{
    public sealed class BattleSetup : MonoBehaviour
    {
        [SerializeField] private CharacterDefinition character;
        [SerializeField] private EnemyPool enemyPool;
        [SerializeField] private CombatantView playerView;
        [SerializeField] private CombatantView enemyView;
        [SerializeField] private EnemyIntentView intentView;
        [SerializeField] private Text energyLabel;
        [SerializeField] private Text statusLabel;
        [SerializeField] private RunState runState;
        [SerializeField] private string energyFormat = "ENERGY  {0} / {1}";
        [SerializeField] private bool showReadyMessage = true;
        public string CharacterName => character == null ? "" : character.DisplayName;
        public CardPileState Piles { get; private set; }
        public event System.Action Initialized;
        public PlayerBattleState Player { get; private set; }
        public CombatantState Enemy { get; private set; }
        public EnemyDefinition SelectedEnemy { get; private set; }
        public EnemyAction CurrentEnemyAction { get; private set; }

        private void Start() => InitializeBattle();

        public void InitializeBattle()
        {
            ReleaseState();
            if (playerView == null || enemyView == null || intentView == null
                || energyLabel == null || statusLabel == null)
            {
                Fail("BattleSetup: view references are missing. Check the Inspector.");
                return;
            }
            playerView.gameObject.SetActive(false);
            enemyView.gameObject.SetActive(false);
            intentView.Clear();
            energyLabel.text = "ENERGY  --";
            if (character == null || !character.IsValid)
            {
                Fail("BattleSetup: assign a valid character definition.");
                return;
            }
            if (enemyPool == null || !enemyPool.TryPick(out var selected))
            {
                Fail("BattleSetup: enemy pool has no valid candidates.");
                return;
            }

            Player = new PlayerBattleState(character);
            if (runState != null) Piles = new CardPileState(runState.Deck);
            SelectedEnemy = selected;
            Enemy = new CombatantState(selected.MaxHealth);
            CurrentEnemyAction = selected.GetAction(0);
            playerView.gameObject.SetActive(true);
            enemyView.gameObject.SetActive(true);
            playerView.Bind(character.DisplayName, character.Portrait, Player.Combatant);
            enemyView.Bind(selected.DisplayName, selected.Portrait, Enemy);
            intentView.Show(CurrentEnemyAction);
            Player.Changed += RefreshEnergy;
            RefreshEnergy();
            statusLabel.text = showReadyMessage ? "Battle setup ready  |  Card and turn systems come next" : "";
            Initialized?.Invoke();
        }

        private void RefreshEnergy() => energyLabel.text = string.Format(energyFormat, Player.CurrentEnergy, Player.BaseEnergy);
        private void Fail(string message)
        {
            if (statusLabel != null) statusLabel.text = message;
            Debug.LogError(message, this);
        }
        private void ReleaseState()
        {
            if (Player != null) Player.Changed -= RefreshEnergy;
            if (playerView != null) playerView.Unbind();
            if (enemyView != null) enemyView.Unbind();
            Player = null;
            Enemy = null;
            SelectedEnemy = null;
            CurrentEnemyAction = null;
            Piles = null;
        }
        private void OnDestroy() => ReleaseState();
    }
}
