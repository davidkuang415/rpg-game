using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using RPG.CameraSystem;
using RPG.Core;
using RPG.UI.HUD;

namespace RPG.EditorTools
{
    /// <summary>
    /// Phase 15: see-more-of-the-arena pass.
    ///
    ///   - The camera pulls back, so more of a room is visible at once.
    ///   - An arrow at the screen edge points at every living enemy the camera can't see, so a
    ///     straggler behind the camera (or across a big room) is never just missing.
    ///
    /// The circular backdrop every character used to have behind it (Phase 12's stand-in
    /// outline, from when every body was a flat-tinted circle) is removed in Phase 12 itself -
    /// RestructureCharacter now strips it - so re-run Phase 12 to pick that up if it hasn't run
    /// since Phase 14's real art went in.
    ///
    /// Safe to re-run.
    /// </summary>
    public static class Phase15SetupBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/TestArena.unity";
        private const string ArrowSpritePath = "Assets/_Project/Art/Kenney/UI/EnemyArrow.png";

        // Was 12 (Phase 1). 20 shows ~67% more of the arena at once.
        private const float PreviousVisibleWorldHeight = 12f;
        private const float NewVisibleWorldHeight = 20f;

        [MenuItem("RPG/Phase 15/See More Of The Arena", priority = 280)]
        public static void Setup()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Scene scene = EnsureTestSceneOpen();
            if (!scene.IsValid()) return;

            GameObject camera = GameObject.Find("Main Camera");
            GameObject hud = GameObject.Find("HUD");

            if (camera == null || hud == null)
            {
                Debug.LogError("[Phase 15] Missing Main Camera or HUD. Run the earlier tools first.");
                return;
            }

            ZoomOut(camera);
            BuildOffscreenIndicators(hud, camera);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Verify(camera, hud);
        }

        private static Scene EnsureTestSceneOpen()
        {
            Scene active = SceneManager.GetActiveScene();
            if (active.path == ScenePath) return active;

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                Debug.LogError($"[Phase 15] {ScenePath} is missing.");
                return default;
            }

            return EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        // ------------------------------------------------------------------ camera

        private static void ZoomOut(GameObject camera)
        {
            var follow = camera.GetComponent<CameraFollow2D>();
            if (follow == null)
            {
                Debug.LogError("[Phase 15] Main Camera has no CameraFollow2D.");
                return;
            }

            EditorSetupUtility.SetPrivateField(follow, "visibleWorldHeight", NewVisibleWorldHeight);
        }

        // ------------------------------------------------------------------ offscreen indicator

        private static void BuildOffscreenIndicators(GameObject hud, GameObject cameraObject)
        {
            EditorSetupUtility.ConfigureSpriteImport(ArrowSpritePath, pixelsPerUnit: 100,
                wrap: TextureWrapMode.Clamp, meshType: SpriteMeshType.FullRect, border: Vector4.zero);
            Sprite arrowSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ArrowSpritePath);

            Transform existing = EditorSetupUtility.FindChild(hud.transform, "OffscreenIndicators");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            GameObject root = EditorSetupUtility.CreateUiObject("OffscreenIndicators", hud.transform);
            EditorSetupUtility.StretchFull((RectTransform)root.transform);

            Image arrowTemplate = EditorSetupUtility.CreateUiImage("ArrowTemplate", root.transform,
                arrowSprite, new Color(1f, 0.4f, 0.35f));
            arrowTemplate.raycastTarget = false;
            arrowTemplate.preserveAspect = true;

            var arrowRect = (RectTransform)arrowTemplate.transform;
            arrowRect.sizeDelta = new Vector2(64f, 64f);

            var indicator = root.AddComponent<OffscreenEnemyIndicator>();
            EditorSetupUtility.SetPrivateField(indicator, "targetCamera", cameraObject.GetComponent<Camera>());
            EditorSetupUtility.SetPrivateField(indicator, "enemyLayers", LayerMask.GetMask(GameLayers.Enemy));
            EditorSetupUtility.SetPrivateField(indicator, "arrowTemplate", arrowRect);
        }

        // ------------------------------------------------------------------ verification

        private static void Verify(GameObject camera, GameObject hud)
        {
            bool ok = true;

            var follow = camera.GetComponent<CameraFollow2D>();
            if (follow == null)
            {
                Debug.LogError("[Phase 15] VERIFY FAILED: Main Camera has no CameraFollow2D.");
                ok = false;
            }

            if (EditorSetupUtility.FindChild(hud.transform, "OffscreenIndicators") == null)
            {
                Debug.LogError("[Phase 15] VERIFY FAILED: HUD has no OffscreenIndicators.");
                ok = false;
            }

            if (ok)
            {
                Debug.Log($"<b>[Phase 15]</b> Camera pulled back from {PreviousVisibleWorldHeight} to " +
                          $"{NewVisibleWorldHeight} visible world units; off-screen enemies now get an " +
                          "edge arrow.");
            }
        }
    }
}
