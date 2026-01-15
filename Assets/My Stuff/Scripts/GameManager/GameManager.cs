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
    private static MapGenerator mapGen;

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

    public void ReturnToLobby()
    {
        Debug.Log("GameManager: Mission Sequence finished. Returning to Lobby.");
        SceneManager.LoadScene(menuSceneName);
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
        // 1. GENERATE MAP
        mapGen = FindAnyObjectByType<MapGenerator>();
        if (mapGen != null)
        {
            int newSeed = Random.Range(0, 100000);
            Debug.Log($"GameManager: Generating Map with Seed: {newSeed}");

            mapGen.seed = newSeed;
            mapGen.GenerateMap();

            yield return new WaitForFixedUpdate();
        }
        else
        {
            Debug.LogError("CRITICAL: No MapGenerator found in the scene! Map will not update.");
        }

        // 2. CHECK DATA
        if (pendingCustomizationData == null || pendingCustomizationData.mechaSO == null)
        {
            Debug.LogError("CRITICAL: No customized data. Returning to menu.");
            SceneManager.LoadScene(menuSceneName);
            yield break;
        }

        Transform spawnPoint = GameObject.FindWithTag("PlayerSpawn").transform;

        if (!spawnPoint || !spawnShipPrefab)
        {
            Debug.LogError("CRITICAL: No spawnpoint or spaceship prefab in GameManager", this.gameObject);
            yield break;
        }

        // 3. INITIALIZE MISSIONS
        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.InitializeMissions();
        }
        else
        {
            Debug.LogWarning("WARNING: No MissionManager found in GameScene.");
        }

        // 4. SPAWN MECHA
        GameObject spawnedMechaInstance = Instantiate(pendingCustomizationData.mechaSO.mechPrefab, spawnPoint.position, spawnPoint.rotation);
        spawnedMechaInstance.SetActive(false);

        // 5. SETUP MECHA DATA & WEAPONS
        MechaRuntimeData runtimeData = spawnedMechaInstance.GetComponent<MechaRuntimeData>();
        if (runtimeData == null)
        {
            runtimeData = spawnedMechaInstance.AddComponent<MechaRuntimeData>();
        }
        runtimeData.Initialize(pendingCustomizationData.mechaSO);
        runtimeData.SetCustomization(pendingCustomizationData.materialIndex, pendingCustomizationData.materialOffsets);
        foreach (var weapon in pendingCustomizationData.equippedWeapons)
        {
            runtimeData.AddWeapon(weapon);
        }

        EquipWeapons(spawnedMechaInstance, runtimeData.EquippedWeapons);
        runtimeData.RefreshActiveWeapons();

        // 6. SETUP COCKPIT
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

        // 7. SETUP CAMERAS
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
            Debug.LogWarning("WARNING: RendererCamera or ScreenCameraPoint not found in Mecha hierarchy.");
        }

       // 9. INIT CONTROLS
        var controls = spawnedMechaInstance.GetComponent<MechaControls>();
        if (controls != null)
            controls.InitializeControls();
        else
            Debug.LogError("MechaControls missing on mecha prefab!");

        controls.enabled = false;

        // 10. SPAWN SHIP
        Vector3 shipStartPosition = spawnPoint.position - (spawnPoint.forward * 800f) + (Vector3.up * 50f);
        GameObject shipInstance = Instantiate(spawnShipPrefab, shipStartPosition, Quaternion.LookRotation(spawnPoint.forward));

        SpawnShipController shipController = shipInstance.GetComponent<SpawnShipController>();
        if (shipController != null)
        {
            shipController.Initialize(spawnedMechaInstance.transform, spawnPoint);
        }

        // 11. ACTIVATE MECHA
        spawnedMechaInstance.SetActive(true);

        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.RegisterPlayer(spawnedMechaInstance.transform);
        }
        else
        {
            Debug.LogWarning("WARNING: No MissionManager found to register player transform.");
        }

        GameObject playerInstance = null;
        GameObject cockpitInstanceRef = cockpitInstance;

        // 8. SPAWN PLAYER (PILOT)
        if (vrPlayerPrefab_GameScene != null)
        {
            // 8.1. Instantiate as a child of the cockpit immediately
            playerInstance = Instantiate(vrPlayerPrefab_GameScene, cockpitInstance.transform);
            playerInstance.SetActive(false); // Disable while we snap him to the seat

            // 8.2. FIND THE SPECIFIC SEAT
            Transform seatSpawn = cockpitInstance.GetComponentsInChildren<Transform>(true)
                                                 .FirstOrDefault(t => t.CompareTag("PlayerSpawn"));

            if (seatSpawn != null)
            {
                Debug.Log($"GameManager: Snapping VR Pilot to seat: {seatSpawn.name}");

                // 8.3. PARENTING & SNAPPING
                playerInstance.transform.SetParent(seatSpawn, false);
                playerInstance.transform.localPosition = Vector3.zero;
                playerInstance.transform.localRotation = Quaternion.identity;

                // 8.4. CHARACTER CONTROLLER FIX
                var characterController = playerInstance.GetComponent<CharacterController>();
                if (characterController != null)
                {
                    characterController.enabled = false;
                }
            }
            else
            {
                Debug.LogError("CRITICAL: No 'PlayerSpawn' tag found INSIDE the cockpit prefab!");
                playerInstance.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            }

            // 8.5. Wake up the player
            playerInstance.SetActive(true);
        }

        // 12. REGISTER UI
        UnityEngine.UIElements.UIDocument hudDoc = spawnedMechaInstance.GetComponentInChildren<UnityEngine.UIElements.UIDocument>();

        if (hudDoc != null && MissionManager.Instance != null)
        {
            MissionManager.Instance.RegisterGameUI(hudDoc);
        }
        else
        {
            Debug.LogWarning("GameManager: Could not find UIDocument in spawned Mecha hierarchy (or MissionManager is missing).");
        }

        // 13. FINALIZE MAP
        GameObject map = mapGen.gameObject;
        //map.transform.localScale = new Vector3(map.transform.localScale.x * 2, 1f, map.transform.localScale.z * 2);
        yield return null;

        // 14. FINAL VR PLAYER REPOSITION (absolute world snap)
        yield return null;

        if (playerInstance != null && cockpitInstanceRef != null)
        {
            Transform finalSpawn = cockpitInstanceRef
                .GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(t => t.name == "playerSpawnPoint");

            if (finalSpawn != null)
            {
                Debug.Log("GameManager: Final player recenter to playerSpawnPoint");

                Transform playerRoot = playerInstance.transform;
                Transform oldParent = playerRoot.parent;
                playerRoot.SetParent(null, true);

                playerRoot.position = finalSpawn.position;
                playerRoot.rotation = finalSpawn.rotation;

                playerRoot.SetParent(finalSpawn, true);

                var cc = playerInstance.GetComponent<CharacterController>();
                if (cc != null)
                {
                    cc.enabled = false;
                    cc.enabled = true;
                }
            }
            else
            {
                Debug.LogError("CRITICAL: playerSpawnPoint transform not found in cockpit!");
            }
        }
    }

    private void EquipWeapons(GameObject mechaInstance, List<WeaponSO> weapons)
    {
        MechaRuntimeData runtimeData = mechaInstance.GetComponent<MechaRuntimeData>();
        if (runtimeData == null)
        {
            Debug.LogError("CRITICAL: EquipWeapons: No MechaRuntimeData found on spawned Mecha!");
            return;
        }

        WeaponSlot[] allSlots = mechaInstance.GetComponentsInChildren<WeaponSlot>(true);
        if (allSlots.Length == 0)
        {
            Debug.LogWarning($"WARNING: No WeaponSlots found on {mechaInstance.name}!");
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
                Debug.Log($"Equipped {weapon.WeaponName}...");
            }
            else
            {
                Debug.LogWarning($"WARNING: No free matching slot found for {weapon.WeaponName} ({weapon.weaponClass}) on {mechaInstance.name}");
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