using UnityEngine;

namespace RPG.Core
{
    /// <summary>
    /// Where a character's visible sprite lives.
    ///
    /// Up to Phase 11 the SpriteRenderer sat on the character's root, next to its collider,
    /// which meant nothing could animate the sprite (squash, lunge, recoil) without resizing
    /// the hitbox. Phase 12 moves the sprite to a child named "Body" so the visual can move
    /// freely while the root - and the physics on it - stays put.
    ///
    /// Both layouts are supported, so a prefab that has not been restructured still works.
    /// </summary>
    public static class CharacterBody
    {
        public const string BodyName = "Body";

        /// <summary>The body renderer: on the root if it is there, else on the "Body" child.</summary>
        public static SpriteRenderer FindRenderer(GameObject character)
        {
            if (character == null) return null;

            var onRoot = character.GetComponent<SpriteRenderer>();
            if (onRoot != null) return onRoot;

            Transform body = character.transform.Find(BodyName);
            return body != null ? body.GetComponent<SpriteRenderer>() : null;
        }

        /// <summary>The transform to animate. The "Body" child, or null when the sprite is on the root.</summary>
        public static Transform FindAnimatable(GameObject character)
        {
            if (character == null) return null;
            return character.transform.Find(BodyName);
        }
    }
}
