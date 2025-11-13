using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Player & Mecha Settings")]
    [SerializeField] private MechaSO selectedMecha;
    [SerializeField] private GameObject vrPlayerPrefab_MainMenu;
    [SerializeField] private GameObject vrPlayerPrefab_GameScene;
    [SerializeField] private GameObject spawnShipPrefab;

    [Header("Scene Settings")]
    [SerializeField] private string menuSceneName = "MainMenu";
    [SerializeField] private string gameSceneName = "GameScene";

    private static MechaCustomizationData pendingCustomizationData;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void StartGame(MechaRuntimeData customizationSource)
    {
        pendingCustomizationData = new MechaCustomizationData
        {
            mechaSO = customizationSource.MechaData,
            materialIndex = customizationSource.SelectedMaterialIndex,
            materialOffsets = customizationSource.MaterialOffsets,
            equippedWeapons = new List<WeaponSO>(customizationSource.EquippedWeapons)
        };

        SceneManager.LoadScene(gameSceneName);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == menuSceneName)
        {
            SpawnPlayerInMenu();
        }
        else if (scene.name == gameSceneName)
        {
            StartCoroutine(SpawnSequenceCoroutine());
        }
    }

    private void SpawnPlayerInMenu()
    {
        Transform spawnPoint = GameObject.FindWithTag("PlayerSpawn").transform;
        if (spawnPoint)
        {
            Instantiate(vrPlayerPrefab_MainMenu, spawnPoint.position, spawnPoint.rotation);
        }
    }

    private IEnumerator SpawnSequenceCoroutine()
    {
        if (pendingCustomizationData == null || pendingCustomizationData.mechaSO == null)
        {
            Debug.LogError("No customized data. Returning to menu.");
            SceneManager.LoadScene(menuSceneName);
            yield break;
        }

        Transform spawnPoint = GameObject.FindWithTag("PlayerSpawn").transform;

        if (!spawnPoint || !spawnShipPrefab)
        {
            Debug.LogError("No spawnpoint or spaceship prefab in GameManager", this.gameObject);
            yield break;
        }

        GameObject spawnedMechaInstance = Instantiate(pendingCustomizationData.mechaSO.mechPrefab, spawnPoint.position, spawnPoint.rotation);
        spawnedMechaInstance.SetActive(false);

        MechaRuntimeData runtimeData = spawnedMechaInstance.AddComponent<MechaRuntimeData>();
        runtimeData.Initialize(pendingCustomizationData.mechaSO);
        runtimeData.SetCustomization(pendingCustomizationData.materialIndex, pendingCustomizationData.materialOffsets);
        foreach (var weapon in pendingCustomizationData.equippedWeapons)
        {
            runtimeData.AddWeapon(weapon);
        }

        EquipWeapons(spawnedMechaInstance, runtimeData.EquippedWeapons);

        Transform cockpitSlot = spawnedMechaInstance
            .GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(t => t.CompareTag("CockpitSlot"));

        if (cockpitSlot == null)
        {
            Debug.LogError($"Mecha Prefab '{spawnedMechaInstance.name}' There is no 'CockpitSlot'!", spawnedMechaInstance);
            Destroy(spawnedMechaInstance);
            yield break;
        }

        MechaCockpit cockpitData = pendingCustomizationData.mechaSO.mechaCockpit;
        GameObject cockpitInstance = Instantiate(cockpitData.cockpitPrefab, cockpitSlot);
        cockpitInstance.transform.localPosition = cockpitData.cockpitOffset;
        cockpitInstance.transform.localRotation = Quaternion.identity;

        Camera rendererCamera = cockpitInstance.transform
            .GetComponentsInChildren<Camera>(true)
            .FirstOrDefault(c => c.CompareTag("RendererCamera"));

        Transform screenCameraPoint = spawnedMechaInstance
            .GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(t => t.name == "ScreenCameraPoint");

        if (rendererCamera != null && screenCameraPoint != null)
        {
            rendererCamera.transform.SetParent(screenCameraPoint);
            rendererCamera.transform.localPosition = Vector3.zero;
            rendererCamera.transform.localRotation = Quaternion.identity;
        }
        else
        {
            Debug.LogWarning("RendererCamera or ScreenCameraPoint not found in Mecha hierarchy.");
        }

        if (vrPlayerPrefab_GameScene != null)
        {
            GameObject playerInstance = Instantiate(vrPlayerPrefab_GameScene, cockpitInstance.transform);
            playerInstance.transform.localPosition = new Vector3(0.013f, 0.176f, 0.193f);
            playerInstance.transform.localRotation = Quaternion.identity;
        }

        spawnedMechaInstance.GetComponent<MechaControls>().InitializeControls();

        Vector3 shipStartPosition = spawnPoint.position - (spawnPoint.forward * 1000f) + (Vector3.up * 50f);
        GameObject shipInstance = Instantiate(spawnShipPrefab, shipStartPosition, Quaternion.LookRotation(spawnPoint.forward));

        SpawnShipController shipController = shipInstance.GetComponent<SpawnShipController>();
        if (shipController != null)
        {
            shipController.Initialize(spawnedMechaInstance.transform, spawnPoint);
        }
        else
        {
            Debug.LogError("Spaceship prefab has no SpawnShipController!", shipInstance);
        }

        spawnedMechaInstance.SetActive(true);

        yield return null;
    }

    private void EquipWeapons(GameObject mechaInstance, List<WeaponSO> weapons)
    {
        MechaRuntimeData runtimeData = mechaInstance.GetComponent<MechaRuntimeData>();
        if (runtimeData == null)
        {
            Debug.LogError("EquipWeapons: No MechaRuntimeData found on spawned Mecha!");
            return;
        }

        WeaponSlot[] allSlots = mechaInstance.GetComponentsInChildren<WeaponSlot>(true);
        if (allSlots.Length == 0)
        {
            Debug.LogWarning($"No WeaponSlots found on {mechaInstance.name}!");
            return;
        }

        foreach (var weapon in weapons)
        {
            WeaponSlot targetSlot = null;

            foreach (var slot in allSlots)
            {
                if (!slot.IsOccupied && slot.slotType == weapon.weaponClass)
                {
                    targetSlot = slot;
                    break;
                }
            }
            if (targetSlot != null)
            {
                targetSlot.MountWeapon(weapon);
                runtimeData.AddWeapon(weapon);
                Debug.Log($"Equipped {weapon.WeaponName} in slot #{targetSlot.slotIndex} ({targetSlot.slotType})");
            }
            else
            {
                Debug.LogWarning($"No free matching slot found for {weapon.WeaponName} ({weapon.weaponClass}) on {mechaInstance.name}");
            }
        }
    }
}

public class MechaCustomizationData
{
    public MechaSO mechaSO;
    public int materialIndex;
    public Vector2 materialOffsets;
    public List<WeaponSO> equippedWeapons;
}