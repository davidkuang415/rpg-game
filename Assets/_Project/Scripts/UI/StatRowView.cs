using UnityEngine;
using UnityEngine.UI;

namespace RPG.UI
{
    /// <summary>
    /// One "Name .... Value" line on the character sheet.
    ///
    /// A two-Text component rather than one Text with padded spaces: the UI font is
    /// proportional, so space-aligned columns drift apart as soon as a value gets wider.
    /// </summary>
    public class StatRowView : MonoBehaviour
    {
        [SerializeField] private Text nameLabel;
        [SerializeField] private Text valueLabel;

        public void Set(string statName, string value)
        {
            if (nameLabel != null) nameLabel.text = statName;
            if (valueLabel != null) valueLabel.text = value;
        }

        /// <summary>Tints the value, used to mark stats raised above their base by gear.</summary>
        public void SetValueColor(Color color)
        {
            if (valueLabel != null) valueLabel.color = color;
        }
    }
}
