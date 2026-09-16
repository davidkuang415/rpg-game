using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using RPG.DebugTools;
using RPG.Enemies;
using RPG.Items;
using RPG.Loot;
using RPG.Stages;
using RPG.Stats;

namespace RPG.EditorTools
{
    /// <summary>
    /// Phase 7 setup: the rarity table, a starter set of item templates, the item registry,
    /// loot tables, and the reward collector that banks drops during a run.
    ///
    /// Safe to re-run: existing assets are reused so any tuning survives.
    /// </summary>
    public static class Phase7SetupBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/TestArena.unity";
        private const string ItemFolder = "Assets/_Project/Data/Items";
        private const string LootFolder = "Assets/_Project/Data/Loot";
        private const string EnemyDataFolder = "Assets/_Project/Data/Enemies";

        [MenuItem("RPG/Phase 7/Add Loot System", priority = 140)]
        public static void Setup()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Scene scene = EnsureTestSceneOpen();
            if (!scene.IsValid()) return;

            RarityTable rarityTable = CreateRarityTable();
            List<ItemDefinition> items = CreateItemTemplates();

            var registry = EditorSetupUtility.CreateOrLoadAsset<ItemRegistry>(
                ItemFolder + "/ItemRegistry.asset", out _);
            EditorSetupUtility.SetPrivateObjectList(registry, "items", items.ConvertAll(i => (Object)i));

            LootTableData standardTable = CreateLootTable("LootTable_Standard", items,
                dropChance: 0.28f, guaranteed: false, rolls: 1, minimumRarity: Rarity.Common,
                levelOffsetMin: 0, levelOffsetMax: 1);

            LootTableData eliteTable = CreateLootTable("LootTable_Elite", items,
                dropChance: 0.6f, guaranteed: false, rolls: 1, minimumRarity: Rarity.Uncommon,
                levelOffsetMin: 1, levelOffsetMax: 2);

            // Boss tables guarantee at least an Epic, per the design spec. Nothing uses this
            // until the boss phase, but the data exists so bosses are a content job later.
            CreateLootTable("LootTable_Boss", items,
                dropChance: 1f, guaranteed: true, rolls: 2, minimumRarity: Rarity.Epic,
                levelOffsetMin: 1, levelOffsetMax: 3);

            AssignLootTables(standardTable, eliteTable);

