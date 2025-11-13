using UnityEngine;

public class WeaponSlot : MonoBehaviour
{
    public int slotIndex;
    public WeaponSO currentWeapon;
    public WeaponClass slotType;
    private MechaRuntimeData mechaRuntimeData;

    public bool IsOccupied => currentWeapon != null;

    private void Start()
    {
        mechaRuntimeData = GetComponentInParent<MechaRuntimeData>();
        if (mechaRuntimeData == null)
        {
            Debug.LogError("WeaponSlot nie mo¿e znaleŸæ MechaRuntimeData w rodzicu!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        WeaponPickup weaponPickup = other.GetComponent<WeaponPickup>();
        if (weaponPickup != null && mechaRuntimeData != null)
        {
            if (weaponPickup.weaponData.weaponClass == slotType)
            {
                MountWeapon(weaponPickup.weaponData);
                mechaRuntimeData.AddWeapon(weaponPickup.weaponData);
            }
            else
            {
                Debug.Log($"Weapon class mismatch: {weaponPickup.weaponData.weaponClass} cannot mount to {slotType} slot!");
            }
        }
    }

    public void MountWeapon(WeaponSO weapon)
    {
        currentWeapon = weapon;

        if (weapon.weaponPrefab != null)
        {
            GameObject instance = Instantiate(weapon.weaponPrefab, transform);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.parent = transform.parent;
            instance.transform.localPosition = new Vector3(0f, 0f, -0.001f);
            instance.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

            GetComponent<BoxCollider>().enabled = false;
            GetComponent<MeshRenderer>().enabled = false;
        }

        Debug.Log($"[WeaponSlot #{slotIndex}] Equipped weapon: {weapon.WeaponName}");
    }
}