using UnityEngine;

public sealed class MapCheckpoint : MonoBehaviour
{
    [SerializeField] private string checkpointId = "checkpoint";
    [SerializeField] private string displayName = "";

    public string CheckpointId => checkpointId;
    public string DisplayName => displayName;

    public void Configure(string id)
    {
        checkpointId = id;
    }

    public void Configure(string id, string label)
    {
        checkpointId = id;
        displayName = label;
    }
}
