using UnityEngine;

namespace RPG.Core
{
    /// <summary>
    /// Single source of truth for the project's physics layer names and masks.
    /// Nothing else in the codebase should type a layer name as a raw string,
    /// so renaming a layer later is a one-file change.
    ///
    /// Masks are cached because LayerMask.GetMask does a string lookup and will
    /// be called from combat code (line-of-sight raycasts) every attack.
    /// </summary>
    public static class GameLayers
    {
        public const string Player = "Player";
        public const string Enemy = "Enemy";
        public const string PlayerProjectile = "PlayerProjectile";
        public const string EnemyProjectile = "EnemyProjectile";
        public const string Wall = "Wall";
        public const string Hazard = "Hazard";
        public const string PickupVisual = "PickupVisual";
        public const string Environment = "Environment";

        private static int _player = -1;
        private static int _enemy = -1;
        private static int _wall = -1;
        private static int _hazard = -1;

        /// <summary>Layers that block movement, line of sight, arrows and melee swings.</summary>
        public static int WallMask => _wall >= 0 ? _wall : (_wall = LayerMask.GetMask(Wall));

        public static int PlayerMask => _player >= 0 ? _player : (_player = LayerMask.GetMask(Player));
        public static int EnemyMask => _enemy >= 0 ? _enemy : (_enemy = LayerMask.GetMask(Enemy));
        public static int HazardMask => _hazard >= 0 ? _hazard : (_hazard = LayerMask.GetMask(Hazard));

        /// <summary>
        /// Call once at startup (see BootstrapValidator) to catch a mistyped or
        /// missing layer in the Editor instead of silently getting mask 0.
        /// </summary>
        public static bool ValidateLayers(out string missing)
        {
            missing = string.Empty;
            string[] required =
            {
                Player, Enemy, PlayerProjectile, EnemyProjectile,
                Wall, Hazard, PickupVisual, Environment
            };

            foreach (string layerName in required)
            {
                if (LayerMask.NameToLayer(layerName) < 0)
                {
                    missing += (missing.Length > 0 ? ", " : "") + layerName;
                }
            }

            return missing.Length == 0;
        }
    }
}
