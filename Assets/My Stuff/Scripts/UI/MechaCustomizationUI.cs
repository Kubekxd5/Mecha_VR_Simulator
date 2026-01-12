using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class MechaCustomizationUI : MonoBehaviour
{
    private MechaSO mechaToCustomize;
    private GameObject mechaInstanceInLobby;
    private MechaRuntimeData mechaRuntimeData;

    private DropdownField materialDropdown;
    private Slider offsetXSlider;
    private Slider offsetYSlider;
    private VisualElement root;
    private void Awake()
    {
        root = GetComponent<UIDocument>().rootVisualElement;

        if (root == null)
        {
            Debug.LogError("Nie uda³o siê znaleŸæ root VisualElement w UIDocument!", this);
            return;
        }

        materialDropdown = root.Q<DropdownField>("MaterialSelectDropdown");
        offsetXSlider = root.Q<Slider>("OffsetXSlider");
        offsetYSlider = root.Q<Slider>("OffsetYSlider");

        root.style.display = DisplayStyle.None;
    }

    public void SetTargetMecha(GameObject mechaInstance, MechaSO mechaSO)
    {
        if (root == null) return;

        this.mechaInstanceInLobby = mechaInstance;
        this.mechaToCustomize = mechaSO;

        if (mechaInstanceInLobby == null || mechaToCustomize == null)
        {
            Debug.LogError("Something went wrong with assigning instance = null");
            root.style.display = DisplayStyle.None;
            return;
        }

        mechaRuntimeData = mechaInstanceInLobby.GetComponent<MechaRuntimeData>();
        if (mechaRuntimeData == null)
        {
            mechaRuntimeData = mechaInstanceInLobby.AddComponent<MechaRuntimeData>();
        }
        mechaRuntimeData.Initialize(mechaToCustomize);

        InitializeUI();

        root.style.display = DisplayStyle.Flex;
    }

    private void InitializeUI()
    {
        if (materialDropdown == null || offsetXSlider == null || offsetYSlider == null)
        {
            Debug.LogError("Couldn't find all the elements. Check your UXML file");
            return;
        }

        materialDropdown.UnregisterValueChangedCallback(OnMaterialChanged);
        offsetXSlider.UnregisterValueChangedCallback(OnOffsetXChanged);
        offsetYSlider.UnregisterValueChangedCallback(OnOffsetYChanged);

        PopulateMaterialDropdown();

        materialDropdown.RegisterValueChangedCallback(OnMaterialChanged);
        offsetXSlider.RegisterValueChangedCallback(OnOffsetXChanged);
        offsetYSlider.RegisterValueChangedCallback(OnOffsetYChanged);

        materialDropdown.index = 0;
        offsetXSlider.value = 0;
        offsetYSlider.value = 0;

        offsetXSlider.lowValue = -1f;
        offsetXSlider.highValue = 1f;
        offsetYSlider.lowValue = -1f;
        offsetYSlider.highValue = 1f;

        ApplyCustomization(0, Vector2.zero);
    }

    private void PopulateMaterialDropdown()
    {
        if (mechaToCustomize.mechaVisuals.mechaSkins == null || mechaToCustomize.mechaVisuals.mechaSkins.Length == 0)
        {
            materialDropdown.choices = new List<string> { "no materials" };
            materialDropdown.SetEnabled(false);
            return;
        }

        materialDropdown.choices = mechaToCustomize.mechaVisuals.mechaSkins.Select(mat => mat.name).ToList();
    }

    private void OnMaterialChanged(ChangeEvent<string> evt)
    {
        int selectedIndex = materialDropdown.index;
        ApplyCustomization(selectedIndex, mechaRuntimeData.MaterialOffsets);
    }

    private void OnOffsetXChanged(ChangeEvent<float> evt)
    {
        Vector2 currentOffsets = mechaRuntimeData.MaterialOffsets;
        ApplyCustomization(mechaRuntimeData.SelectedMaterialIndex, new Vector2(evt.newValue, currentOffsets.y));
    }

    private void OnOffsetYChanged(ChangeEvent<float> evt)
    {
        Vector2 currentOffsets = mechaRuntimeData.MaterialOffsets;
        ApplyCustomization(mechaRuntimeData.SelectedMaterialIndex, new Vector2(currentOffsets.x, evt.newValue));
    }
    private void ApplyCustomization(int materialIndex, Vector2 offsets)
    {
        if (mechaRuntimeData == null) return;

        mechaRuntimeData.SetCustomization(materialIndex, offsets);
    }
}