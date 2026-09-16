using System.Collections.Generic;
using UnityEngine;

namespace RPG.Classes
{
    /// <summary>
    /// The list of classes the game knows about, and the lookup from a saved class ID back to
    /// its ClassData asset.
    ///
    /// This indirection matters for saving: the save file stores "knight", not a reference to
    /// a ScriptableObject. That is what lets assets be renamed, moved or rebuilt without
    /// corrupting existing saves, and it is the same pattern items will use later.
    /// </summary>
    [CreateAssetMenu(fileName = "ClassRegistry", menuName = "RPG/Classes/Class Registry")]
    public class ClassRegistry : ScriptableObject
    {
        [SerializeField] private List<ClassData> classes = new List<ClassData>();

        public IReadOnlyList<ClassData> Classes => classes;

        public ClassData GetById(string classId)
        {
            if (string.IsNullOrEmpty(classId)) return null;

            for (int i = 0; i < classes.Count; i++)
            {
                if (classes[i] != null && classes[i].ClassId == classId) return classes[i];
            }
            return null;
        }

        public ClassData GetDefault() => classes.Count > 0 ? classes[0] : null;

        private void OnValidate()
        {
            // Duplicate IDs would make save loading ambiguous, so catch them at authoring time.
            var seen = new HashSet<string>();
            foreach (ClassData data in classes)
            {
                if (data == null) continue;
                if (!seen.Add(data.ClassId))
                {
                    Debug.LogError($"ClassRegistry '{name}': duplicate class id '{data.ClassId}'.", this);
                }
            }
        }
    }
}
