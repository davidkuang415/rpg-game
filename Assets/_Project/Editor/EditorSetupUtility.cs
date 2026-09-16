using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace RPG.EditorTools
{
    /// <summary>
    /// Shared helpers for the project's setup tools: writing private [SerializeField] values,
    /// creating assets idempotently, and building placeholder uGUI objects.
    ///
    /// Editor-only. These exist so the phase setup tools stay short and so the tricky parts
    /// (serialized properties, destroyed-asset detection) are written once.
    /// </summary>
    public static class EditorSetupUtility
    {
        /// <summary>
        /// Assigns a private [SerializeField] field through SerializedObject, which is what
        /// makes the value show up in the Inspector and actually get saved.
        /// </summary>
        public static void SetPrivateField(UnityEngine.Object target, string fieldName, object value)
        {
            if (target == null)
            {
                Debug.LogError($"[Setup] Cannot set '{fieldName}' on a null object.");
                return;
            }

            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(fieldName);
            if (property == null)
            {
                Debug.LogError($"[Setup] '{target.GetType().Name}' has no serialized field '{fieldName}'.");
                return;
            }

            switch (value)
            {
                case UnityEngine.Object unityObject:
                    // Unity objects fake-null when destroyed or unloaded, so an unusable
                    // reference still matches this pattern. Catch it instead of saving null.
                    if (unityObject == null)
                    {
                        Debug.LogError($"[Setup] Tried to assign a missing/unloaded asset to " +
                                       $"'{target.GetType().Name}.{fieldName}'.");
                        return;
                    }
                    property.objectReferenceValue = unityObject;
                    break;

                case null: property.objectReferenceValue = null; break;
                case Enum enumValue: property.intValue = Convert.ToInt32(enumValue); break;
                case string s: property.stringValue = s; break;
                case float f: property.floatValue = f; break;
                case int i: property.intValue = i; break;
                case bool b: property.boolValue = b; break;
                case Color c: property.colorValue = c; break;
                case Vector2 v: property.vector2Value = v; break;

                default:
                    Debug.LogError($"[Setup] Unsupported value type '{value.GetType().Name}' for '{fieldName}'.");
                    return;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        /// <summary>Fills a serialized list/array of object references.</summary>
        public static void SetPrivateObjectList(UnityEngine.Object target, string fieldName,
            IList<UnityEngine.Object> values)
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(fieldName);
            if (property == null || !property.isArray)
            {
                Debug.LogError($"[Setup] '{target.GetType().Name}.{fieldName}' is not a serialized list.");
                return;
            }

            property.ClearArray();
            for (int i = 0; i < values.Count; i++)
            {
                property.InsertArrayElementAtIndex(i);
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        /// <summary>
        /// Loads an asset, or creates it if missing. <paramref name="created"/> reports which
        /// happened, so callers only apply defaults to genuinely new assets and never stomp
        /// on values the designer has since tuned by hand.
        /// </summary>
        public static T CreateOrLoadAsset<T>(string path, out bool created) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
            {
                created = false;
                return existing;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            created = true;

            // Reload so the caller holds the AssetDatabase-owned instance.
            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        /// <summary>Built-in font for placeholder UI. Name differs between Unity versions.</summary>
        public static Font GetDefaultFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font == null) Debug.LogWarning("[Setup] No built-in font found; placeholder labels will be blank.");
            return font;
        }

        // ---------------------------------------------------------------- UI builders

        public static GameObject CreateUiObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        public static Image CreateUiImage(string name, Transform parent, Sprite sprite, Color color)
        {
            GameObject go = CreateUiObject(name, parent);
            go.AddComponent<CanvasRenderer>();

            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = true;
            return image;
        }

        public static Text CreateUiText(string name, Transform parent, string content, int fontSize,
            TextAnchor alignment)
        {
            GameObject go = CreateUiObject(name, parent);
            go.AddComponent<CanvasRenderer>();

            var text = go.AddComponent<Text>();
            text.font = GetDefaultFont();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        public static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>Finds a direct child by name, or null.</summary>
        public static Transform FindChild(Transform parent, string childName)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                if (parent.GetChild(i).name == childName) return parent.GetChild(i);
            }
            return null;
        }

        /// <summary>Adds a component only if the object does not already have one.</summary>
        public static T EnsureComponent<T>(GameObject target) where T : Component
        {
            T existing = target.GetComponent<T>();
            return existing != null ? existing : target.AddComponent<T>();
        }
    }
}
