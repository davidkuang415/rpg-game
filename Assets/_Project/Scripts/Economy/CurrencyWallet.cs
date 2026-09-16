using System;
using UnityEngine;

namespace RPG.Economy
{
    public enum CurrencyType
    {
        Gold = 0,
        Gems = 1
    }

    /// <summary>
    /// The player's currencies.
    ///
    /// Gold: equipment upgrades, shop purchases, later enchanting.
    /// Gems: inventory expansion, earned through play (weekly quests later). No real-money
    /// purchases exist or are planned in this codebase.
    ///
    /// All changes go through Add/TrySpend so the UI can react to one event and the save
    /// system has one place to read from. Currency never occupies inventory slots.
    /// </summary>
    public class CurrencyWallet : MonoBehaviour
    {
        [SerializeField, Min(0)] private int startingGold;
        [SerializeField, Min(0)] private int startingGems;
        [SerializeField] private bool logChanges;

        public int Gold { get; private set; }
        public int Gems { get; private set; }

        /// <summary>(currency, newAmount, delta). Delta is negative for spending.</summary>
        public event Action<CurrencyType, int, int> CurrencyChanged;

        private void Awake()
        {
            Gold = startingGold;
            Gems = startingGems;
        }

        public int Get(CurrencyType currency) => currency == CurrencyType.Gold ? Gold : Gems;

        public bool CanAfford(CurrencyType currency, int amount) => Get(currency) >= amount;

        public void Add(CurrencyType currency, int amount)
        {
            if (amount <= 0) return;
            Apply(currency, amount);
        }

        /// <summary>Spends only if the full amount is available. Returns false and changes nothing otherwise.</summary>
        public bool TrySpend(CurrencyType currency, int amount)
        {
            if (amount < 0 || !CanAfford(currency, amount)) return false;
            Apply(currency, -amount);
            return true;
        }

        /// <summary>Used by the save system on load, and by debug tools.</summary>
        public void SetBalances(int gold, int gems)
        {
            Gold = Mathf.Max(0, gold);
            Gems = Mathf.Max(0, gems);
            CurrencyChanged?.Invoke(CurrencyType.Gold, Gold, 0);
            CurrencyChanged?.Invoke(CurrencyType.Gems, Gems, 0);
        }

        private void Apply(CurrencyType currency, int delta)
        {
            int newAmount;
            if (currency == CurrencyType.Gold) newAmount = Gold = Mathf.Max(0, Gold + delta);
            else newAmount = Gems = Mathf.Max(0, Gems + delta);

            if (logChanges) Debug.Log($"[Wallet] {currency} {delta:+#;-#;0} -> {newAmount}", this);
            CurrencyChanged?.Invoke(currency, newAmount, delta);
        }
    }
}
