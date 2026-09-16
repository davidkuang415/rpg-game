using System;
using System.Collections.Generic;
using RPG.Items;

namespace RPG.Save
{
    /// <summary>One equipped item and the slot it occupies.</summary>
    [Serializable]
    public class EquippedItemEntry
    {
        public EquipmentSlot Slot;
        public EquipmentInstance Item;
    }

    /// <summary>
    /// An item waiting in Reward Storage, with the absolute time it expires.
    ///
    /// Stored as a Unix timestamp rather than a remaining duration, so the countdown survives
    /// the game being closed - a requirement the design spec calls out explicitly. Nothing
    /// writes these yet; the field exists so the save format does not change when Reward
    /// Storage ships.
    /// </summary>
    [Serializable]
    public class RewardStorageEntry
    {
        public EquipmentInstance Item;
        public long ClaimedAtUnixSeconds;
        public long ExpiresAtUnixSeconds;
    }

    /// <summary>Player preferences. Placeholders until there is audio to control.</summary>
    [Serializable]
    public class SettingsData
    {
        public float MasterVolume = 1f;
        public float MusicVolume = 0.8f;
        public float SfxVolume = 1f;
        public bool HapticsEnabled = true;
    }

    /// <summary>
    /// The entire save file.
    ///
    /// Two rules shape this type:
    ///
    /// 1. It holds only PLAIN DATA and STABLE IDs - never a ScriptableObject reference. A save
    ///    stores "knight" and "iron_sword", and the registries turn those back into assets on
    ///    load. Assets can then be renamed, moved or rebuilt without breaking saves.
    ///
    /// 2. It is versioned. Version is checked on load and migrated forward, so a format change
    ///    later is a migration step rather than a wiped profile.
    ///
    /// It is deliberately a serializable C# object with no Unity dependencies beyond the item
    /// instances, which is what makes uploading the same JSON to a server later a transport
    /// change rather than a rewrite.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        /// <summary>Bump when the format changes, and add a migration step for the old value.</summary>
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;
        public long LastSavedUnixSeconds;

        // --- Progression ---
        public string ClassId;
        public int PlayerLevel = 1;
        public float CurrentXp;
        public int HighestUnlockedStage = 1;

        // --- Currency ---
        public int Gold;
        public int Gems;

        // --- Inventory ---
        public int InventoryCapacity = 10;
        public int InventoryExpansionsBought;

        public List<EquipmentInstance> InventoryItems = new List<EquipmentInstance>();
        public List<EquippedItemEntry> EquippedItems = new List<EquippedItemEntry>();
        public List<RewardStorageEntry> RewardStorage = new List<RewardStorageEntry>();

        public SettingsData Settings = new SettingsData();

        /// <summary>
        /// Repairs anything a save might be missing - most importantly instance IDs, since a
        /// missing GUID would let two items collide in the inventory.
        /// </summary>
        public void Sanitize()
        {
            InventoryItems ??= new List<EquipmentInstance>();
            EquippedItems ??= new List<EquippedItemEntry>();
            RewardStorage ??= new List<RewardStorageEntry>();
            Settings ??= new SettingsData();

            for (int i = InventoryItems.Count - 1; i >= 0; i--)
            {
                if (InventoryItems[i] == null) InventoryItems.RemoveAt(i);
                else InventoryItems[i].EnsureInstanceId();
            }

            for (int i = EquippedItems.Count - 1; i >= 0; i--)
            {
                if (EquippedItems[i]?.Item == null) EquippedItems.RemoveAt(i);
                else EquippedItems[i].Item.EnsureInstanceId();
            }

            for (int i = RewardStorage.Count - 1; i >= 0; i--)
            {
                if (RewardStorage[i]?.Item == null) RewardStorage.RemoveAt(i);
                else RewardStorage[i].Item.EnsureInstanceId();
            }
        }
    }
}
