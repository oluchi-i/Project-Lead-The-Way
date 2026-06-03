using UnityEngine;

public sealed class MapCheckpoint : MonoBehaviour
{
    [SerializeField] private string checkpointId = "checkpoint";
    [SerializeField] private string displayName = "";
    [SerializeField] private string levelSceneName = "";

    public string CheckpointId => checkpointId;
    public string DisplayName => displayName;
    public string LevelSceneName => levelSceneName;

    public void Configure(string id)
    {
        checkpointId = id;
    }

    public void Configure(string id, string label)
    {
        checkpointId = id;
        displayName = label;
    }

    public void Configure(string id, string label, string sceneName)
    {
        checkpointId = id;
        displayName = label;
        levelSceneName = sceneName;
    }
}
