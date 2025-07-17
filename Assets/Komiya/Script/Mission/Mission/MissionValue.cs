using System;
using UnityEngine;

[CreateAssetMenu(fileName = "MissionValue", menuName = "Scriptable Objects/MissionValue")]

[Serializable]
public class MissionValue : ScriptableObject
{
    public int parentPurpose;
    public int childPurpose;

    public string explain;
}
