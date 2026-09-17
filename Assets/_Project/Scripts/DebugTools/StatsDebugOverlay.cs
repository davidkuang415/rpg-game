using System.Collections.Generic;
using UnityEngine;
using RPG.Core.Combat;
using RPG.Economy;
using RPG.Inventory;
using RPG.Player;
using RPG.Items;
using RPG.Loot;
using RPG.Progression;
using RPG.Save;
using RPG.Stages;
using RPG.Stats;
using RPG.UI;

namespace RPG.DebugTools
{
    /// <summary>
    /// Development-only stat readout drawn with IMGUI.
    ///
    /// IMGUI is used deliberately: it needs no prefabs, fonts or canvas wiring, so a debug
    /// tool can never break the real game UI or accidentally ship as part of it. The actual
    /// player-facing HUD will be built with uGUI in the polish phase.
    /// </summary>
    public class StatsDebugOverlay : MonoBehaviour
    {
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private Health playerHealth;
        [SerializeField] private PlayerLevel playerLevel;
        [SerializeField] private ClassSelectionPanel classSelectionPanel;
        [SerializeField] private StageManager stageManager;

        [Header("Inventory")]
        [SerializeField] private InventoryManager inventory;
        [SerializeField] private CurrencyWallet wallet;
        [SerializeField] private RewardClaimer rewardClaimer;
        [SerializeField] private HubScreen hubScreen;
        [SerializeField] private SaveManager saveManager;

        [Header("Loot")]
        [SerializeField] private StageRewardCollector rewardCollector;
        [SerializeField] private ItemRegistry itemRegistry;
        [SerializeField] private RarityTable rarityTable;
        [Tooltip("Off by default: the player-facing stats now live on the hub's gear page, " +
                 "and this overlay is a development tool that should not sit over the arena.")]
        [SerializeField] private bool startVisible;

        private bool _visible;
        private GUIStyle _labelStyle;
        private GUIStyle _headerStyle;

        private void Awake() => _visible = startVisible;

        private void OnGUI()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            EnsureStyles();

            // Scale the overlay up on high-density screens so it stays readable on a phone.
            float scale = Mathf.Max(1f, Screen.height / 900f);
            Matrix4x4 previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));

            if (GUI.Button(new Rect(10f, 10f, 90f, 30f), _visible ? "Hide Stats" : "Stats"))
            {
                _visible = !_visible;
            }

            if (_visible) DrawPanel();

            GUI.matrix = previousMatrix;
