using UnityEngine;

public class WeaponSlot : MonoBehaviour
{
    public int slotIndex;
    public bool isLeft;
    public WeaponSO currentWeapon;
    public WeaponClass slotType;
    private MechaRuntimeData mechaRuntimeData;

    public bool IsOccupied => currentWeapon != null;

    private void Start()
    {
        mechaRuntimeData = GetComponentInParent<MechaRuntimeData>();
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

            instance.transform.SetParent(transform.parent, true);

            instance.transform.localPosition = new Vector3(0f, 0f, -0.001f);
            instance.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

            WeaponBehaviour script = instance.GetComponent<WeaponBehaviour>();
            if (script != null)
            {
                script.Initialize(weapon, isLeft);

                if (mechaRuntimeData != null)
                {
                    mechaRuntimeData.AddWeaponInstance(script);
                }
            }
            else
            {
                Debug.LogError($"Spawned weapon '{weapon.name}' is missing WeaponBehaviour script!");
            }

            GetComponent<BoxCollider>().enabled = false;
            GetComponent<MeshRenderer>().enabled = false;
        }

        Debug.Log($"[WeaponSlot #{slotIndex} ({(isLeft ? "Left" : "Right")})] Equipped: {weapon.WeaponName}");
    }
}