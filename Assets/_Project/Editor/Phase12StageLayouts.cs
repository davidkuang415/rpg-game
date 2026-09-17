using UnityEngine;
using RPG.Enemies;
using RPG.Stages;

namespace RPG.EditorTools
{
    /// <summary>
    /// The ten handcrafted stages, as code that places rooms, walls, spawns and doors.
    ///
    /// Stages 1 and 2 are the Phase 6 layouts, rebuilt here with the Phase 12 look so every
    /// stage is drawn the same way. 3 to 9 are new, each built around one idea (a corridor,
    /// pillars, a switchback...), and 10 is the first boss arena.
    ///
    /// Each method returns the saved prefab. The matching StageData assets are created by
    /// Phase12SetupBuilder, which is also where the enemy level and reward modifiers live.
    /// </summary>
    public static class Phase12StageLayouts
    {
        public struct Enemies
        {
            public EnemyData Grunt, Slinger, Brute, Boss;
        }

        // ------------------------------------------------------------------ 1: arena

        public static GameObject Stage01(Enemies e)
        {
            var s = StageKit.Begin("Stage_01_Arena", new Vector2(0f, -16f));
            var size = new Vector2(50f, 50f);

            StageKit.Floor(s, Vector2.zero, size);
            StageKit.Border(s, Vector2.zero, size);
            StageKit.Wall(s, "Wall_Inner_A", new Vector2(6f, 3f), new Vector2(1f, 12f));
            StageKit.Wall(s, "Wall_Inner_B", new Vector2(-8f, -6f), new Vector2(14f, 1f));

            RoomController room = StageKit.Room(s, "Room_Main", isStart: true, isFinal: true);
            StageKit.Waves(room,
                StageKit.Wave("Wave 1", 0f,
                    StageKit.Spawn(room, "Spawn_Grunt_1", new Vector2(-6f, 3f), e.Grunt),
                    StageKit.Spawn(room, "Spawn_Grunt_2", new Vector2(-8f, -2f), e.Grunt),
                    StageKit.Spawn(room, "Spawn_Grunt_3", new Vector2(-3f, 9f), e.Grunt, 1),
                    StageKit.Spawn(room, "Spawn_Slinger_1", new Vector2(9f, 3f), e.Slinger),
                    StageKit.Spawn(room, "Spawn_Slinger_2", new Vector2(2f, 11f), e.Slinger)));

            return StageKit.Finish(s, "Stage_01_Arena", size);
        }

        // ------------------------------------------------------------------ 2: two rooms

        public static GameObject Stage02(Enemies e)
        {
            var s = StageKit.Begin("Stage_02_TwoRooms", new Vector2(-20f, 0f));
            var size = new Vector2(50f, 26f);

            StageKit.Floor(s, Vector2.zero, size);
            StageKit.Border(s, Vector2.zero, size);

            // Dividing wall with a gap in the middle for the door.
            StageKit.Wall(s, "Divider_Top", new Vector2(0f, 7.5f), new Vector2(1f, 11f));
            StageKit.Wall(s, "Divider_Bottom", new Vector2(0f, -7.5f), new Vector2(1f, 11f));

            // Cover inside each room, so line of sight matters on both sides.
            StageKit.Wall(s, "Cover_A", new Vector2(-14f, 2f), new Vector2(1f, 8f));
            StageKit.Wall(s, "Cover_B", new Vector2(13f, -3f), new Vector2(9f, 1f));

            RoomController a = StageKit.Room(s, "Room_A", isStart: true);
            StageKit.Waves(a,
                StageKit.Wave("Wave 1", 0f,
                    StageKit.Spawn(a, "A_Grunt_1", new Vector2(-8f, 6f), e.Grunt),
                    StageKit.Spawn(a, "A_Grunt_2", new Vector2(-8f, -6f), e.Grunt),
                    StageKit.Spawn(a, "A_Slinger_1", new Vector2(-16f, 8f), e.Slinger)));

            RoomController b = StageKit.Room(s, "Room_B", isFinal: true);
            StageKit.Waves(b,
                StageKit.Wave("Wave 1", 0f,
                    StageKit.Spawn(b, "B_Slinger_1", new Vector2(10f, 8f), e.Slinger),
                    StageKit.Spawn(b, "B_Slinger_2", new Vector2(18f, -6f), e.Slinger)),
                StageKit.Wave("Wave 2 (delayed)", 1.5f,
                    StageKit.Spawn(b, "B_Brute", new Vector2(20f, 0f), e.Brute),
                    StageKit.Spawn(b, "B_Grunt_1", new Vector2(14f, 5f), e.Grunt, 1),
                    StageKit.Spawn(b, "B_Grunt_2", new Vector2(14f, -5f), e.Grunt, 1)));

            StageKit.Door(a, b, "Door_A_to_B", new Vector2(0f, 0f), new Vector2(3f, 4f));

            return StageKit.Finish(s, "Stage_02_TwoRooms", size);
        }

