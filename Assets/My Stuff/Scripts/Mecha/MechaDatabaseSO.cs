using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MechaDatabase", menuName = "Game/Mecha Database", order = 99)]
public class MechaDatabaseSO : ScriptableObject
{
    [Tooltip("Lista wszystkich grywalnych mechów dostêpnych w menu wyboru.")]
    public List<MechaSO> availableMechs;
}