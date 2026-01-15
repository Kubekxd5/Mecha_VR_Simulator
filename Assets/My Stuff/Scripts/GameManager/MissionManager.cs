using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
using UnityEngine.UIElements;
public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance { get; private set; }

    [Header("Configuration")]
    [SerializeField] private List<MissionSO> availableMissions;
    [SerializeField] private int missionsToSelectCount = 3;

    [Header("UI Settings")]
    [Tooltip("The name of the VisualElement in UXML (e.g., 'RightBox')")]
    [SerializeField] private string targetContainerName = "RightBox";
    [SerializeField] private string textStyleClass = "MyLabelStyle";

    // Internal State
    private UIDocument runtimeHUD;
    private VisualElement listContainer;
    private List<ActiveMission> activeMissions = new List<ActiveMission>();
    private Dictionary<ActiveMission, Label> missionLabelMap = new Dictionary<ActiveMission, Label>();
    private bool isSequenceFinished = false;
    private bool isGameOver = false;
    private Transform playerTransform;
    private Vector3 lastPlayerPosition;
    private float movementSpeedThreshold = 0.5f;
    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }
    public void RegisterPlayer(Transform player)
    {
        playerTransform = player;
        lastPlayerPosition = player.position;
    }

    public void RegisterGameUI(UIDocument doc)
    {
        if (doc == null) return;

        runtimeHUD = doc;

        if (runtimeHUD.rootVisualElement == null)
        {
            Debug.LogWarning("WARNING MissionManager: UIDocument root is null.");
            return;
        }

        VisualElement root = runtimeHUD.rootVisualElement;
        listContainer = root.Q<VisualElement>(targetContainerName);

        if (listContainer != null)
        {
            listContainer.Clear();
            listContainer.style.flexDirection = FlexDirection.Column;
            listContainer.style.justifyContent = Justify.Center;

            listContainer.style.paddingTop = 30;
            listContainer.style.paddingBottom = 30;
            listContainer.style.paddingLeft = 25;
            listContainer.style.paddingRight = 25;

            listContainer.style.whiteSpace = WhiteSpace.Normal;
            listContainer.style.overflow = Overflow.Hidden;

            foreach (var mission in activeMissions)
            {
                CreateMissionLabel(mission);
            }
        }
        else
        {
            Debug.LogError($"CRITICAL MissionManager: Could not find '{targetContainerName}'");
        }
    }

    public void InitializeMissions()
    {
        isSequenceFinished = false;
        isGameOver = false;

        activeMissions.Clear();
        missionLabelMap.Clear();

        var shuffled = availableMissions.OrderBy(x => Random.value).ToList();
        int count = Mathf.Min(missionsToSelectCount, shuffled.Count);

        for (int i = 0; i < count; i++)
        {
            ActiveMission newMission = new ActiveMission(shuffled[i]);
            activeMissions.Add(newMission);
        }

        Debug.Log($"MissionManager: Generated {activeMissions.Count} missions.");
    }

    private void Update()
    {
        if (isSequenceFinished || isGameOver || activeMissions.Count == 0) return;

        bool uiNeedsUpdate = false;
        float dt = Time.deltaTime;

        // Calculate if player is moving
        bool isMoving = false;
        if (playerTransform != null)
        {
            float dist = Vector3.Distance(playerTransform.position, lastPlayerPosition);
            float speed = dist / dt;
            if (speed > movementSpeedThreshold) isMoving = true;

            lastPlayerPosition = playerTransform.position;
        }

        foreach (var mission in activeMissions)
        {
            if (mission.isCompleted) continue;

            if (mission.sourceData.missionType == MissionType.Survival)
            {
                mission.progressAccumulator += dt;
                uiNeedsUpdate = CheckTimeProgress(mission) || uiNeedsUpdate;
            }
            else if (mission.sourceData.missionType == MissionType.Travel && isMoving)
            {
                mission.progressAccumulator += dt;
                uiNeedsUpdate = CheckTimeProgress(mission) || uiNeedsUpdate;
            }
        }

        if (uiNeedsUpdate)
        {
            UpdateUI();
            CheckAllMissionsComplete();
        }
    }

    private bool CheckTimeProgress(ActiveMission mission)
    {
        int newIntValues = Mathf.FloorToInt(mission.progressAccumulator);

        if (newIntValues > mission.currentValue)
        {
            mission.currentValue = newIntValues;

            if (mission.currentValue >= mission.targetValue)
            {
                mission.currentValue = mission.targetValue;
                mission.isCompleted = true;
            }
            return true;
        }
        return false;
    }

    private void CreateMissionLabel(ActiveMission mission)
    {
        if (listContainer == null) return;

        Label label = new Label();
        label.text = GetMissionText(mission);
        label.AddToClassList(textStyleClass);

        label.style.fontSize = 16;
        label.style.whiteSpace = WhiteSpace.Normal;
        label.style.marginBottom = 5;
        label.style.unityTextAlign = TextAnchor.MiddleCenter;
        label.style.flexShrink = 1;

        listContainer.Add(label);
        missionLabelMap.Add(mission, label);
    }

    /*public void ReportProgress(MissionType type, int amount = 1)
    {
        if (isSequenceFinished) return;

        bool anyUpdated = false;

        foreach (var mission in activeMissions)
        {
            if (!mission.isCompleted && mission.sourceData.missionType == type)
            {
                mission.currentValue += amount;
                if (mission.currentValue >= mission.targetValue)
                {
                    mission.currentValue = mission.targetValue;
                    mission.isCompleted = true;
                }
                anyUpdated = true;
            }
        }

        if (anyUpdated)
        {
            UpdateUI();
            CheckAllMissionsComplete();
        }
    }*/

    public void ReportProgress(MissionType type, MissionTargetCategory category, int amount = 1)
    {
        if (isGameOver || isSequenceFinished) return;

        bool anyUpdated = false;

        foreach (var mission in activeMissions)
        {
            if (mission.isCompleted) continue;

            if (mission.sourceData.missionType == type &&
                mission.sourceData.targetCategory == category)
            {
                mission.currentValue += amount;
                if (mission.currentValue >= mission.targetValue)
                {
                    mission.currentValue = mission.targetValue;
                    mission.isCompleted = true;
                }
                anyUpdated = true;
            }
        }

        if (anyUpdated)
        {
            UpdateUI();
            CheckAllMissionsComplete();
        }
    }

    private string GetMissionText(ActiveMission mission)
    {
        string status = mission.isCompleted ? "[COMPLETE]" : $"[{mission.currentValue}/{mission.targetValue}]";
        return $"{status}\n{mission.GetDescription()}";
    }
    private void CheckAllMissionsComplete()
    {
        if (activeMissions.All(m => m.isCompleted))
        {
            StartCoroutine(FinishSequence());
        }
    }
    private void UpdateUI()
    {
        foreach (var kvp in missionLabelMap)
        {
            ActiveMission mission = kvp.Key;
            Label label = kvp.Value;

            label.text = GetMissionText(mission);

            if (mission.isCompleted)
            {
                label.style.color = new StyleColor(Color.green);
            }
        }
    }

    private IEnumerator FinishSequence()
    {
        isSequenceFinished = true;

        // Victory Popup
        if (runtimeHUD != null)
        {
            VisualElement overlay = new VisualElement();
            overlay.style.position = Position.Absolute;
            overlay.style.width = Length.Percent(100);
            overlay.style.height = Length.Percent(100);
            overlay.style.backgroundColor = new StyleColor(new Color(0, 0, 0, 0.8f));
            overlay.style.justifyContent = Justify.Center;
            overlay.style.alignItems = Align.Center;

            Label msg = new Label("MISSION ACCOMPLISHED");
            msg.AddToClassList(textStyleClass);
            msg.style.fontSize = 50;
            msg.style.color = Color.yellow;

            overlay.Add(msg);
            runtimeHUD.rootVisualElement.Add(overlay);
        }

        yield return new WaitForSeconds(5.0f);
        GameManager.Instance.ReturnToLobby();
    }

    public void ReportPlayerDeath()
    {
        if (isSequenceFinished || isGameOver) return;

        StartCoroutine(GameOverSequence());
    }

    private IEnumerator GameOverSequence()
    {
        isGameOver = true;
        Debug.Log("Mission Failed: Player Died.");

        if (runtimeHUD != null)
        {
            VisualElement overlay = new VisualElement();
            overlay.style.position = Position.Absolute;
            overlay.style.width = Length.Percent(100);
            overlay.style.height = Length.Percent(100);
            overlay.style.backgroundColor = new StyleColor(new Color(0.4f, 0, 0, 0.7f));
            overlay.style.justifyContent = Justify.Center;
            overlay.style.alignItems = Align.Center;

            Label msg = new Label("MISSION FAILED");
            msg.AddToClassList(textStyleClass);
            msg.style.fontSize = 60;
            msg.style.color = Color.red;
            msg.style.unityFontStyleAndWeight = FontStyle.Bold;

            Label subMsg = new Label("MECH CRITICAL FAILURE");
            subMsg.style.fontSize = 20;
            subMsg.style.color = Color.white;
            subMsg.style.marginTop = 10;

            overlay.Add(msg);
            overlay.Add(subMsg);
            runtimeHUD.rootVisualElement.Add(overlay);
        }

        yield return new WaitForSeconds(5.0f);

        GameManager.Instance.ReturnToLobby();
    }
}