        // ------------------------------------------------------------------ 3: corridor

        /// <summary>Three rooms in a row. Teaches that clearing a room opens the next door.</summary>
        public static GameObject Stage03(Enemies e)
        {
            var s = StageKit.Begin("Stage_03_Corridor", new Vector2(-28f, 0f));
            var size = new Vector2(66f, 22f);

            StageKit.Floor(s, Vector2.zero, size);
            StageKit.Border(s, Vector2.zero, size);

            // Two dividers, each with a doorway in the middle.
            foreach (float x in new[] { -11f, 11f })
            {
                StageKit.Wall(s, $"Divider_{x}_Top", new Vector2(x, 6.5f), new Vector2(1f, 9f));
                StageKit.Wall(s, $"Divider_{x}_Bottom", new Vector2(x, -6.5f), new Vector2(1f, 9f));
            }

            StageKit.Wall(s, "Cover_B1", new Vector2(-3f, 4f), new Vector2(1f, 6f));
            StageKit.Wall(s, "Cover_B2", new Vector2(4f, -4f), new Vector2(1f, 6f));
            StageKit.Wall(s, "Cover_C", new Vector2(22f, 0f), new Vector2(6f, 1f));

            RoomController a = StageKit.Room(s, "Room_A", isStart: true);
            StageKit.Waves(a,
                StageKit.Wave("Wave 1", 0f,
                    StageKit.Spawn(a, "A_Grunt_1", new Vector2(-18f, 5f), e.Grunt),
                    StageKit.Spawn(a, "A_Grunt_2", new Vector2(-18f, -5f), e.Grunt),
                    StageKit.Spawn(a, "A_Slinger", new Vector2(-14f, 0f), e.Slinger)));

            RoomController b = StageKit.Room(s, "Room_B");
            StageKit.Waves(b,
                StageKit.Wave("Wave 1", 0f,
                    StageKit.Spawn(b, "B_Slinger_1", new Vector2(-1f, 6f), e.Slinger),
                    StageKit.Spawn(b, "B_Slinger_2", new Vector2(6f, -6f), e.Slinger)),
                StageKit.Wave("Wave 2", 1f,
                    StageKit.Spawn(b, "B_Grunt_1", new Vector2(7f, 5f), e.Grunt, 1),
                    StageKit.Spawn(b, "B_Grunt_2", new Vector2(7f, -3f), e.Grunt, 1)));

            RoomController c = StageKit.Room(s, "Room_C", isFinal: true);
            StageKit.Waves(c,
                StageKit.Wave("Wave 1", 0f,
                    StageKit.Spawn(c, "C_Brute", new Vector2(27f, 0f), e.Brute),
                    StageKit.Spawn(c, "C_Grunt_1", new Vector2(20f, 6f), e.Grunt),
                    StageKit.Spawn(c, "C_Grunt_2", new Vector2(20f, -6f), e.Grunt),
                    StageKit.Spawn(c, "C_Slinger", new Vector2(29f, 7f), e.Slinger)));

            StageKit.Door(a, b, "Door_A_to_B", new Vector2(-11f, 0f), new Vector2(3f, 4f));
            StageKit.Door(b, c, "Door_B_to_C", new Vector2(11f, 0f), new Vector2(3f, 4f));

            return StageKit.Finish(s, "Stage_03_Corridor", size);
        }

        // ------------------------------------------------------------------ 4: pillars

