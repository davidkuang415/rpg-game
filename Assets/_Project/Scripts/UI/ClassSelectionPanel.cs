using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using RPG.Classes;
using RPG.Player;

namespace RPG.UI
{
    /// <summary>
    /// Class selection screen. Builds one button per class in the ClassRegistry at runtime,
    /// so adding a class later needs no UI work - only a new ClassData asset in the registry.
    /// </summary>
    public class ClassSelectionPanel : ModalPanel
    {
        [Header("Data")]
        [SerializeField] private ClassRegistry registry;
        [SerializeField] private PlayerStats playerStats;

        [Header("Content")]
        [Tooltip("Parent the generated class buttons are added to.")]
        [SerializeField] private RectTransform buttonContainer;

        [Tooltip("Disabled button used as the template for each generated class button.")]
        [SerializeField] private Button buttonTemplate;

        [Header("Behaviour")]
        [SerializeField] private bool showOnStart = true;

        private readonly List<Button> _spawnedButtons = new List<Button>();

        /// <summary>Raised after a class is chosen. The game flow and save system listen here.</summary>
        public event Action<ClassData> ClassSelected;

        protected override void Awake()
        {
            base.Awake();
            if (buttonTemplate != null) buttonTemplate.gameObject.SetActive(false);
        }

        private void Start()
        {
            if (showOnStart) Show();
            else Hide();
        }

        public override void Show()
        {
            if (registry == null || playerStats == null)
            {
                Debug.LogError($"{nameof(ClassSelectionPanel)} on '{name}' is missing its Registry " +
                               "or Player Stats reference.", this);
                return;
            }

            base.Show();
        }

        protected override void BuildContent()
        {
            for (int i = 0; i < _spawnedButtons.Count; i++)
            {
                if (_spawnedButtons[i] != null) Destroy(_spawnedButtons[i].gameObject);
            }
            _spawnedButtons.Clear();

            if (buttonTemplate == null || buttonContainer == null) return;

            IReadOnlyList<ClassData> classes = registry.Classes;
            for (int i = 0; i < classes.Count; i++)
            {
                ClassData classData = classes[i];
                if (classData == null) continue;

                Button button = Instantiate(buttonTemplate, buttonContainer);
                button.gameObject.name = $"Button_{classData.ClassId}";
                button.gameObject.SetActive(true);

                var label = button.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.text = $"{classData.DisplayName}\n{classData.Description}\n" +
                                 $"Weapon: {classData.AllowedWeaponType}";
                }

                // Local copy captured deliberately: the loop variable would otherwise be shared.
                ClassData captured = classData;
                button.onClick.AddListener(() => Select(captured));

                _spawnedButtons.Add(button);
            }
        }

        private void Select(ClassData classData)
        {
            playerStats.SetClass(classData);
            Hide();
            ClassSelected?.Invoke(classData);
        }
    }
}
