using Roguelike.UI;
using UnityEngine;
using UnityEngine.UI;
using Roguelike.Core;
using Roguelike.Cards;

namespace Roguelike.Combat
{
    public enum BattlePhase { Inactive, PlayerTurn, EnemyTurn, Victory, Defeat }

    public sealed class BattleSetup : MonoBehaviour
    {
        private const int CardsPerTurn = 5;

        [SerializeField] private CharacterDefinition character;
        [SerializeField] private EnemyPool enemyPool;
        [SerializeField] private CombatantView playerView;
        [SerializeField] private CombatantView enemyView;
        [SerializeField] private EnemyIntentView intentView;
        [SerializeField] private Text energyLabel;
        [SerializeField] private Text statusLabel;
        [SerializeField] private Button endTurnButton;
        [SerializeField] private RunState runState;
        [SerializeField] private string energyFormat = "ENERGY  {0} / {1}";
        private int enemyActionIndex;
        private int endTurnFrame;

        public string CharacterName => character == null ? "" : character.DisplayName;
        public CardPileState Piles { get; private set; }
        public event System.Action Initialized;
        public PlayerBattleState Player { get; private set; }
        public CombatantState Enemy { get; private set; }
        public EnemyDefinition SelectedEnemy { get; private set; }
        public EnemyAction CurrentEnemyAction { get; private set; }
        public BattlePhase Phase { get; private set; }

        private void Start() => InitializeBattle();

        private void OnEnable()
        {
            if (endTurnButton != null) endTurnButton.onClick.AddListener(EndTurn);
            RefreshEndTurnButton();
        }

        private void OnDisable()
        {
            if (endTurnButton != null) endTurnButton.onClick.RemoveListener(EndTurn);
            RefreshEndTurnButton();
        }

        private void Update()
        {
            // Keep the enemy phase locked through the frame that received the click.
            if (Phase == BattlePhase.EnemyTurn && Time.frameCount != endTurnFrame)
                ResolveEnemyTurn();
        }

        public void EndTurn()
        {
            if (!isActiveAndEnabled || Phase != BattlePhase.PlayerTurn) return;

            endTurnFrame = Time.frameCount;
            SetPhase(BattlePhase.EnemyTurn);
            Piles?.DiscardHand();
        }

        public void InitializeBattle()
        {
            ReleaseState();
            if (playerView == null || enemyView == null || intentView == null
                || energyLabel == null || statusLabel == null || endTurnButton == null)
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
            playerView.gameObject.SetActive(true);
            enemyView.gameObject.SetActive(true);
            playerView.Bind(character.DisplayName, character.Portrait, Player.Combatant);
            enemyView.Bind(selected.DisplayName, selected.Portrait, Enemy);
            intentView.BindTarget(enemyView);
            enemyActionIndex = 0;
            ShowEnemyAction();
            Player.Changed += RefreshEnergy;
            Player.Combatant.Changed += CheckBattleResult;
            Enemy.Changed += CheckBattleResult;
            statusLabel.text = "";
            BeginPlayerTurn();
            Initialized?.Invoke();
        }

        private void BeginPlayerTurn()
        {
            if (Phase != BattlePhase.Inactive && Phase != BattlePhase.EnemyTurn) return;

            Player.Combatant.SetBlock(0);
            if (Phase == BattlePhase.Victory || Phase == BattlePhase.Defeat) return;

            Player.SetEnergy(Player.BaseEnergy);
            if (Phase == BattlePhase.Inactive || Phase == BattlePhase.EnemyTurn)
            {
                SetPhase(BattlePhase.PlayerTurn);
                Piles?.Draw(CardsPerTurn);
            }
        }

        private void ResolveEnemyTurn()
        {
            Enemy.SetBlock(0);
            if (Phase != BattlePhase.EnemyTurn) return;

            var action = CurrentEnemyAction;
            if (action.Type == EnemyActionType.Attack)
                Player.Combatant.TakeDamage(action.Amount);
            else
                Enemy.SetBlock(action.Amount);

            if (Phase != BattlePhase.EnemyTurn) return;

            enemyActionIndex = (enemyActionIndex + 1) % SelectedEnemy.ActionCount;
            ShowEnemyAction();
            BeginPlayerTurn();
        }

        private void ShowEnemyAction()
        {
            CurrentEnemyAction = SelectedEnemy.GetAction(enemyActionIndex);
            intentView.Show(CurrentEnemyAction);
        }

        private void CheckBattleResult()
        {
            if (Phase == BattlePhase.Victory || Phase == BattlePhase.Defeat) return;

            if (Player.Combatant.CurrentHealth == 0)
                FinishBattle(BattlePhase.Defeat, "패배");
            else if (Enemy.CurrentHealth == 0)
                FinishBattle(BattlePhase.Victory, "승리");
        }

        private void FinishBattle(BattlePhase result, string message)
        {
            SetPhase(result);
            CurrentEnemyAction = null;
            intentView.Clear();
            statusLabel.text = message;
        }

        private void SetPhase(BattlePhase phase)
        {
            Phase = phase;
            RefreshEndTurnButton();
        }

        private void RefreshEndTurnButton()
        {
            if (endTurnButton != null)
                endTurnButton.interactable = isActiveAndEnabled && Phase == BattlePhase.PlayerTurn;
        }

        private void RefreshEnergy() => energyLabel.text = string.Format(energyFormat, Player.CurrentEnergy, Player.BaseEnergy);
        private void Fail(string message)
        {
            if (statusLabel != null) statusLabel.text = message;
            Debug.LogError(message, this);
        }
        private void ReleaseState()
        {
            SetPhase(BattlePhase.Inactive);
            if (Player != null)
            {
                Player.Changed -= RefreshEnergy;
                Player.Combatant.Changed -= CheckBattleResult;
            }
            if (Enemy != null) Enemy.Changed -= CheckBattleResult;
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