        /// <summary>One room, four pillars, three waves. Cover works for both sides.</summary>
        public static GameObject Stage04(Enemies e)
        {
            var s = StageKit.Begin("Stage_04_Pillars", new Vector2(0f, -17f));
            var size = new Vector2(46f, 46f);

            StageKit.Floor(s, Vector2.zero, size);
            StageKit.Border(s, Vector2.zero, size);

            foreach (Vector2 p in new[] { new Vector2(-10f, -10f), new Vector2(10f, -10f),
                                          new Vector2(-10f, 10f), new Vector2(10f, 10f) })
            {
                StageKit.Wall(s, $"Pillar_{p.x}_{p.y}", p, new Vector2(4f, 4f));
            }

            RoomController room = StageKit.Room(s, "Room_Main", isStart: true, isFinal: true);
            StageKit.Waves(room,
                StageKit.Wave("Wave 1", 0f,
                    StageKit.Spawn(room, "W1_Grunt_1", new Vector2(-14f, 4f), e.Grunt),
                    StageKit.Spawn(room, "W1_Grunt_2", new Vector2(14f, 4f), e.Grunt),
                    StageKit.Spawn(room, "W1_Grunt_3", new Vector2(0f, 14f), e.Grunt),
                    StageKit.Spawn(room, "W1_Slinger", new Vector2(0f, 18f), e.Slinger)),
                StageKit.Wave("Wave 2", 1f,
                    StageKit.Spawn(room, "W2_Slinger_1", new Vector2(-17f, 17f), e.Slinger),
                    StageKit.Spawn(room, "W2_Slinger_2", new Vector2(17f, 17f), e.Slinger),
                    StageKit.Spawn(room, "W2_Grunt_1", new Vector2(-6f, 0f), e.Grunt, 1),
                    StageKit.Spawn(room, "W2_Grunt_2", new Vector2(6f, 0f), e.Grunt, 1)),
                StageKit.Wave("Wave 3", 1.5f,
                    StageKit.Spawn(room, "W3_Brute", new Vector2(0f, 12f), e.Brute),
                    StageKit.Spawn(room, "W3_Grunt_1", new Vector2(-16f, -14f), e.Grunt),
                    StageKit.Spawn(room, "W3_Grunt_2", new Vector2(16f, -14f), e.Grunt)));

            return StageKit.Finish(s, "Stage_04_Pillars", size);
        }

        // ------------------------------------------------------------------ 5: crossroads

