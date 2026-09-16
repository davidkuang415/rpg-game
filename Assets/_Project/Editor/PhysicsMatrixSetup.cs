using UnityEditor;
using UnityEngine;
using RPG.Core;

namespace RPG.EditorTools
{
    /// <summary>
    /// Configures the Physics 2D collision matrix for the project's layers.
    ///
    /// Nothing depends on this being right - every combat query passes an explicit LayerMask -
    /// but leaving "everything collides with everything" on makes the physics engine test pairs
    /// that can never matter, which is wasted work on a phone.
    ///
    /// Projectile layers are set to collide with NOTHING: projectiles are raycast sweeps with
    /// no colliders at all, so any physics pairing for them is pure overhead.
    /// </summary>
    public static class PhysicsMatrixSetup
    {
        // Which layers each layer should physically collide with. Anything not listed is disabled.
        private static readonly (string layer, string[] collidesWith)[] Rules =
        {
            (GameLayers.Player, new[] { GameLayers.Enemy, GameLayers.Wall, GameLayers.Hazard, GameLayers.Environment }),
            (GameLayers.Enemy, new[] { GameLayers.Player, GameLayers.Enemy, GameLayers.Wall, GameLayers.Hazard, GameLayers.Environment }),
            (GameLayers.PlayerProjectile, new string[0]),
            (GameLayers.EnemyProjectile, new string[0]),
            (GameLayers.Wall, new[] { GameLayers.Player, GameLayers.Enemy }),
            (GameLayers.Hazard, new[] { GameLayers.Player, GameLayers.Enemy }),
            (GameLayers.PickupVisual, new string[0]),
            (GameLayers.Environment, new[] { GameLayers.Player, GameLayers.Enemy })
        };

        [MenuItem("RPG/Setup/Configure Physics 2D Collision Matrix", priority = 200)]
        public static void Configure()
        {
            int applied = 0;

            for (int i = 0; i < Rules.Length; i++)
            {
                int layerA = LayerMask.NameToLayer(Rules[i].layer);
                if (layerA < 0)
                {
                    Debug.LogError($"[Physics] Layer '{Rules[i].layer}' does not exist. " +
                                   "Run RPG > Phase 1 > Create Project Layers Only first.");
                    return;
                }

                for (int j = i; j < Rules.Length; j++)
                {
                    int layerB = LayerMask.NameToLayer(Rules[j].layer);
                    if (layerB < 0) continue;

                    // A pair collides only if BOTH sides agree it should.
                    bool shouldCollide = Allows(Rules[i].collidesWith, Rules[j].layer) &&
                                         Allows(Rules[j].collidesWith, Rules[i].layer);

                    Physics2D.IgnoreLayerCollision(layerA, layerB, !shouldCollide);
                    applied++;
                }
            }

            // Mark the settings object dirty so the change is written to ProjectSettings rather
            // than lasting only for this Editor session.
            Object[] settings = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/Physics2DSettings.asset");
            if (settings != null && settings.Length > 0)
            {
                EditorUtility.SetDirty(settings[0]);
                AssetDatabase.SaveAssets();
            }

            Debug.Log($"<b>[Physics]</b> Applied {applied} collision-matrix pairs. " +
                      "Verify in Project Settings > Physics 2D; if the checkboxes did not stick, " +
                      "set them by hand once - nothing in gameplay depends on them.");
        }

        private static bool Allows(string[] allowed, string layerName)
        {
            for (int i = 0; i < allowed.Length; i++)
            {
                if (allowed[i] == layerName) return true;
            }
            return false;
        }
    }
}
