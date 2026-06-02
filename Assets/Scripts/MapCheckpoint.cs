using UnityEngine;

public sealed class MapCheckpoint : MonoBehaviour
{
    [SerializeField] private string checkpointId = "checkpoint";

    public string CheckpointId => checkpointId;

    public void Configure(string id)
    {
        checkpointId = id;
    }
}