        /// <summary>An L of three rooms around a solid block. Doors turn the corner.</summary>
        public static GameObject Stage05(Enemies e)
        {
            var s = StageKit.Begin("Stage_05_Crossroads", new Vector2(-20f, -20f));
            var size = new Vector2(54f, 54f);

            StageKit.Floor(s, Vector2.zero, size);
            StageKit.Border(s, Vector2.zero, size);

            // The top-left quarter is solid rock. Rooms: A bottom-left, B bottom-right, C top-right.
            StageKit.Wall(s, "Block_TopLeft", new Vector2(-13.5f, 13.5f), new Vector2(27f, 27f));

            // A | B divider (vertical at x = 0, below the block), door in the middle.
            StageKit.Wall(s, "Divider_AB_Top", new Vector2(0f, -5.75f), new Vector2(1f, 11.5f));
            StageKit.Wall(s, "Divider_AB_Bottom", new Vector2(0f, -21.25f), new Vector2(1f, 11.5f));

            // B / C divider (horizontal at y = 0, right of the block), door in the middle.
            StageKit.Wall(s, "Divider_BC_Left", new Vector2(5.75f, 0f), new Vector2(11.5f, 1f));
            StageKit.Wall(s, "Divider_BC_Right", new Vector2(21.25f, 0f), new Vector2(11.5f, 1f));

            StageKit.Wall(s, "Cover_A", new Vector2(-14f, -8f), new Vector2(6f, 1f));
            StageKit.Wall(s, "Cover_B", new Vector2(14f, -14f), new Vector2(1f, 7f));
            StageKit.Wall(s, "Cover_C1", new Vector2(8f, 14f), new Vector2(1f, 7f));
            StageKit.Wall(s, "Cover_C2", new Vector2(19f, 8f), new Vector2(7f, 1f));

            RoomController a = StageKit.Room(s, "Room_A", isStart: true);
            StageKit.Waves(a,
                StageKit.Wave("Wave 1", 0f,
                    StageKit.Spawn(a, "A_Grunt_1", new Vector2(-8f, -6f), e.Grunt),
                    StageKit.Spawn(a, "A_Grunt_2", new Vector2(-20f, -6f), e.Grunt),
                    StageKit.Spawn(a, "A_Slinger", new Vector2(-14f, -4f), e.Slinger),
                    StageKit.Spawn(a, "A_Grunt_3", new Vector2(-6f, -22f), e.Grunt, 1)));

            RoomController b = StageKit.Room(s, "Room_B");
            StageKit.Waves(b,
                StageKit.Wave("Wave 1", 0f,
                    StageKit.Spawn(b, "B_Slinger_1", new Vector2(20f, -6f), e.Slinger),
                    StageKit.Spawn(b, "B_Slinger_2", new Vector2(20f, -22f), e.Slinger),
                    StageKit.Spawn(b, "B_Grunt_1", new Vector2(8f, -20f), e.Grunt, 1)),
                StageKit.Wave("Wave 2", 1.2f,
                    StageKit.Spawn(b, "B_Brute", new Vector2(14f, -6f), e.Brute),
                    StageKit.Spawn(b, "B_Grunt_2", new Vector2(22f, -14f), e.Grunt, 1)));

            RoomController c = StageKit.Room(s, "Room_C", isFinal: true);
            StageKit.Waves(c,
                StageKit.Wave("Wave 1", 0f,
                    StageKit.Spawn(c, "C_Grunt_1", new Vector2(6f, 20f), e.Grunt, 1),
                    StageKit.Spawn(c, "C_Grunt_2", new Vector2(22f, 20f), e.Grunt, 1),
                    StageKit.Spawn(c, "C_Slinger_1", new Vector2(14f, 22f), e.Slinger),
                    StageKit.Spawn(c, "C_Slinger_2", new Vector2(23f, 4f), e.Slinger, 1)),
                StageKit.Wave("Wave 2", 1.5f,
                    StageKit.Spawn(c, "C_Brute_1", new Vector2(8f, 8f), e.Brute),
                    StageKit.Spawn(c, "C_Brute_2", new Vector2(20f, 14f), e.Brute, 1)));

            StageKit.Door(a, b, "Door_A_to_B", new Vector2(0f, -13.5f), new Vector2(3f, 4f));
            StageKit.Door(b, c, "Door_B_to_C", new Vector2(13.5f, 0f), new Vector2(4f, 3f));

            return StageKit.Finish(s, "Stage_05_Crossroads", size);
        }

        // ------------------------------------------------------------------ 6: gauntlet

