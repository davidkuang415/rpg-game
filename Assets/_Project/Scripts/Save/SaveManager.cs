using System;
using System.Collections.Generic;
using UnityEngine;
using RPG.Classes;
using RPG.Economy;
using RPG.Inventory;
using RPG.Items;
using RPG.Player;
using RPG.Progression;
using RPG.Stages;

namespace RPG.Save
{
    /// <summary>
    /// Reads and writes the player's profile.
    ///
    /// It is the one place that knows how a running game maps onto a save file. Every system it
    /// touches already exposes a Set/restore method for exactly this purpose, so loading is
    /// assignment rather than surgery.
    ///
    /// Design rules honoured here:
    ///  - ScriptableObjects hold definitions; this holds instances and IDs
    ///  - the format is versioned and migrated, never silently reinterpreted
    ///  - loaded equipment is re-validated, so an edited or stale save cannot produce an
    ///    illegal loadout (a Knight holding a bow)
    ///  - storage is behind an interface, so cloud saves later are a transport change
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        [Header("Storage")]
        [SerializeField] private string fileName = "profile.json";
        [SerializeField] private bool prettyPrint = true;

        [Header("Systems")]
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private PlayerLevel playerLevel;
        [SerializeField] private CurrencyWallet wallet;
        [SerializeField] private InventoryManager inventory;
        [SerializeField] private EquipmentManager equipment;
        [SerializeField] private StageProgressState stageProgress;

        [Tooltip("Only used to restore the starting bag size on a profile reset.")]
        [SerializeField] private InventoryConfig inventoryConfig;

        [Header("Registries")]
        [SerializeField] private ClassRegistry classRegistry;
        [SerializeField] private ItemRegistry itemRegistry;

        [Header("Autosave")]
        [SerializeField] private StageEventChannel stageEvents;

        [Tooltip("Save whenever a stage is completed.")]
        [SerializeField] private bool saveOnStageComplete = true;

        [Tooltip("Save when the app is backgrounded or quit. Essential on mobile, where a " +
                 "process can be killed without warning.")]
        [SerializeField] private bool saveOnPauseAndQuit = true;

        [SerializeField] private bool logSaves = true;

        private ISaveStorage _storage;

        public bool HasSave => Storage.Exists();
        public string SaveLocation => Storage.Describe();

        /// <summary>Raised after a successful save or load.</summary>
        public event Action Saved;
        public event Action Loaded;

        private ISaveStorage Storage => _storage ??= new LocalFileSaveStorage(fileName);

        private void OnEnable()
        {
            if (stageEvents != null) stageEvents.StageCompleted += OnStageCompleted;
        }

        private void OnDisable()
        {
            if (stageEvents != null) stageEvents.StageCompleted -= OnStageCompleted;
        }

