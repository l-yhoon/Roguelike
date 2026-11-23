using System;
using System.Collections.Generic;
using UnityEngine;

namespace Roguelike.Core
{
    // Run-level values survive battle reinitialization; combat piles do not.
    public sealed class RunState : MonoBehaviour
    {
        [SerializeField] private string playerName = "naminal";
        [SerializeField, Min(0)] private int gold = 99;
        [SerializeField, Min(1)] private int floor = 1;
        [SerializeField] private string[] potions = { "", "", "" };
        [SerializeField] private List<string> deck = new List<string>
        {
            "strike", "strike", "strike", "strike", "strike",
            "defend", "defend", "defend", "defend", "bash"
        };
        public string PlayerName => playerName;
        public int Gold => gold;
        public int Floor => floor;
        public IReadOnlyList<string> Deck => deck.AsReadOnly();
        public int PotionSlots => potions.Length;
        public string GetPotion(int slot) => potions[slot];
        public event Action Changed;

        public void SetGold(int value) { gold = Mathf.Max(0, value); Changed?.Invoke(); }
        public void SetFloor(int value) { floor = Mathf.Max(1, value); Changed?.Invoke(); }
        public void SetPotion(int slot, string potion) { potions[slot] = potion ?? ""; Changed?.Invoke(); }
        public void AddCard(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Card ID is required.");
            deck.Add(id);
            Changed?.Invoke();
        }
        public static string CardName(string id)
        {
            switch (id)
            {
                case "strike": return "타격";
                case "defend": return "수비";
                case "bash": return "강타";
                default: return id;
            }
        }
    }
}