        /// <summary>A tall run of three rooms, each with slingers dug in behind cover.</summary>
        public static GameObject Stage06(Enemies e)
        {
            var s = StageKit.Begin("Stage_06_Gauntlet", new Vector2(0f, -32f));
            var size = new Vector2(26f, 72f);

            StageKit.Floor(s, Vector2.zero, size);
            StageKit.Border(s, Vector2.zero, size);

            foreach (float y in new[] { -12f, 12f })
            {
                StageKit.Wall(s, $"Divider_{y}_Left", new Vector2(-7.5f, y), new Vector2(11f, 1f));
                StageKit.Wall(s, $"Divider_{y}_Right", new Vector2(7.5f, y), new Vector2(11f, 1f));
            }

            // Slinger nests: short walls the player has to walk around.
            StageKit.Wall(s, "Nest_A", new Vector2(-5f, -20f), new Vector2(6f, 1f));
            StageKit.Wall(s, "Nest_B1", new Vector2(6f, -2f), new Vector2(5f, 1f));
            StageKit.Wall(s, "Nest_B2", new Vector2(-6f, 5f), new Vector2(5f, 1f));
            StageKit.Wall(s, "Nest_C1", new Vector2(-5f, 22f), new Vector2(6f, 1f));
            StageKit.Wall(s, "Nest_C2", new Vector2(6f, 27f), new Vector2(6f, 1f));

            RoomController a = StageKit.Room(s, "Room_A", isStart: true);
            StageKit.Waves(a,
                StageKit.Wave("Wave 1", 0f,
                    StageKit.Spawn(a, "A_Slinger", new Vector2(-5f, -17f), e.Slinger),
                    StageKit.Spawn(a, "A_Grunt_1", new Vector2(7f, -18f), e.Grunt),
                    StageKit.Spawn(a, "A_Grunt_2", new Vector2(-9f, -28f), e.Grunt)));

            RoomController b = StageKit.Room(s, "Room_B");
            StageKit.Waves(b,
                StageKit.Wave("Wave 1", 0f,
                    StageKit.Spawn(b, "B_Slinger_1", new Vector2(6f, 1f), e.Slinger),
                    StageKit.Spawn(b, "B_Slinger_2", new Vector2(-6f, 8f), e.Slinger),
                    StageKit.Spawn(b, "B_Grunt_1", new Vector2(0f, -8f), e.Grunt, 1),
                    StageKit.Spawn(b, "B_Grunt_2", new Vector2(9f, 9f), e.Grunt, 1)));

            RoomController c = StageKit.Room(s, "Room_C", isFinal: true);
            StageKit.Waves(c,
                StageKit.Wave("Wave 1", 0f,
                    StageKit.Spawn(c, "C_Slinger_1", new Vector2(-5f, 25f), e.Slinger),
                    StageKit.Spawn(c, "C_Slinger_2", new Vector2(6f, 30f), e.Slinger, 1),
                    StageKit.Spawn(c, "C_Grunt_1", new Vector2(8f, 17f), e.Grunt, 1)),
                StageKit.Wave("Wave 2", 1.2f,
                    StageKit.Spawn(c, "C_Brute", new Vector2(0f, 32f), e.Brute),
                    StageKit.Spawn(c, "C_Grunt_2", new Vector2(-9f, 16f), e.Grunt, 1)));

            StageKit.Door(a, b, "Door_A_to_B", new Vector2(0f, -12f), new Vector2(4f, 3f));
            StageKit.Door(b, c, "Door_B_to_C", new Vector2(0f, 12f), new Vector2(4f, 3f));

            return StageKit.Finish(s, "Stage_06_Gauntlet", size);
        }

        // ------------------------------------------------------------------ 7: ambush

        /// <summary>One open room with a ring of cover; the waves keep coming and get heavier.</summary>
        public static GameObject Stage07(Enemies e)
        {
            var s = StageKit.Begin("Stage_07_Ambush", new Vector2(0f, 0f));
            var size = new Vector2(50f, 50f);

            StageKit.Floor(s, Vector2.zero, size);
            StageKit.Border(s, Vector2.zero, size);

            StageKit.Wall(s, "Ring_N", new Vector2(0f, 9f), new Vector2(10f, 1f));
            StageKit.Wall(s, "Ring_S", new Vector2(0f, -9f), new Vector2(10f, 1f));
            StageKit.Wall(s, "Ring_E", new Vector2(9f, 0f), new Vector2(1f, 10f));
            StageKit.Wall(s, "Ring_W", new Vector2(-9f, 0f), new Vector2(1f, 10f));

            foreach (Vector2 p in new[] { new Vector2(-17f, -17f), new Vector2(17f, -17f),
                                          new Vector2(-17f, 17f), new Vector2(17f, 17f) })
            {
                StageKit.Wall(s, $"Corner_{p.x}_{p.y}", p, new Vector2(3f, 3f));
            }

            RoomController room = StageKit.Room(s, "Room_Main", isStart: true, isFinal: true);
            StageKit.Waves(room,
                StageKit.Wave("Wave 1", 0f,
                    StageKit.Spawn(room, "W1_Grunt_1", new Vector2(-20f, 0f), e.Grunt),
                    StageKit.Spawn(room, "W1_Grunt_2", new Vector2(20f, 0f), e.Grunt),
                    StageKit.Spawn(room, "W1_Grunt_3", new Vector2(0f, 20f), e.Grunt),
                    StageKit.Spawn(room, "W1_Grunt_4", new Vector2(0f, -20f), e.Grunt)),
                StageKit.Wave("Wave 2", 0.8f,
                    StageKit.Spawn(room, "W2_Slinger_1", new Vector2(-20f, 20f), e.Slinger),
                    StageKit.Spawn(room, "W2_Slinger_2", new Vector2(20f, 20f), e.Slinger),
                    StageKit.Spawn(room, "W2_Slinger_3", new Vector2(20f, -20f), e.Slinger),
                    StageKit.Spawn(room, "W2_Grunt_1", new Vector2(-20f, -20f), e.Grunt, 1),
                    StageKit.Spawn(room, "W2_Grunt_2", new Vector2(-14f, 6f), e.Grunt, 1)),
                StageKit.Wave("Wave 3", 1.2f,
                    StageKit.Spawn(room, "W3_Brute_1", new Vector2(-20f, 0f), e.Brute),
                    StageKit.Spawn(room, "W3_Brute_2", new Vector2(20f, 0f), e.Brute),
                    StageKit.Spawn(room, "W3_Grunt_1", new Vector2(0f, 20f), e.Grunt, 2),
                    StageKit.Spawn(room, "W3_Grunt_2", new Vector2(0f, -20f), e.Grunt, 2),
                    StageKit.Spawn(room, "W3_Slinger", new Vector2(20f, 20f), e.Slinger, 1)));

            return StageKit.Finish(s, "Stage_07_Ambush", size);
        }