        private void OnStageCompleted(StageData stage)
        {
            if (saveOnStageComplete) Save();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused && saveOnPauseAndQuit) Save();
        }

        private void OnApplicationQuit()
        {
            if (saveOnPauseAndQuit) Save();
        }

        // ------------------------------------------------------------------ save

        public void Save()
        {
            // A profile with no class has never really started, so it is not written out. This
            // is what makes a reset stick: without it, quitting immediately after a wipe would
            // save the still-loaded state straight back over the file we just deleted.
            if (playerStats == null || playerStats.CurrentClass == null)
            {
                if (logSaves) Debug.Log("[Save] Nothing to save yet: no class chosen.", this);
                return;
            }

            SaveData data = Capture();
            string json = JsonUtility.ToJson(data, prettyPrint);

            Storage.Write(json);

            if (logSaves) Debug.Log($"[Save] Wrote profile ({json.Length} bytes) to {Storage.Describe()}", this);
            Saved?.Invoke();
        }

        private SaveData Capture()
        {
            var data = new SaveData
            {
                Version = SaveData.CurrentVersion,
                LastSavedUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            if (playerStats != null && playerStats.CurrentClass != null)
            {
                data.ClassId = playerStats.CurrentClass.ClassId;
            }

            if (playerLevel != null)
            {
                data.PlayerLevel = playerLevel.Level;
                data.CurrentXp = playerLevel.CurrentXp;
            }

            if (stageProgress != null) data.HighestUnlockedStage = stageProgress.HighestUnlockedStage;

            if (wallet != null)
            {
                data.Gold = wallet.Gold;
                data.Gems = wallet.Gems;
            }

            if (inventory != null)
            {
                data.InventoryCapacity = inventory.Capacity;
                data.InventoryExpansionsBought = inventory.ExpansionsBought;
                data.InventoryItems = new List<EquipmentInstance>(inventory.Items);
            }

            if (equipment != null)
            {
                foreach (KeyValuePair<EquipmentSlot, EquipmentInstance> pair in equipment.AllEquipped)
                {
                    if (pair.Value == null) continue;
                    data.EquippedItems.Add(new EquippedItemEntry { Slot = pair.Key, Item = pair.Value });
                }
            }

            // Reward Storage is captured here once that system exists; the list stays empty
            // until then so the format does not change when it arrives.

            return data;
        }

        // ------------------------------------------------------------------ load

        /// <summary>Loads if a save exists. Returns false when starting fresh.</summary>
        public bool LoadIfPresent()
        {
            if (!HasSave) return false;
            return Load();
        }

        public bool Load()
        {
            string json = Storage.Read();
            if (string.IsNullOrEmpty(json)) return false;

            SaveData data;
            try
            {
                data = JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[Save] Profile is corrupt and was not loaded: {exception.Message}", this);
                return false;
            }

            if (data == null) return false;

            if (!Migrate(data)) return false;
            data.Sanitize();

            Apply(data);

            if (logSaves) Debug.Log($"[Save] Loaded profile (v{data.Version}) from {Storage.Describe()}", this);
            Loaded?.Invoke();
            return true;
        }

        /// <summary>
        /// Brings an older save up to the current format. Each version gets its own step, so
        /// a profile can travel forward through several releases in one load.
        /// </summary>
        private bool Migrate(SaveData data)
        {
            if (data.Version > SaveData.CurrentVersion)
            {
                Debug.LogError($"[Save] Profile is version {data.Version}, newer than this build " +
                               $"(v{SaveData.CurrentVersion}). Refusing to load so it is not overwritten.", this);
                return false;
            }

            // while (data.Version < SaveData.CurrentVersion) { switch (data.Version) { ... } }
            // Nothing to migrate yet - version 1 is the first format.

            data.Version = SaveData.CurrentVersion;
            return true;
        }

        private void Apply(SaveData data)
        {
            if (playerStats != null && classRegistry != null && !string.IsNullOrEmpty(data.ClassId))
            {
                ClassData classData = classRegistry.GetById(data.ClassId);
                if (classData != null) playerStats.SetClass(classData);
                else Debug.LogWarning($"[Save] Unknown class id '{data.ClassId}'; keeping the current class.", this);
            }

            if (playerLevel != null) playerLevel.SetProgress(data.PlayerLevel, data.CurrentXp);
            if (stageProgress != null) stageProgress.SetProgress(data.HighestUnlockedStage);
            if (wallet != null) wallet.SetBalances(data.Gold, data.Gems);

            if (inventory != null)
            {
                inventory.SetCapacity(data.InventoryCapacity, data.InventoryExpansionsBought);
                inventory.SetItems(FilterKnownItems(data.InventoryItems));
            }

            RestoreEquipment(data);
        }

        /// <summary>
        /// Drops items whose template no longer exists, so a deleted item asset degrades into
        /// one missing item rather than a broken profile.
        /// </summary>
        private List<EquipmentInstance> FilterKnownItems(List<EquipmentInstance> items)
        {
            var kept = new List<EquipmentInstance>(items.Count);

            for (int i = 0; i < items.Count; i++)
            {
                if (itemRegistry != null && itemRegistry.GetDefinition(items[i]) == null)
                {
                    Debug.LogWarning($"[Save] Dropping item with unknown template " +
                                     $"'{items[i].TemplateId}'.", this);
                    continue;
                }

                kept.Add(items[i]);
            }

            return kept;
        }

        /// <summary>
        /// Restores the loadout, re-running the same validation a live equip would.
        /// Anything that fails - wrong class, retired slot, deleted template - goes to the bag
        /// instead of being equipped or silently lost.
        /// </summary>
        private void RestoreEquipment(SaveData data)
        {
            if (equipment == null) return;

            for (int i = 0; i < data.EquippedItems.Count; i++)
            {
                EquippedItemEntry entry = data.EquippedItems[i];

                ItemDefinition definition = itemRegistry != null
                    ? itemRegistry.GetDefinition(entry.Item)
                    : null;

                if (definition == null)
                {
                    Debug.LogWarning($"[Save] Equipped item '{entry.Item.TemplateId}' is unknown; dropped.", this);
                    continue;
                }

                bool slotValid = equipment.IsSlotSupported(entry.Slot) && definition.Slot == entry.Slot;
                bool classValid = definition is not WeaponDefinition weapon ||
                                  EquipmentManager.IsWeaponAllowedForClass(weapon, playerStats.CurrentClass);

                if (slotValid && classValid)
                {
                    equipment.SetEquippedDirect(entry.Slot, entry.Item);
                    continue;
                }

                Debug.LogWarning($"[Save] '{definition.DisplayName}' is no longer valid in " +
                                 $"{entry.Slot}; moved to the bag.", this);

                if (inventory != null) inventory.ForceAdd(entry.Item);
            }
        }

        // ------------------------------------------------------------------ debug

        public void DeleteSave()
        {
            Storage.Delete();
            if (logSaves) Debug.Log($"[Save] Deleted profile at {Storage.Describe()}", this);
        }

        /// <summary>
        /// A true fresh start: deletes the file AND returns every live system to its day-one
        /// state, so the running session really is at zero rather than merely unsaved.
        ///
        /// DeleteSave alone is not enough - it removes the file while the class, level, wallet
        /// and bag are all still loaded in memory, so play simply carries on and the next
        /// autosave writes them back.
        ///
        /// It does not decide which screen to show next; GameFlowController owns the flow.
        /// </summary>
        public void ResetProfile()
        {
            DeleteSave();

            if (equipment != null)
            {
                // Copied first: unequipping mutates the collection this reads from.
                var slots = new List<EquipmentSlot>(equipment.SupportedSlots);
                for (int i = 0; i < slots.Count; i++) equipment.SetEquippedDirect(slots[i], null);
            }

            if (inventory != null)
            {
                inventory.ClearAll();
                inventory.SetCapacity(InitialInventoryCapacity, 0);
            }

            if (wallet != null) wallet.SetBalances(0, 0);
            if (playerLevel != null) playerLevel.SetProgress(1, 0f);
            if (stageProgress != null) stageProgress.SetProgress(1);

            // Last, because clearing the class recalculates stats and should see an already
            // empty loadout rather than recalculating twice.
            if (playerStats != null) playerStats.ClearClass();

            if (logSaves) Debug.Log("[Save] Profile reset to a fresh start.", this);
        }

        /// <summary>Bag size for a brand new profile, read from the same config the game uses.</summary>
        private int InitialInventoryCapacity =>
            inventoryConfig != null ? inventoryConfig.InitialCapacity : 10;
    }
}
