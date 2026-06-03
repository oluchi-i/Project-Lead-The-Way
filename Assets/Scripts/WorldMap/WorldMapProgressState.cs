public static class WorldMapProgressState
{
    private static bool hasReturnCheckpoint;
    private static int returnCheckpointIndex;

    public static bool HasReturnCheckpoint => hasReturnCheckpoint;
    public static int ReturnCheckpointIndex => returnCheckpointIndex;

    public static void SetReturnCheckpoint(int checkpointIndex)
    {
        returnCheckpointIndex = checkpointIndex;
        hasReturnCheckpoint = true;
    }

    public static void ClearReturnCheckpoint()
    {
        hasReturnCheckpoint = false;
    }
}