        // ------------------------------------------------------------------ 8: switchback

        /// <summary>Two tall rooms threaded with zigzag walls, so every fight is around a corner.</summary>
        public static GameObject Stage08(Enemies e)
        {
            var s = StageKit.Begin("Stage_08_Switchback", new Vector2(-14f, -27f));
            var size = new Vector2(40f, 60f);

            StageKit.Floor(s, Vector2.zero, size);
            StageKit.Border(s, Vector2.zero, size);

            // Middle divider with the door on the left.
            StageKit.Wall(s, "Divider_Right", new Vector2(5f, 0f), new Vector2(30f, 1f));
            StageKit.Wall(s, "Divider_Left", new Vector2(-17f, 0f), new Vector2(6f, 1f));

            // Zigzag: walls alternate which side they leave open.
            StageKit.Wall(s, "Zig_A1", new Vector2(6f, -20f), new Vector2(28f, 1f));    // gap on the left
            StageKit.Wall(s, "Zig_A2", new Vector2(-6f, -10f), new Vector2(28f, 1f));   // gap on the right
            StageKit.Wall(s, "Zig_B1", new Vector2(6f, 10f), new Vector2(28f, 1f));     // gap on the left
            StageKit.Wall(s, "Zig_B2", new Vector2(-6f, 20f), new Vector2(28f, 1f));    // gap on the right

            RoomController a = StageKit.Room(s, "Room_A", isStart: true);
            StageKit.Waves(a,
                StageKit.Wave("Wave 1", 0f,
                    StageKit.Spawn(a, "A_Grunt_1", new Vector2(-16f, -24f), e.Grunt),
                    StageKit.Spawn(a, "A_Slinger_1", new Vector2(14f, -15f), e.Slinger),
                    StageKit.Spawn(a, "A_Grunt_2", new Vector2(0f, -15f), e.Grunt, 1),
                    StageKit.Spawn(a, "A_Slinger_2", new Vector2(-14f, -5f), e.Slinger, 1),
                    StageKit.Spawn(a, "A_Brute", new Vector2(10f, -5f), e.Brute)));

            RoomController b = StageKit.Room(s, "Room_B", isFinal: true);
            StageKit.Waves(b,
                StageKit.Wave("Wave 1", 0f,
                    StageKit.Spawn(b, "B_Grunt_1", new Vector2(-12f, 5f), e.Grunt, 1),
                    StageKit.Spawn(b, "B_Grunt_2", new Vector2(12f, 5f), e.Grunt, 1),
                    StageKit.Spawn(b, "B_Slinger_1", new Vector2(-14f, 15f), e.Slinger, 1)),
                StageKit.Wave("Wave 2", 1f,
                    StageKit.Spawn(b, "B_Brute_1", new Vector2(14f, 15f), e.Brute),
                    StageKit.Spawn(b, "B_Slinger_2", new Vector2(14f, 25f), e.Slinger, 1),
                    StageKit.Spawn(b, "B_Brute_2", new Vector2(-14f, 25f), e.Brute, 1)));

            StageKit.Door(a, b, "Door_A_to_B", new Vector2(-12f, 0f), new Vector2(4f, 3f));

            return StageKit.Finish(s, "Stage_08_Switchback", size);
        }

