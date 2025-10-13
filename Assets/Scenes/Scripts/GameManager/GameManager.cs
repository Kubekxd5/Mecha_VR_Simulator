using System.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Player & Mecha Settings")]
    [SerializeField] private MechaSO selectedMecha;
    [SerializeField] private Transform playerSpawnPoint;
    [SerializeField] private GameObject spawnShipPrefab;

    [Header("Runtime References")]
    [SerializeField] private GameObject spawnedMechaInstance;

    private void Awake()
    {
        // Implementacja Singletona
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void Start()
    {
        StartPlayerSpawnSequence();
    }

    public void StartPlayerSpawnSequence()
    {
        if (selectedMecha == null || playerSpawnPoint == null || spawnShipPrefab == null)
        {
            Debug.LogError("Nie mo¿na rozpocz¹æ sekwencji spawnowania! Brakuje referencji w GameManager.");
            return;
        }

        StartCoroutine(SpawnSequenceCoroutine());
    }

    private IEnumerator SpawnSequenceCoroutine()
    {
        spawnedMechaInstance = Instantiate(selectedMecha.mechPrefab, playerSpawnPoint.position, playerSpawnPoint.rotation);
        spawnedMechaInstance.SetActive(false);

        MechaRuntimeData runtimeData = spawnedMechaInstance.AddComponent<MechaRuntimeData>();
        runtimeData.Initialize(selectedMecha);

        MechHudController hudController = spawnedMechaInstance.GetComponentInChildren<MechHudController>();
        if (hudController != null)
        {
            hudController.Initialize(spawnedMechaInstance.transform);
        }

        EquipDefaultWeapons(spawnedMechaInstance);

        Vector3 shipStartPosition = playerSpawnPoint.position - (playerSpawnPoint.forward * 1000f) + (Vector3.up * 20f);
        Quaternion shipStartRotation = Quaternion.LookRotation(playerSpawnPoint.forward);

        GameObject shipInstance = Instantiate(spawnShipPrefab, shipStartPosition, shipStartRotation);
        SpawnShipController shipController = shipInstance.GetComponent<SpawnShipController>();

        if (shipController != null && shipController.playerHolder != null)
        {
            spawnedMechaInstance.SetActive(true);
            shipController.Initialize(spawnedMechaInstance.transform, playerSpawnPoint);
        }
        else
        {
            Debug.LogError("Nie znaleziono obiektu 'playerHolder' w prefabie statku! Mech zostanie aktywowany w punkcie zrzutu.");
            spawnedMechaInstance.SetActive(true);
        }

        if (selectedMecha.mechaAudio.spawnSound != null)
        {
            AudioSource.PlayClipAtPoint(selectedMecha.mechaAudio.spawnSound, spawnedMechaInstance.transform.position);
        }

        yield return null;
    }

    private void EquipDefaultWeapons(GameObject mechaInstance)
    {
        MechaWeaponHardpoint[] hardpoints = mechaInstance.GetComponentsInChildren<MechaWeaponHardpoint>();
        GameObject[] defaultWeapons = selectedMecha.mechaLoadout.defaultWeapon;

        if (defaultWeapons.Length == 0 || hardpoints.Length == 0)
        {
            Debug.LogWarning($"Mech {selectedMecha.mechName} nie ma zdefiniowanego domyœlnego uzbrojenia lub punktów monta¿owych.");
            return;
        }

        Debug.Log($"Wyposa¿anie mecha {selectedMecha.mechName} w {defaultWeapons.Length} broni.");

        for (int i = 0; i < hardpoints.Length; i++)
        {
            if (i < defaultWeapons.Length && defaultWeapons[i] != null)
            {
                GameObject weaponPrefab = defaultWeapons[i];
                Instantiate(weaponPrefab, hardpoints[i].MountPoint.position, hardpoints[i].MountPoint.rotation, hardpoints[i].MountPoint);
            }
        }
    }
}