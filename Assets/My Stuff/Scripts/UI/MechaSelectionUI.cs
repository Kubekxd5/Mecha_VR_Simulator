using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

[RequireComponent(typeof(UIDocument))]
public class MechaSelectionUI : MonoBehaviour
{
    [Header("Database and scene")]
    [SerializeField] private MechaDatabaseSO mechaDatabase;
    [SerializeField] private Transform mechaSpawnPoint;

    [Header("References")]
    [SerializeField] private MechaCustomizationUI customizationUIController;

    private VisualElement selectedMechImage;
    private VisualElement mechaSetContainer;

    private GameObject currentMechaInstance;

    private void Start()
    {
        if (mechaDatabase == null || mechaSpawnPoint == null || customizationUIController == null)
        {
            Debug.LogError("MechaSelectionUI nie jest w pe≥ni skonfigurowany! Sprawdü referencje w Inspektorze.", this);
            return;
        }

        var root = GetComponent<UIDocument>().rootVisualElement;

        selectedMechImage = root.Q<VisualElement>("SelectedMechImage");
        mechaSetContainer = root.Q<VisualElement>("MechaSet");

        selectedMechImage.RegisterCallback<ClickEvent>(evt => StartGame());

        var mechaButtons = mechaSetContainer.Query<VisualElement>(className: "mecha-select-button").ToList();

        if (mechaButtons.Count != mechaDatabase.availableMechs.Count)
        {
            Debug.LogWarning($"Amount of buttons ({mechaButtons.Count}) does not equal amount of mechas in db: ({mechaDatabase.availableMechs.Count}).", this);
        }

        for (int i = 0; i < mechaButtons.Count; i++)
        {
            if (i < mechaDatabase.availableMechs.Count)
            {
                MechaSO mechaData = mechaDatabase.availableMechs[i];
                mechaButtons[i].RegisterCallback<ClickEvent>(evt => SelectMech(mechaData));

                if (mechaData.mechIcon != null)
                {
                    mechaButtons[i].style.backgroundImage = new StyleBackground(mechaData.mechIcon.texture);
                }
            }
        }

        if (mechaDatabase.availableMechs.Count > 0)
        {
            SelectMech(mechaDatabase.availableMechs[0]);
        }
    }

    private void SelectMech(MechaSO selectedMechaSO)
    {
        if (selectedMechaSO == null) return;

        if (selectedMechImage != null && selectedMechaSO.mechIcon != null)
        {
            selectedMechImage.style.backgroundImage = new StyleBackground(selectedMechaSO.mechIcon.texture);
        }

        if (currentMechaInstance != null)
        {
            Destroy(currentMechaInstance);
        }

        if (selectedMechaSO.mechPrefab != null)
        {
            currentMechaInstance = Instantiate(selectedMechaSO.mechPrefab, mechaSpawnPoint.position, mechaSpawnPoint.rotation);
        }

        customizationUIController.SetTargetMecha(currentMechaInstance, selectedMechaSO);
    }

    private void StartGame()
    {
        GameManager.Instance.StartGame(currentMechaInstance.GetComponent<MechaRuntimeData>());
    }
}