        // ------------------------------------------------------------------ 9: citadel

        /// <summary>Four rooms in a square, cleared clockwise. The longest stage before the boss.</summary>
        public static GameObject Stage09(Enemies e)
        {
            var s = StageKit.Begin("Stage_09_Citadel", new Vector2(-22f, -22f));
            var size = new Vector2(60f, 60f);

            StageKit.Floor(s, Vector2.zero, size);
            StageKit.Border(s, Vector2.zero, size);

            // A cross of dividers, each arm with a doorway in its middle.
            // Each arm is two 13-unit walls leaving a 4-unit doorway - the door's exact height.
            StageKit.Wall(s, "Div_S_Low", new Vector2(0f, -23.5f), new Vector2(1f, 13f));    // A | B
            StageKit.Wall(s, "Div_S_High", new Vector2(0f, -6.5f), new Vector2(1f, 13f));
            StageKit.Wall(s, "Div_E_Low", new Vector2(6.5f, 0f), new Vector2(13f, 1f));      // B / C
            StageKit.Wall(s, "Div_E_High", new Vector2(23.5f, 0f), new Vector2(13f, 1f));
            StageKit.Wall(s, "Div_N_Low", new Vector2(0f, 6.5f), new Vector2(1f, 13f));      // C | D
            StageKit.Wall(s, "Div_N_High", new Vector2(0f, 23.5f), new Vector2(1f, 13f));
            StageKit.Wall(s, "Div_W", new Vector2(-15f, 0f), new Vector2(30f, 1f));          // D / A, sealed

            StageKit.Wall(s, "Cover_A", new Vector2(-15f, -15f), new Vector2(4f, 4f));
            StageKit.Wall(s, "Cover_B1", new Vector2(15f, -22f), new Vector2(8f, 1f));
            StageKit.Wall(s, "Cover_B2", new Vector2(22f, -10f), new Vector2(1f, 8f));
            StageKit.Wall(s, "Cover_C", new Vector2(15f, 15f), new Vector2(4f, 4f));
            StageKit.Wall(s, "Cover_D1", new Vector2(-15f, 22f), new Vector2(8f, 1f));
            StageKit.Wall(s, "Cover_D2", new Vector2(-22f, 10f), new Vector2(1f, 8f));

            RoomController a = StageKit.Room(s, "Room_A", isStart: true);
            StageKit.Waves(a,
                StageKit.Wave("Wave 1", 0f,
                    StageKit.Spawn(a, "A_Grunt_1", new Vector2(-8f, -8f), e.Grunt),
                    StageKit.Spawn(a, "A_Grunt_2", new Vector2(-24f, -8f), e.Grunt),
                    StageKit.Spawn(a, "A_Slinger", new Vector2(-8f, -24f), e.Slinger, 1)));

            RoomController b = StageKit.Room(s, "Room_B");
            StageKit.Waves(b,
                StageKit.Wave("Wave 1", 0f,
                    StageKit.Spawn(b, "B_Slinger_1", new Vector2(15f, -25f), e.Slinger, 1),
                    StageKit.Spawn(b, "B_Slinger_2", new Vector2(25f, -8f), e.Slinger, 1),
                    StageKit.Spawn(b, "B_Grunt_1", new Vector2(8f, -8f), e.Grunt, 1)),
                StageKit.Wave("Wave 2", 1f,
                    StageKit.Spawn(b, "B_Brute", new Vector2(15f, -15f), e.Brute),
                    StageKit.Spawn(b, "B_Grunt_2", new Vector2(24f, -24f), e.Grunt, 2)));

            RoomController c = StageKit.Room(s, "Room_C");
            StageKit.Waves(c,
                StageKit.Wave("Wave 1", 0f,
                    StageKit.Spawn(c, "C_Grunt_1", new Vector2(8f, 8f), e.Grunt, 1),
                    StageKit.Spawn(c, "C_Grunt_2", new Vector2(24f, 8f), e.Grunt, 1),
                    StageKit.Spawn(c, "C_Grunt_3", new Vector2(8f, 24f), e.Grunt, 1),
                    StageKit.Spawn(c, "C_Slinger", new Vector2(24f, 24f), e.Slinger, 1)),
                StageKit.Wave("Wave 2", 1f,
                    StageKit.Spawn(c, "C_Brute_1", new Vector2(15f, 22f), e.Brute),
                    StageKit.Spawn(c, "C_Brute_2", new Vector2(22f, 15f), e.Brute, 1)));

            RoomController d = StageKit.Room(s, "Room_D", isFinal: true);
            StageKit.Waves(d,
                StageKit.Wave("Wave 1", 0f,
                    StageKit.Spawn(d, "D_Slinger_1", new Vector2(-15f, 25f), e.Slinger, 2),
                    StageKit.Spawn(d, "D_Slinger_2", new Vector2(-25f, 8f), e.Slinger, 2),
                    StageKit.Spawn(d, "D_Grunt_1", new Vector2(-8f, 8f), e.Grunt, 2),
                    StageKit.Spawn(d, "D_Grunt_2", new Vector2(-8f, 24f), e.Grunt, 2)),
                StageKit.Wave("Wave 2", 1.5f,
                    StageKit.Spawn(d, "D_Brute_1", new Vector2(-24f, 24f), e.Brute, 1),
                    StageKit.Spawn(d, "D_Brute_2", new Vector2(-15f, 15f), e.Brute, 1),
                    StageKit.Spawn(d, "D_Grunt_3", new Vector2(-24f, 5f), e.Grunt, 2)));

            StageKit.Door(a, b, "Door_A_to_B", new Vector2(0f, -15f), new Vector2(3f, 4f));
            StageKit.Door(b, c, "Door_B_to_C", new Vector2(15f, 0f), new Vector2(4f, 3f));
            StageKit.Door(c, d, "Door_C_to_D", new Vector2(0f, 15f), new Vector2(3f, 4f));

            return StageKit.Finish(s, "Stage_09_Citadel", size);
        }