#endif
        }

        private void DrawPanel()
        {
            GUILayout.BeginArea(new Rect(10f, 50f, 260f, 500f), GUI.skin.box);

            if (playerStats == null)
            {
                GUILayout.Label("No PlayerStats assigned.", _labelStyle);
                GUILayout.EndArea();
                return;
            }

            string className = playerStats.CurrentClass != null
                ? playerStats.CurrentClass.DisplayName
                : "(no class selected)";

            GUILayout.Label($"Class: {className}", _headerStyle);

            if (playerHealth != null)
            {
                GUILayout.Label($"HP: {playerHealth.CurrentHealth:0} / {playerHealth.MaxHealth:0}",
                    _headerStyle);
            }

            if (playerLevel != null)
            {
                string xpText = playerLevel.IsMaxLevel
                    ? "MAX"
                    : $"{playerLevel.CurrentXp:0} / {playerLevel.XpForNextLevel:0}";
                GUILayout.Label($"Level {playerLevel.Level}   XP {xpText}", _headerStyle);
            }

            GUILayout.Space(4f);

            StatBlock stats = playerStats.Current;
            for (int i = 0; i < StatTypeInfo.Count; i++)
            {
                var stat = (StatType)i;
                GUILayout.Label($"{StatTypeInfo.DisplayName(stat)}: {StatTypeInfo.Format(stat, stats[stat])}",
                    _labelStyle);
            }

            GUILayout.Space(6f);

            // Lets damage-taking, healing and (later) life steal be exercised before enemies
            // can actually hurt the player.
            if (playerHealth != null)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Hurt 10"))
                {
                    playerHealth.TakeDamage(new DamageInfo(
                        10f, false, null, playerHealth.transform.position, Vector2.zero,
                        canLifeSteal: false));
                }
                if (GUILayout.Button("Full Heal")) playerHealth.ResetToFull();
                GUILayout.EndHorizontal();
            }

            if (playerLevel != null)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("+50 XP")) playerLevel.AddXp(50f);
                if (GUILayout.Button("+500 XP")) playerLevel.AddXp(500f);
                GUILayout.EndHorizontal();
            }

            if (stageManager != null && stageManager.IsStageActive)
            {
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Clear Stage")) stageManager.ForceCompleteCurrentStage();
                if (GUILayout.Button("Restart")) stageManager.RestartCurrentStage();
                GUILayout.EndHorizontal();
            }

            // Proves the spec rule that an account is never locked to one class.
            if (classSelectionPanel != null && !classSelectionPanel.IsOpen &&
                GUILayout.Button("Change Class"))
            {
                classSelectionPanel.Show();
            }

            GUILayout.EndArea();

            DrawPendingRewards();
        }

        /// <summary>
        /// Shows what the current run has banked but not yet claimed. Proves the spec rule that
        /// equipment never drops on the floor - it accumulates here until the stage ends.
        /// </summary>
        private void DrawPendingRewards()
        {
            if (rewardCollector == null) return;

            GUILayout.BeginArea(new Rect(280f, 50f, 300f, 560f), GUI.skin.box);

            StageRewards pending = rewardCollector.Pending;
            GUILayout.Label("Pending Rewards", _headerStyle);
            GUILayout.Label($"Kills {pending.EnemiesDefeated}   XP {pending.XpEarned:0}   " +
                            $"Gold {pending.GoldEarned}", _labelStyle);
            GUILayout.Space(4f);

            IReadOnlyList<EquipmentInstance> items = pending.Items;
            int firstShown = Mathf.Max(0, items.Count - 8);

            for (int i = firstShown; i < items.Count; i++)
            {
                EquipmentInstance item = items[i];
                ItemDefinition definition = itemRegistry != null ? itemRegistry.GetDefinition(item) : null;

                Color previous = GUI.color;
                if (rarityTable != null) GUI.color = rarityTable.GetColor(item.Rarity);

                GUILayout.Label(EquipmentStatCalculator.GetDisplayName(item, definition, rarityTable),
                    _labelStyle);

                GUI.color = previous;
            }

            if (items.Count > 8) GUILayout.Label($"...and {items.Count - 8} more", _labelStyle);

            GUILayout.Space(6f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Roll Item")) rewardCollector.DebugGenerate(10);
            if (GUILayout.Button("Discard")) rewardCollector.ClaimAll();
            if (rewardClaimer != null && GUILayout.Button("Claim")) rewardClaimer.ClaimPending();
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);
            GUILayout.Label("Account", _headerStyle);

            if (inventory != null)
            {
                GUILayout.Label($"Bag {inventory.Count} / {inventory.Capacity}", _labelStyle);
            }

            if (wallet != null)
            {
                GUILayout.Label($"Gold {wallet.Gold}   Gems {wallet.Gems}", _labelStyle);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("+500 Gold")) wallet.Add(CurrencyType.Gold, 500);
                if (GUILayout.Button("+100 Gems")) wallet.Add(CurrencyType.Gems, 100);
                GUILayout.EndHorizontal();
            }

            GUILayout.BeginHorizontal();
            if (hubScreen != null && !hubScreen.IsOpen && GUILayout.Button("Open Hub"))
            {
                // Page 2 is the bag. Opening straight to it keeps the debug shortcut as short
                // as it was before the hub existed.
                hubScreen.ShowOnPage(2);
            }
            if (inventory != null && GUILayout.Button("Clear Bag")) inventory.ClearAll();
            GUILayout.EndHorizontal();

            if (saveManager != null)
            {
                GUILayout.Space(6f);
                GUILayout.Label("Profile", _headerStyle);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Save")) saveManager.Save();
                if (GUILayout.Button("Load")) saveManager.Load();
                if (GUILayout.Button("Wipe")) saveManager.DeleteSave();
                GUILayout.EndHorizontal();
            }

            GUILayout.EndArea();
        }

        private void EnsureStyles()
        {
            if (_labelStyle != null) return;

            _labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 13 };
            _headerStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold };
        }
    }
}