            StageRewardCollector collector = SetUpCollector(rarityTable, standardTable);
            WireDebugOverlay(collector, registry, rarityTable);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("<b>[Phase 7]</b> Loot system installed. Kill enemies and watch the " +
                      "Pending Rewards panel fill. Nothing drops on the floor, by design.");
        }

        private static Scene EnsureTestSceneOpen()
        {
            Scene active = SceneManager.GetActiveScene();
            if (active.path == ScenePath) return active;

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                Debug.LogError($"[Phase 7] {ScenePath} is missing. Run RPG > Phase 1 > Build Test Scene.");
                return default;
            }

            return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        private static RarityTable CreateRarityTable()
        {
            RarityTable table = EditorSetupUtility.CreateOrLoadAsset<RarityTable>(
                ItemFolder + "/RarityTable.asset", out bool created);

            if (created)
            {
                table.ResetToDefaults();
                EditorUtility.SetDirty(table);
            }

            return table;
        }

        // ------------------------------------------------------------------ item templates

        /// <summary>
        /// A starter catalogue. The three swords deliberately have different profiles - heavy,
        /// balanced, fast - so that ATK alone never decides which weapon is best.
        /// </summary>
        private static List<ItemDefinition> CreateItemTemplates()
        {
            var items = new List<ItemDefinition>();

            items.Add(CreateWeapon("IronSword", "iron_sword", "Iron Sword", WeaponType.Sword,
                range: 1.6f, arc: 120f, dropWeight: 10f, baseValue: 12f,
                stats: new[]
                {
                    Stat(StatType.Attack, 6f, 1.2f),
                    Stat(StatType.CritChance, 0.01f, 0.0004f)
                }));

            items.Add(CreateWeapon("Broadsword", "broadsword", "Broadsword", WeaponType.Sword,
                range: 1.9f, arc: 100f, dropWeight: 6f, baseValue: 18f,
                stats: new[]
                {
                    Stat(StatType.Attack, 10f, 1.9f),
                    Stat(StatType.AttackSpeed, -0.25f, 0f),      // hits hard, swings slowly
                    Stat(StatType.CritDamage, 0.15f, 0.004f)
                }));

            items.Add(CreateWeapon("SwiftBlade", "swift_blade", "Swift Blade", WeaponType.Sword,
                range: 1.4f, arc: 130f, dropWeight: 6f, baseValue: 16f,
                stats: new[]
                {
                    Stat(StatType.Attack, 4f, 0.8f),
                    Stat(StatType.AttackSpeed, 0.35f, 0.004f),   // less damage, far more often
                    Stat(StatType.CritChance, 0.02f, 0.0006f)
                }));

            items.Add(CreateWeapon("OakBow", "oak_bow", "Oak Bow", WeaponType.Bow,
                range: 8f, arc: 0f, dropWeight: 10f, baseValue: 12f,
                stats: new[]
                {
                    Stat(StatType.Attack, 5f, 1.0f),
                    Stat(StatType.CritChance, 0.01f, 0.0004f)
                }));

            items.Add(CreateWeapon("HuntersBow", "hunters_bow", "Hunter's Bow", WeaponType.Bow,
                range: 9.5f, arc: 0f, dropWeight: 6f, baseValue: 18f,
                stats: new[]
                {
                    Stat(StatType.Attack, 4f, 0.85f),
                    Stat(StatType.AttackSpeed, 0.3f, 0.003f),
                    Stat(StatType.CritDamage, 0.2f, 0.005f)
                }));

            items.Add(CreateArmor("IronHelmet", "iron_helmet", "Iron Helmet", EquipmentSlot.Helmet,
                dropWeight: 10f, baseValue: 10f,
                stats: new[] { Stat(StatType.MaxHealth, 20f, 4f), Stat(StatType.Defense, 3f, 0.8f) }));

            items.Add(CreateArmor("IronChestplate", "iron_chestplate", "Iron Chestplate",
                EquipmentSlot.Chestplate, dropWeight: 10f, baseValue: 14f,
                stats: new[] { Stat(StatType.MaxHealth, 35f, 7f), Stat(StatType.Defense, 5f, 1.2f) }));

            items.Add(CreateArmor("IronLeggings", "iron_leggings", "Iron Leggings",
                EquipmentSlot.Leggings, dropWeight: 10f, baseValue: 12f,
                stats: new[] { Stat(StatType.MaxHealth, 25f, 5f), Stat(StatType.Defense, 4f, 1f) }));

            // Boots are the only armour that grants movement speed, per the spec.
            items.Add(CreateArmor("IronBoots", "iron_boots", "Iron Boots", EquipmentSlot.Boots,
                dropWeight: 10f, baseValue: 10f,
                stats: new[]
                {
                    Stat(StatType.MaxHealth, 15f, 3f),
                    Stat(StatType.Defense, 2f, 0.6f),
                    Stat(StatType.MoveSpeed, 0.15f, 0.01f)
                }));

            return items;
        }

        private static ItemStatLine Stat(StatType stat, float baseValue, float perLevel) =>
            new ItemStatLine { Stat = stat, BaseValue = baseValue, PerItemLevel = perLevel };

        private static WeaponDefinition CreateWeapon(string assetName, string id, string displayName,
            WeaponType weaponType, float range, float arc, float dropWeight, float baseValue,
            ItemStatLine[] stats)
        {
            WeaponDefinition weapon = EditorSetupUtility.CreateOrLoadAsset<WeaponDefinition>(
                $"{ItemFolder}/Weapons/{assetName}.asset", out bool created);

            if (!created) return weapon;

            EditorSetupUtility.SetPrivateField(weapon, "itemId", id);
            EditorSetupUtility.SetPrivateField(weapon, "displayName", displayName);
            EditorSetupUtility.SetPrivateField(weapon, "weaponType", weaponType);
            EditorSetupUtility.SetPrivateField(weapon, "range", range);
            EditorSetupUtility.SetPrivateField(weapon, "arcDegrees", arc);
            EditorSetupUtility.SetPrivateField(weapon, "dropWeight", dropWeight);
            EditorSetupUtility.SetPrivateField(weapon, "baseValue", baseValue);
            WriteStatLines(weapon, stats);

            return weapon;
        }

        private static ArmorDefinition CreateArmor(string assetName, string id, string displayName,
            EquipmentSlot slot, float dropWeight, float baseValue, ItemStatLine[] stats)
        {
            ArmorDefinition armor = EditorSetupUtility.CreateOrLoadAsset<ArmorDefinition>(
                $"{ItemFolder}/Armor/{assetName}.asset", out bool created);

            if (!created) return armor;

            EditorSetupUtility.SetPrivateField(armor, "itemId", id);
            EditorSetupUtility.SetPrivateField(armor, "displayName", displayName);
            EditorSetupUtility.SetPrivateField(armor, "slot", slot);
            EditorSetupUtility.SetPrivateField(armor, "dropWeight", dropWeight);
            EditorSetupUtility.SetPrivateField(armor, "baseValue", baseValue);
            WriteStatLines(armor, stats);

            return armor;
        }

        /// <summary>Writes the stat list, which is an array of a nested serializable struct.</summary>
        private static void WriteStatLines(ItemDefinition definition, ItemStatLine[] lines)
        {
            var serialized = new SerializedObject(definition);
            SerializedProperty list = serialized.FindProperty("stats");
            list.ClearArray();

            for (int i = 0; i < lines.Length; i++)
            {
                list.InsertArrayElementAtIndex(i);
                SerializedProperty element = list.GetArrayElementAtIndex(i);

                element.FindPropertyRelative("Stat").intValue = (int)lines[i].Stat;
                element.FindPropertyRelative("BaseValue").floatValue = lines[i].BaseValue;
                element.FindPropertyRelative("PerItemLevel").floatValue = lines[i].PerItemLevel;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
        }

        // ------------------------------------------------------------------ loot tables

        private static LootTableData CreateLootTable(string assetName, List<ItemDefinition> items,
            float dropChance, bool guaranteed, int rolls, Rarity minimumRarity,
            int levelOffsetMin, int levelOffsetMax)
        {
            LootTableData table = EditorSetupUtility.CreateOrLoadAsset<LootTableData>(
                $"{LootFolder}/{assetName}.asset", out bool created);

            if (!created) return table;

            EditorSetupUtility.SetPrivateField(table, "dropChance", dropChance);
            EditorSetupUtility.SetPrivateField(table, "guaranteedDrop", guaranteed);
            EditorSetupUtility.SetPrivateField(table, "rollCount", rolls);
            EditorSetupUtility.SetPrivateField(table, "minimumRarity", minimumRarity);
            EditorSetupUtility.SetPrivateField(table, "itemLevelOffsetMin", levelOffsetMin);
            EditorSetupUtility.SetPrivateField(table, "itemLevelOffsetMax", levelOffsetMax);
            EditorSetupUtility.SetPrivateObjectList(table, "possibleItems",
                items.ConvertAll(i => (Object)i));

            return table;
        }

        private static void AssignLootTables(LootTableData standard, LootTableData elite)
        {
            Assign("MeleeGrunt", standard);
            Assign("RangedSlinger", standard);
            Assign("TankBrute", elite);   // tougher enemy, better odds

            void Assign(string dataName, LootTableData table)
            {
                var data = AssetDatabase.LoadAssetAtPath<EnemyData>($"{EnemyDataFolder}/{dataName}.asset");
                if (data == null) return;

                EditorSetupUtility.SetPrivateField(data, "lootTable", table);
            }
        }

        // ------------------------------------------------------------------ scene wiring

        private static StageRewardCollector SetUpCollector(RarityTable rarityTable, LootTableData fallback)
        {
            GameObject systems = GameObject.Find("GameSystems");
            if (systems == null) systems = new GameObject("GameSystems");

            var collector = EditorSetupUtility.EnsureComponent<StageRewardCollector>(systems);

            var enemyEvents = AssetDatabase.LoadAssetAtPath<EnemyEventChannel>(
                EnemyDataFolder + "/EnemyEventChannel.asset");
            var stageEvents = AssetDatabase.LoadAssetAtPath<StageEventChannel>(
                "Assets/_Project/Data/Stages/StageEventChannel.asset");

            EditorSetupUtility.SetPrivateField(collector, "enemyEvents", enemyEvents);
            EditorSetupUtility.SetPrivateField(collector, "stageEvents", stageEvents);
            EditorSetupUtility.SetPrivateField(collector, "rarityTable", rarityTable);
            EditorSetupUtility.SetPrivateField(collector, "fallbackLootTable", fallback);

            return collector;
        }

        private static void WireDebugOverlay(StageRewardCollector collector, ItemRegistry registry,
            RarityTable rarityTable)
        {
            GameObject devTools = GameObject.Find("DevTools");
            if (devTools == null) return;

            var overlay = devTools.GetComponent<StatsDebugOverlay>();
            if (overlay == null) return;

            EditorSetupUtility.SetPrivateField(overlay, "rewardCollector", collector);
            EditorSetupUtility.SetPrivateField(overlay, "itemRegistry", registry);
            EditorSetupUtility.SetPrivateField(overlay, "rarityTable", rarityTable);
        }
    }
}
