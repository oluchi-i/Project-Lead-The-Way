using UnityEngine;

public sealed class MapCheckpoint : MonoBehaviour
{
    [SerializeField] private string checkpointId = "checkpoint";
    [SerializeField] private string displayName = "";
    [SerializeField] private string levelSceneName = "";

    public string CheckpointId => checkpointId;
    public string DisplayName => displayName;
    public string LevelSceneName => levelSceneName;
    public string ResolvedLevelSceneName => !string.IsNullOrWhiteSpace(levelSceneName)
        ? levelSceneName
        : GetDefaultLevelSceneName(checkpointId);

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

    private static string GetDefaultLevelSceneName(string id)
    {
        if (!TryGetCheckpointNumber(id, out var number) || number <= 1)
            return string.Empty;

        return "Level" + (number - 1).ToString("00");
    }

    private static bool TryGetCheckpointNumber(string id, out int number)
    {
        number = 0;
        if (string.IsNullOrWhiteSpace(id))
            return false;

        var dashIndex = id.LastIndexOf('-');
        if (dashIndex < 0 || dashIndex >= id.Length - 1)
            return false;

        return int.TryParse(id.Substring(dashIndex + 1), out number);
    }
}
