using UnityEngine;

[CreateAssetMenu(fileName = "NewMission", menuName = "Game/Mission Data")]
public class MissionSO : ScriptableObject
{
    [Header("Mission Info")]
    public string missionTitle;
    [Tooltip("Use {0} for the number. E.g. 'Destroy {0} Trees'")]
    public string descriptionTemplate;

    [Header("Settings")]
    public MissionType missionType; // e.g., Elimination
    public MissionTargetCategory targetCategory; // e.g., Nature

    [Header("Randomization")]
    public int minTargetValue = 5;
    public int maxTargetValue = 15;
}

[System.Serializable]
public class ActiveMission
{
    public MissionSO sourceData;
    public int targetValue;
    public int currentValue;
    public bool isCompleted;
    public float progressAccumulator;

    public ActiveMission(MissionSO data)
    {
        sourceData = data;
        targetValue = Random.Range(data.minTargetValue, data.maxTargetValue + 1);
        currentValue = 0;
        isCompleted = false;
        progressAccumulator = 0f;
    }


    public string GetDescription()
    {
        if (sourceData.missionType == MissionType.Survival || sourceData.missionType == MissionType.Travel)
        {
            int remaining = Mathf.Max(0, targetValue - currentValue);
            string timeString = string.Format("{0:00}:{1:00}", remaining / 60, remaining % 60);
            return string.Format(sourceData.descriptionTemplate, timeString);
        }

        return string.Format(sourceData.descriptionTemplate, targetValue);
    }

    public float GetProgressNormal()
    {
        if (targetValue == 0) return 1f;
        return Mathf.Clamp01((float)currentValue / targetValue);
    }
}

public interface IDamageableEntity
{
    void TakeDamage(float amount);
    MissionTargetCategory GetCategory();
}

#region Enums
public enum MissionType
{
    Elimination,    // Kill Enemies
    Survival,       // Survive Time
    Collection,     // Collect Scrap
    Objective,       // Destroy Buildings
    Travel
}

public enum MissionTargetCategory
{
    EnemyUnit,      // Walking Mechs
    EnemyBuilding,  // Spawners/Bases
    Nature,         // Trees, Rocks
    Prop,           // Barrels, Crates
    Player
}
#endregion