        // ------------------------------------------------------------------ 10: boss

        /// <summary>The Warlord's arena: an escort wave, then the boss with slingers on the flanks.</summary>
        public static GameObject Stage10(Enemies e)
        {
            var s = StageKit.Begin("Stage_10_Warlord", new Vector2(0f, -22f));
            var size = new Vector2(56f, 56f);

            StageKit.Floor(s, Vector2.zero, size);
            StageKit.Border(s, Vector2.zero, size);

            foreach (Vector2 p in new[] { new Vector2(-12f, -12f), new Vector2(12f, -12f),
                                          new Vector2(-12f, 12f), new Vector2(12f, 12f) })
            {
                StageKit.Wall(s, $"Pillar_{p.x}_{p.y}", p, new Vector2(3f, 3f));
            }

            // A throne dais the boss starts on: two short walls that funnel the approach.
            StageKit.Wall(s, "Dais_Left", new Vector2(-8f, 20f), new Vector2(6f, 1f));
            StageKit.Wall(s, "Dais_Right", new Vector2(8f, 20f), new Vector2(6f, 1f));

            RoomController room = StageKit.Room(s, "Room_Arena", isStart: true, isFinal: true);
            StageKit.Waves(room,
                StageKit.Wave("Escort", 0f,
                    StageKit.Spawn(room, "E_Grunt_1", new Vector2(-16f, 0f), e.Grunt),
                    StageKit.Spawn(room, "E_Grunt_2", new Vector2(16f, 0f), e.Grunt),
                    StageKit.Spawn(room, "E_Grunt_3", new Vector2(-6f, 14f), e.Grunt, 1),
                    StageKit.Spawn(room, "E_Grunt_4", new Vector2(6f, 14f), e.Grunt, 1),
                    StageKit.Spawn(room, "E_Slinger_1", new Vector2(-20f, 20f), e.Slinger),
                    StageKit.Spawn(room, "E_Slinger_2", new Vector2(20f, 20f), e.Slinger)),
                StageKit.Wave("Warlord", 1.5f,
                    StageKit.Spawn(room, "Boss_Warlord", new Vector2(0f, 23f), e.Boss),
                    StageKit.Spawn(room, "B_Slinger_1", new Vector2(-22f, -10f), e.Slinger, 1),
                    StageKit.Spawn(room, "B_Slinger_2", new Vector2(22f, -10f), e.Slinger, 1)));

            return StageKit.Finish(s, "Stage_10_Warlord", size);
        }
    }
}
