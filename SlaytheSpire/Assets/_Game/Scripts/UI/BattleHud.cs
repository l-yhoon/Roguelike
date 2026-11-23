using System.Collections.Generic;
using System.Text;
using Roguelike.Combat;
using Roguelike.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Roguelike.UI
{
    public sealed class BattleHud : MonoBehaviour
    {
        [SerializeField] private BattleSetup battle;
        [SerializeField] private RunState run;
        [SerializeField] private Text playerName;
        [SerializeField] private Text characterName;
        [SerializeField] private Text health;
        [SerializeField] private Text gold;
        [SerializeField] private Text floor;
        [SerializeField] private Text deckCount;
        [SerializeField] private Text drawCount;
        [SerializeField] private Text discardCount;
        [SerializeField] private GameObject popup;
        [SerializeField] private Text popupTitle;
        [SerializeField] private Text popupBody;
        [SerializeField] private GameObject settings;
        [SerializeField] private Toggle fullscreen;
        [SerializeField] private Slider volume;
        [SerializeField] private Button[] potionButtons;
        private PlayerBattleState observedPlayer;
        private Roguelike.Cards.CardPileState observedPiles;

        private void OnEnable()
        {
            battle.Initialized += BindBattle;
            run.Changed += Refresh;
            BindBattle();
        }
        private void OnDisable()
        {
            battle.Initialized -= BindBattle;
            run.Changed -= Refresh;
            UnbindBattle();
        }
        public void BindBattle()
        {
            UnbindBattle();
            observedPlayer = battle.Player;
            observedPiles = battle.Piles;
            if (observedPlayer != null) observedPlayer.Combatant.Changed += Refresh;
            if (observedPiles != null) observedPiles.Changed += Refresh;
            Refresh();
        }
        private void UnbindBattle()
        {
            if (observedPlayer != null) observedPlayer.Combatant.Changed -= Refresh;
            if (observedPiles != null) observedPiles.Changed -= Refresh;
            observedPlayer = null;
            observedPiles = null;
        }
        public void Refresh()
        {
            playerName.text = run.PlayerName;
            characterName.text = battle.CharacterName;
            health.text = observedPlayer == null ? "--/--"
                : $"{observedPlayer.Combatant.CurrentHealth}/{observedPlayer.Combatant.MaxHealth}";
            gold.text = run.Gold.ToString();
            floor.text = run.Floor.ToString();
            deckCount.text = run.Deck.Count.ToString();
            drawCount.text = (observedPiles?.DrawPile.Count ?? 0).ToString();
            discardCount.text = (observedPiles?.DiscardPile.Count ?? 0).ToString();
            for (int i = 0; i < potionButtons.Length; i++)
            {
                var label = potionButtons[i].GetComponentInChildren<Text>();
                bool occupied = i < run.PotionSlots && !string.IsNullOrEmpty(run.GetPotion(i));
                label.text = occupied ? "●" : "";
                potionButtons[i].GetComponent<Image>().color = occupied ? new Color(.75f, .4f, .85f) : Color.white;
            }
        }
        public void ShowMap() => Show("지도", $"1막 · {run.Floor}층\n\n아직 생성된 경로가 없습니다.");
        public void ShowDeck() => Show("전체 카드", CardList(run.Deck));
        public void ShowDraw() => Show("뽑을 카드", observedPiles == null ? "" : CardList(observedPiles.DrawPile));
        public void ShowDiscard() => Show("버린 카드", observedPiles == null ? "" : CardList(observedPiles.DiscardPile));
        public void ShowPotion(int slot)
        {
            string value = slot < run.PotionSlots ? run.GetPotion(slot) : "";
            Show("포션 가방", string.IsNullOrEmpty(value) ? "빈 포션 슬롯입니다." : value);
        }
        public void ShowSettings()
        {
            Show("설정", "");
            settings.SetActive(true);
            fullscreen.SetIsOnWithoutNotify(Screen.fullScreen);
            volume.SetValueWithoutNotify(AudioListener.volume);
        }
        public void SetFullscreen(bool value) => Screen.fullScreen = value;
        public void SetVolume(float value) => AudioListener.volume = Mathf.Clamp01(value);
        public void ClosePopup() => popup.SetActive(false);
        private void Show(string title, string body)
        {
            popupTitle.text = title;
            popupBody.text = body;
            settings.SetActive(false);
            popup.SetActive(true);
            popup.transform.SetAsLastSibling();
        }
        private static string CardList(IReadOnlyList<string> cards)
        {
            if (cards.Count == 0) return "카드가 없습니다.";
            var counts = new Dictionary<string, int>();
            for (int i = 0; i < cards.Count; i++)
            {
                string name = RunState.CardName(cards[i]);
                counts[name] = counts.TryGetValue(name, out int count) ? count + 1 : 1;
            }
            var text = new StringBuilder($"총 {cards.Count}장\n\n");
            foreach (var entry in counts) text.AppendLine($"{entry.Key}  × {entry.Value}");
            return text.ToString();
        }
    }
}
