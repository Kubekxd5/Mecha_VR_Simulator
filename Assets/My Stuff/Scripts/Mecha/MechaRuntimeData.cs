using System;
using System.Collections.Generic;
using UnityEngine;

public class MechaRuntimeData : MonoBehaviour, IDamageableEntity
{
    [Header("Mecha Data")]
    [SerializeField] private MechaSO mechaData;

    [Header("Customization Data")]
    [SerializeField] private int selectedMaterialIndex;
    [SerializeField] private Vector2 materialOffsets;
    [SerializeField] private List<WeaponSO> equippedWeapons = new();
    private Dictionary<int, WeaponSlot> weaponSlots = new();

    [Header("Runtime Stats")]
    [SerializeField] private float currentHealth;

    public MechaSO MechaData => mechaData;
    public float CurrentHealth => currentHealth;
    public int SelectedMaterialIndex => selectedMaterialIndex;
    public Vector2 MaterialOffsets => materialOffsets;
    public List<WeaponSO> EquippedWeapons => equippedWeapons;
    public List<WeaponBehaviour> ActiveWeaponScripts = new List<WeaponBehaviour>();

    public event Action<float, float> OnHealthChanged;

    private void Awake()
    {
        if (mechaData == null)
        {
            Debug.LogError("Critical error: MechaSO is unavailable " + gameObject.name);
            return;
        }
        currentHealth = mechaData.mechaStats.health;
    }

    public void Initialize(MechaSO data)
    {
        mechaData = data;
        currentHealth = data.mechaStats.health;
        gameObject.name = $"Mecha_{data.mechName}";

        WeaponSlot[] slots = GetComponentsInChildren<WeaponSlot>();
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].slotIndex = i;
            RegisterWeaponSlot(slots[i]);
        }
    }

    public void SetCustomization(int matIndex, Vector2 offsets)
    {
        selectedMaterialIndex = matIndex;
        materialOffsets = offsets;
        ApplyMaterial();
    }

    public void ApplyMaterial()
    {
        if (mechaData == null || mechaData.mechaVisuals.mechaSkins.Length <= selectedMaterialIndex)
        {
            Debug.LogWarning("Nie mo¿na zaaplikowaæ materia³u - brak danych lub nieprawid³owy indeks.");
            return;
        }

        Material materialToApply = mechaData.mechaVisuals.mechaSkins[selectedMaterialIndex];
        var renderers = GetComponentsInChildren<Renderer>();

        foreach (var renderer in renderers)
        {
            if (renderer.tag == "WeaponSlot") continue;
            Material instancedMaterial = new Material(materialToApply);
            instancedMaterial.mainTextureOffset = materialOffsets;
            renderer.material = instancedMaterial;
        }
    }
    public void RegisterWeaponSlot(WeaponSlot slot)
    {
        if (!weaponSlots.ContainsKey(slot.slotIndex))
        {
            weaponSlots.Add(slot.slotIndex, slot);
            Debug.Log($"Registered WeaponSlot #{slot.slotIndex} on {name}");
        }
        else
        {
            Debug.LogWarning($"Duplicate WeaponSlot index {slot.slotIndex} detected on {name}");
        }
    }
    public void RefreshActiveWeapons()
    {
        ActiveWeaponScripts.Clear();

        WeaponBehaviour[] foundScripts = GetComponentsInChildren<WeaponBehaviour>(true);

        foreach (var script in foundScripts)
        {
            ActiveWeaponScripts.Add(script);
        }

        Debug.Log($"Mecha initialized with {ActiveWeaponScripts.Count} active weapons.");
    }

    public WeaponSlot GetSlotByIndex(int index)
    {
        return weaponSlots.TryGetValue(index, out var slot) ? slot : null;
    }

    public void AddWeapon(WeaponSO weapon)
    {
        equippedWeapons.Add(weapon);
    }

    public void AddWeaponInstance(WeaponBehaviour weaponScript)
    {
        if (weaponScript != null)
        {
            ActiveWeaponScripts.Add(weaponScript);
        }
    }

    public void TakeDamage(float damageAmount)
    {
        if (currentHealth <= 0) return;

        currentHealth -= damageAmount;

        Debug.Log($"{gameObject.name} Took {damageAmount} damage. Remaining: {currentHealth}");

        if (mechaData != null)
        {
            OnHealthChanged?.Invoke(currentHealth, mechaData.mechaStats.health);
        }

        Debug.Log($"{gameObject.name} Took {damageAmount} damage. Remaining: {currentHealth}");
        
        if (mechaData.mechaAudio.damageSound != null)
        {
            AudioSource.PlayClipAtPoint(mechaData.mechaAudio.damageSound, transform.position);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        currentHealth = 0;
        Debug.Log($"{mechaData.mechName} zosta³ zniszczony!");

        if (mechaData.mechaAudio.deathSound != null)
        {
            AudioSource.PlayClipAtPoint(mechaData.mechaAudio.deathSound, transform.position);
        }

        Destroy(gameObject, 2f);
    }

    public MissionTargetCategory GetCategory()
    {
        return MissionTargetCategory.Player;
    }
}