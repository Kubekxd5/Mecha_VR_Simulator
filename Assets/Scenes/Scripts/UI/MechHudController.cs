using UnityEngine;
using UnityEngine.UIElements;

public class MechHudController : MonoBehaviour
{
    [Header("Hud Referenes")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform rayPoint;
    [SerializeField] private UIDocument uiDocument;
    private MechaRuntimeData mechaRuntimeData;

    [Header("Raycast Settings")]
    public LayerMask targetLayers;
    public float maxDistance = 2000f;

    [Header("Compass Settings")]
    public Transform playerTransform;

    [Header("Speedometer Settings")]
    [SerializeField] private float smoothSpeed = 2f;
    [SerializeField] private float filterStrength = 0.5f;

    private Label distanceCounterLabel;
    private Label compassLabel;
    private Label speedLabel;

    private float displayedSpeed = 0f;
    private float smoothVelocity;
    private Vector3 lastPosition;

    [SerializeField] private bool isInitialized = false;

    private void Awake()
    {
        if (uiDocument == null)
        {
            uiDocument = GetComponent<UIDocument>();
        }
    }

    private void Start()
    {
        StartLogic();
    }

    private void StartLogic()
    {
        if (mainCamera == null)
        {
            Debug.LogError("MechHudController: Brak przypisanej kamery (mainCamera)!", this.gameObject);
            return;
        }

        if (uiDocument == null)
        {
            Debug.LogError("MechHudController: Nie znaleziono komponentu UIDocument!", this.gameObject);
            return;
        }

        var root = uiDocument.rootVisualElement;
        if (root == null)
        {
            Debug.LogError("MechHudController: rootVisualElement jest null. Upewnij siê, ¿e UIDocument ma przypisany poprawny plik UXML.", this.gameObject);
            return;
        }

        distanceCounterLabel = root.Q<Label>("CrosshairDistanceCounter");
        compassLabel = root.Q<Label>("CompassText");
        speedLabel = root.Q<Label>("CurrentSpeedLabel");

        if (distanceCounterLabel == null) Debug.LogError("MechHudController: Nie znaleziono etykiety 'CrosshairDistanceCounter' w UI!");
        if (compassLabel == null) Debug.LogError("MechHudController: Nie znaleziono etykiety 'CompassText' w UI!");
        if (speedLabel == null) Debug.LogError("MechHudController: Nie znaleziono etykiety 'CurrentSpeedLabel' w UI!");
    }


    public void Initialize(Transform mechaTransform)
    {
        if (mechaTransform == null)
        {
            Debug.LogError("Mecha Transform passed to HUD was null!", this.gameObject);
            this.enabled = false;
            return;
        }

        Debug.Log("MechHudController Initialized.");
        this.playerTransform = mechaTransform;
        this.mechaRuntimeData = mechaTransform.GetComponent<MechaRuntimeData>();

        isInitialized = true;
    }

    void Update()
    {
        if (!isInitialized || playerTransform == null) return;

        if (lastPosition == Vector3.zero)
        {
            lastPosition = playerTransform.position;
        }

        UpdateDistanceCounter();
        UpdateCompass();
        UpdateSpeedometer();
    }

    private void UpdateDistanceCounter()
    {
        if (mainCamera == null || distanceCounterLabel == null)
        {
            return;
        }

        Ray ray = new Ray(rayPoint.position, rayPoint.forward);

        RaycastHit hitInfo;

        if (Physics.Raycast(ray, out hitInfo, maxDistance, targetLayers))
        {
            int distance = Mathf.RoundToInt(hitInfo.distance);

            distanceCounterLabel.text = $"{distance}m";
        }
        else
        {
            distanceCounterLabel.text = "---m";
        }
    }

    private void UpdateCompass()
    {
        if (compassLabel == null || playerTransform == null) return;

        float heading = playerTransform.eulerAngles.y;

        compassLabel.text = FormatCompassHeading(heading);
    }

    private void UpdateSpeedometer()
    {
        if (speedLabel == null || playerTransform == null) return;

        float speed = Vector3.Distance(playerTransform.position, lastPosition) / Time.deltaTime;
        float speedKmh = speed * 3.6f * 10f;

        if (Mathf.Abs(speedKmh - displayedSpeed) > filterStrength)
            displayedSpeed = Mathf.SmoothDamp(displayedSpeed, speedKmh, ref smoothVelocity, Time.deltaTime/smoothSpeed);

        speedLabel.text = $"{speedKmh.ToString("F1")} km/s";

        lastPosition = playerTransform.position;
    }

    private string FormatCompassHeading(float heading)
    {
        // North Sector
        if (heading > 337.5f || heading <= 22.5f)
        {
            if (Mathf.Approximately(heading, 0)) return "N";
            if (heading > 337.5f) return $"N {Mathf.RoundToInt(360 - heading)}° W";
            return $"N {Mathf.RoundToInt(heading)}° E";
        }
        // North-East Sector
        else if (heading > 22.5f && heading <= 67.5f)
        {
            if (Mathf.Approximately(heading, 45)) return "NE";
            return $"N {Mathf.RoundToInt(heading)}° E";
        }
        // East Sector
        else if (heading > 67.5f && heading <= 112.5f)
        {
            if (Mathf.Approximately(heading, 90)) return "E";
            return $"E {Mathf.RoundToInt(heading - 90)}° S";
        }
        // South-East Sector
        else if (heading > 112.5f && heading <= 157.5f)
        {
            if (Mathf.Approximately(heading, 135)) return "SE";
            return $"S {Mathf.RoundToInt(180 - heading)}° E";
        }
        // South Sector
        else if (heading > 157.5f && heading <= 202.5f)
        {
            if (Mathf.Approximately(heading, 180)) return "S";
            return $"S {Mathf.RoundToInt(heading - 180)}° W";
        }
        // South-West Sector
        else if (heading > 202.5f && heading <= 247.5f)
        {
            if (Mathf.Approximately(heading, 225)) return "SW";
            return $"S {Mathf.RoundToInt(heading - 180)}° W";
        }
        // West Sector
        else if (heading > 247.5f && heading <= 292.5f)
        {
            if (Mathf.Approximately(heading, 270)) return "W";
            return $"W {Mathf.RoundToInt(heading - 270)}° N";
        }
        // North-West Sector
        else
        {
            if (Mathf.Approximately(heading, 315)) return "NW";
            return $"N {Mathf.RoundToInt(360 - heading)}° W";
        }
    }

    private void OnDrawGizmos()
    {
        if (mainCamera == null) return;

        Gizmos.color = Color.red;
        Ray ray = new Ray(rayPoint.position, rayPoint.forward);
        Gizmos.DrawRay(ray.origin, ray.direction * maxDistance);
    }
}