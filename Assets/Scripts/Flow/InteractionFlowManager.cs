using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

#pragma warning disable 0649 // Unity assigns serialized fields from scenes.

public class InteractionFlowManager : MonoBehaviour
{
    private enum LevelFlowState
    {
        Intro,
        Playing,
        DeathCinematic,
        Success,
        Failure
    }

    [SerializeField] private BoardManager boardManager;
    [SerializeField] private BoardPlayerMover playerMover;
    [SerializeField] private BoardObject playerObject;
    [SerializeField] private BoardObject startTile;
    [SerializeField] private BoardObject startDoor;
    [FormerlySerializedAs("targetDoor")]
    [SerializeField] private BoardObject destinationDoor;
    [SerializeField] private Vector2Int gameplayStartTile;
    [SerializeField] private LevelResultFlashUI resultFlashUI;
    [SerializeField] private PlayerMovement playerDeathAnimation;
    [SerializeField] private IntroCameraTransition introCameraTransition;
    [SerializeField] private DeathCinematicCamera deathCinematicCamera;
    [SerializeField] private GameObject teleportEffectPrefab;
    [SerializeField] private AudioSource teleportAudioSource;
    [SerializeField] private AudioClip teleportSound;
    [SerializeField] private float introTeleportDelayAfterDoorOpen = 0.5f;
    [SerializeField] private float teleportEffectDuration = 1f;
    [SerializeField] private float teleportAppearanceDelay = 0.22f;
    [SerializeField] private float teleportAppearanceDuration = 0.78f;
    [SerializeField] private float teleportRiseOffset = 0.22f;
    [SerializeField] private float teleportYawDegrees = 38f;
    [SerializeField] private float teleportScaleOvershoot = 1.08f;
    [SerializeField] private float teleportEffectHeightOffset = 0.05f;
    [SerializeField] private float teleportEffectTileCoverage = 0.1f;
    [SerializeField] private float introSpawnYawOffsetDegrees = 180f;
    [SerializeField] private int maxInteractionCount = 6;
    [SerializeField] private int interactionCount;

    private int lastInteractionFrame = -1;
    private int lastHandledActionFrame = -1;
    private LevelFlowState flowState = LevelFlowState.Intro;
    private DoorScript.Door startDoorScript;
    private DoorScript.Door destinationDoorScript;
    private Coroutine introRoutine;
    private Coroutine interactionResolutionRoutine;
    private Coroutine deathCinematicRoutine;
    private Coroutine successRoutine;
    private int activeObjectActionCount;
    private bool loggedMissingReferences;
    private Vector3 originalPlayerScale = Vector3.one;
    private Vector3 originalPlayerLocalPosition;
    private Quaternion originalPlayerLocalRotation = Quaternion.identity;
    private bool cachedOriginalPlayerScale;
    private Transform cachedPlayerVisualRoot;

    private static readonly Vector2Int[] Directions =
    {
        Vector2Int.right,
        Vector2Int.left,
        Vector2Int.up,
        Vector2Int.down
    };

    public BoardObject StartTile => startTile;
    public BoardObject StartDoor => startDoor;
    public BoardObject DestinationDoor => destinationDoor;
    public Vector2Int GameplayStartTile => gameplayStartTile;
    public int InteractionCount => interactionCount;
    public int MaxInteractionCount => Mathf.Max(1, maxInteractionCount);
    public event Action<int> InteractionCountChanged;
    public event Action<bool> DeathCinematicVisibilityChanged;
    public bool HasRegisteredInteractionThisFrame => lastInteractionFrame == Time.frameCount;
    public bool HasHandledActionThisFrame => lastHandledActionFrame == Time.frameCount;
    public bool IsDeathCinematicActive => flowState == LevelFlowState.DeathCinematic;
    public bool CanAcceptAction
    {
        get
        {
            EnsureReferences();
            return flowState == LevelFlowState.Playing
                && HasRequiredGameplayReferences()
                && lastHandledActionFrame != Time.frameCount
                && activeObjectActionCount == 0
                && (playerMover == null || !playerMover.IsMoving);
        }
    }

    public void Configure(BoardManager newBoardManager, BoardPlayerMover newPlayerMover, BoardObject newPlayerObject, BoardObject newDestinationDoor)
    {
        boardManager = newBoardManager;
        playerMover = newPlayerMover;
        playerObject = newPlayerObject;
        destinationDoor = newDestinationDoor;
        destinationDoorScript = null;
        cachedPlayerVisualRoot = null;
        cachedOriginalPlayerScale = false;
    }

    public void ConfigureLevelFlow(BoardObject newStartTile, BoardObject newStartDoor, BoardObject newDestinationDoor)
    {
        startTile = newStartTile;
        startDoor = newStartDoor;
        destinationDoor = newDestinationDoor;
        startDoorScript = null;
        destinationDoorScript = null;
    }

    public void ConfigureResultFlash(LevelResultFlashUI newResultFlashUI)
    {
        resultFlashUI = newResultFlashUI;
    }

    public void ConfigurePlayerDeathAnimation(PlayerMovement newPlayerDeathAnimation)
    {
        playerDeathAnimation = newPlayerDeathAnimation;
        cachedPlayerVisualRoot = null;
        cachedOriginalPlayerScale = false;
    }

    public void ConfigureIntroCameraTransition(IntroCameraTransition newIntroCameraTransition)
    {
        introCameraTransition = newIntroCameraTransition;
    }

    public void ConfigureDeathCinematicCamera(DeathCinematicCamera newDeathCinematicCamera)
    {
        deathCinematicCamera = newDeathCinematicCamera;
    }

    public void ConfigureTeleportEffect(GameObject newTeleportEffectPrefab)
    {
        teleportEffectPrefab = newTeleportEffectPrefab;
    }

    public void ConfigureTeleportAudio(AudioSource newTeleportAudioSource, AudioClip newTeleportSound)
    {
        teleportAudioSource = newTeleportAudioSource;
        teleportSound = newTeleportSound;
    }

    private void Awake()
    {
        EnsureReferences();
    }

    private void Start()
    {
        EnsureReferences();
        InteractionCountChanged?.Invoke(interactionCount);

        if (CanRunIntro())
        {
            introRoutine = StartCoroutine(RunIntro());
        }
        else
        {
            WarnMissingIntroSetup();
            flowState = LevelFlowState.Playing;
        }
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            ReloadCurrentLevel();
    }

    public bool RegisterInteraction(SelectableControlObject selectedObject, ControlAction action)
    {
        return RegisterInteraction();
    }

    public void FailLevel()
    {
        if (flowState == LevelFlowState.Playing)
            BeginDeathCinematic(Vector2Int.zero);
        else
            CompleteLevel(false);
    }

    public bool RegisterInteraction()
    {
        if (!CanAcceptAction)
            return false;

        RegisterInteractionCore();
        return true;
    }

    public bool TryBeginObjectAction()
    {
        if (!CanAcceptAction)
            return false;

        activeObjectActionCount++;
        lastHandledActionFrame = Time.frameCount;
        return true;
    }

    public void CompleteObjectAction(bool countsAsInteraction)
    {
        if (activeObjectActionCount > 0)
            activeObjectActionCount--;

        if (!countsAsInteraction || flowState != LevelFlowState.Playing)
            return;

        RegisterInteractionCore();
    }

    private void RegisterInteractionCore()
    {
        interactionCount++;
        lastInteractionFrame = Time.frameCount;
        lastHandledActionFrame = Time.frameCount;
        InteractionCountChanged?.Invoke(interactionCount);

        if (TryFindNextStep(out var direction))
        {
            var nextTile = playerObject.TilePosition + direction;
            if (IsFatalHazardTileForPlayer(nextTile))
            {
                BeginDeathCinematic(direction);
                return;
            }

            var reachesGoal = IsGoalTileForPlayer(nextTile);
            if (playerMover.TryStep(direction) && reachesGoal)
            {
                CompleteLevel(true);
                return;
            }
        }

        BeginInteractionResolution();
    }

    public void MarkActionHandledWithoutInteraction()
    {
        lastHandledActionFrame = Time.frameCount;
    }

    public void StepPlayerTowardDoor()
    {
        EnsureReferences();

        if (boardManager == null || playerMover == null || playerObject == null || destinationDoor == null)
            return;

        if (playerMover.IsMoving)
            return;

        boardManager.RebuildRegistry();

        if (TryFindNextStep(out var direction))
            playerMover.TryStep(direction);
    }

    private IEnumerator RunIntro()
    {
        flowState = LevelFlowState.Intro;
        boardManager.RebuildRegistry();
        playerMover.PlaceAtTile(startTile.TilePosition);
        ApplyIntroSpawnRotation();
        SetPlayerVisible(false);
        SetPlayerScale(0f);
        introCameraTransition?.PlaceAtStart();
        yield return null;

        var entryDoor = GetDoorScript(startDoor, ref startDoorScript);
        if (entryDoor == null)
        {
            Debug.LogWarning("InteractionFlowManager intro was cancelled because the start door has no Door component.", this);
            flowState = LevelFlowState.Playing;
            introRoutine = null;
            yield break;
        }

        entryDoor.Open();
        yield return WaitForDoor(entryDoor);

        if (introTeleportDelayAfterDoorOpen > 0f)
            yield return new WaitForSeconds(introTeleportDelayAfterDoorOpen);

        yield return PlayPlayerAppearance();

        var path = CreateIntroPath(playerObject.TilePosition, gameplayStartTile);
        var cameraTransitionRoutine = introCameraTransition != null
            ? StartCoroutine(introCameraTransition.TransitionToGameplay())
            : null;

        if (path.Count > 0)
            yield return playerMover.PlayScriptedPath(path);

        if (cameraTransitionRoutine != null)
            yield return cameraTransitionRoutine;

        entryDoor.Close();
        yield return WaitForDoor(entryDoor);

        boardManager.RebuildRegistry();
        SetPlayerScale(1f);
        SetPlayerVisible(true);
        flowState = LevelFlowState.Playing;
        introRoutine = null;
    }

    private bool CanRunIntro()
    {
        return boardManager != null
            && playerMover != null
            && playerObject != null
            && startTile != null
            && startDoor != null
            && GetDoorScript(startDoor, ref startDoorScript) != null;
    }

    private bool HasRequiredGameplayReferences()
    {
        return boardManager != null
            && playerMover != null
            && playerObject != null
            && destinationDoor != null;
    }

    private void WarnMissingIntroSetup()
    {
        if (boardManager == null || playerMover == null || playerObject == null || startTile == null || startDoor == null)
        {
            Debug.LogWarning(
                "InteractionFlowManager skipped the level intro because one or more intro references are missing. Run Tools > Lead The Way > Scene > Wire Current Scene References.",
                this);
            return;
        }

        if (GetDoorScript(startDoor, ref startDoorScript) == null)
            Debug.LogWarning("InteractionFlowManager skipped the level intro because the configured start door has no Door component.", startDoor);
    }

    private List<Vector2Int> CreateIntroPath(Vector2Int fromTile, Vector2Int toTile)
    {
        var path = new List<Vector2Int>();
        var current = fromTile;

        while (current.x != toTile.x)
        {
            current += current.x < toTile.x ? Vector2Int.right : Vector2Int.left;
            path.Add(current);
        }

        while (current.y != toTile.y)
        {
            current += current.y < toTile.y ? Vector2Int.up : Vector2Int.down;
            path.Add(current);
        }

        return path;
    }

    private IEnumerator WaitForDoor(DoorScript.Door door)
    {
        if (door == null)
            yield break;

        var elapsed = 0f;
        const float maxWait = 1.5f;
        while (door.IsAnimating && elapsed < maxWait)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    private bool TryFindNextStep(out Vector2Int nextDirection)
    {
        nextDirection = default;

        var start = playerObject.TilePosition;
        var goalTiles = GetDoorApproachTiles();
        if (goalTiles.Count == 0 || goalTiles.Contains(start))
            return false;

        var queue = new Queue<Vector2Int>();
        var visited = new HashSet<Vector2Int>();
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();

        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (goalTiles.Contains(current))
            {
                nextDirection = ReconstructFirstDirection(start, current, cameFrom);
                return nextDirection != Vector2Int.zero;
            }

            foreach (var direction in Directions)
            {
                var next = current + direction;
                if (visited.Contains(next) || !boardManager.CanEnterTile(playerObject, next))
                    continue;

                visited.Add(next);
                cameFrom[next] = current;
                queue.Enqueue(next);
            }
        }

        return false;
    }

    private HashSet<Vector2Int> GetDoorApproachTiles()
    {
        var goals = new HashSet<Vector2Int>();
        foreach (var goalObject in boardManager.GetGoalObjects())
        {
            if (goalObject == null)
                continue;

            foreach (var goalTile in goalObject.GetOccupiedTiles())
            {
                if (boardManager.CanEnterTile(playerObject, goalTile) && HasEnterableGoalApproach(goalTile))
                    goals.Add(goalTile);
            }
        }

        var doorOpen = IsDestinationDoorOpen();
        if (goals.Count > 0 && doorOpen)
            return goals;

        goals.Clear();
        if (destinationDoor == null)
            return goals;

        var doorTile = destinationDoor.TilePosition;
        foreach (var direction in Directions)
        {
            var candidate = doorTile + direction;
            if (!doorOpen && boardManager.IsGoalTile(candidate))
                continue;

            if (boardManager.CanEnterTile(playerObject, candidate))
                goals.Add(candidate);
        }

        return goals;
    }

    private bool HasEnterableGoalApproach(Vector2Int goalTile)
    {
        if (boardManager.IsInsideBounds(goalTile))
            return true;

        foreach (var direction in Directions)
        {
            var candidate = goalTile + direction;
            if (boardManager.IsInsideBounds(candidate) && boardManager.CanEnterTile(playerObject, candidate))
                return true;
        }

        return false;
    }

    private bool IsDestinationDoorOpen()
    {
        var door = GetDoorScript(destinationDoor, ref destinationDoorScript);
        return door == null || door.open;
    }

    private DoorScript.Door GetDoorScript(BoardObject doorObject, ref DoorScript.Door cachedDoor)
    {
        if (doorObject == null)
            return null;

        if (cachedDoor == null)
            cachedDoor = doorObject.GetComponentInChildren<DoorScript.Door>(true);

        return cachedDoor;
    }

    private void BeginInteractionResolution()
    {
        if (flowState != LevelFlowState.Playing)
            return;

        if (interactionResolutionRoutine != null)
            StopCoroutine(interactionResolutionRoutine);

        interactionResolutionRoutine = StartCoroutine(ResolveInteractionAfterMovement());
    }

    private IEnumerator ResolveInteractionAfterMovement()
    {
        while (playerMover != null && playerMover.IsMoving)
            yield return null;

        if (IsPlayerOnActiveHazard())
        {
            interactionResolutionRoutine = null;
            BeginDeathCinematic(Vector2Int.zero);
            yield break;
        }

        if (IsPlayerOnGoalTile())
        {
            interactionResolutionRoutine = null;
            CompleteLevel(true);
            yield break;
        }

        if (interactionCount >= MaxInteractionCount)
        {
            interactionResolutionRoutine = null;
            BeginDeathCinematic(Vector2Int.zero);
            yield break;
        }

        interactionResolutionRoutine = null;
    }

    private void CompleteLevel(bool succeeded)
    {
        if (flowState == LevelFlowState.Success || flowState == LevelFlowState.Failure)
            return;

        flowState = succeeded ? LevelFlowState.Success : LevelFlowState.Failure;

        if (succeeded)
        {
            if (successRoutine != null)
                StopCoroutine(successRoutine);

            successRoutine = StartCoroutine(RunSuccessSequence());
        }
        else
        {
            PlayPlayerDeathAnimation();

            if (resultFlashUI != null)
                resultFlashUI.Flash(false);
        }
    }

    private IEnumerator RunSuccessSequence()
    {
        var cameraTransitionRoutine = introCameraTransition != null && introCameraTransition.HasEndingPose
            ? StartCoroutine(introCameraTransition.TransitionToEnding())
            : null;

        while (playerMover != null && playerMover.IsMoving)
            yield return null;

        if (cameraTransitionRoutine != null)
            yield return cameraTransitionRoutine;

        yield return PlayPlayerDisappearance();

        var exitDoor = GetDoorScript(destinationDoor, ref destinationDoorScript);
        if (exitDoor != null)
        {
            exitDoor.Close();
            yield return WaitForDoor(exitDoor);
        }

        if (resultFlashUI != null)
            resultFlashUI.Flash(true);

        successRoutine = null;
    }

    private IEnumerator PlayPlayerAppearance()
    {
        yield return PlayTeleportTransition(playerObject != null ? playerObject.TilePosition : startTile.TilePosition, true);
    }

    private IEnumerator PlayPlayerDisappearance()
    {
        yield return PlayTeleportTransition(playerObject != null ? playerObject.TilePosition : Vector2Int.zero, false);
    }

    private IEnumerator PlayTeleportTransition(Vector2Int tile, bool appearing)
    {
        var effect = SpawnTeleportEffect(tile);
        PlayTeleportSound();
        var elapsed = 0f;
        var duration = Mathf.Max(0.01f, teleportEffectDuration);
        var appearanceDelay = Mathf.Clamp(teleportAppearanceDelay, 0f, duration);
        var appearanceDuration = Mathf.Max(0.01f, teleportAppearanceDuration);
        var appearanceCompleteTime = Mathf.Min(duration, appearanceDelay + appearanceDuration);

        if (appearing)
        {
            SetPlayerVisible(false);
            SetPlayerScale(0f);
        }
        else
        {
            SetPlayerVisible(true);
            SetPlayerScale(1f);
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            if (elapsed >= appearanceDelay && elapsed <= appearanceCompleteTime)
            {
                var t = Mathf.Clamp01((elapsed - appearanceDelay) / Mathf.Max(0.01f, appearanceCompleteTime - appearanceDelay));
                t = Mathf.SmoothStep(0f, 1f, t);

                if (appearing)
                {
                    SetPlayerVisible(true);
                    SetPlayerTeleportTransition(t, true);
                }
                else
                {
                    SetPlayerTeleportTransition(t, false);
                }
            }

            yield return null;
        }

        SetPlayerVisible(appearing);
        SetPlayerTeleportTransition(1f, appearing);

        if (effect != null)
            Destroy(effect);
    }

    private void PlayTeleportSound()
    {
        if (teleportSound == null)
            return;

        if (teleportAudioSource == null)
            teleportAudioSource = GetComponent<AudioSource>();

        if (teleportAudioSource == null)
            teleportAudioSource = gameObject.AddComponent<AudioSource>();

        teleportAudioSource.playOnAwake = false;
        teleportAudioSource.loop = false;
        teleportAudioSource.spatialBlend = 0f;
        teleportAudioSource.PlayOneShot(teleportSound);
    }

    private GameObject SpawnTeleportEffect(Vector2Int tile)
    {
        if (teleportEffectPrefab == null || boardManager == null)
            return null;

        var height = playerMover != null ? playerMover.transform.position.y : 0f;
        var position = boardManager.TileToWorld(tile, height + teleportEffectHeightOffset);
        var effect = Instantiate(teleportEffectPrefab, position, Quaternion.identity);
        effect.name = $"{teleportEffectPrefab.name} Runtime";
        effect.transform.localScale = Vector3.one * Mathf.Max(0.01f, boardManager.TileSize * teleportEffectTileCoverage);
        return effect;
    }

    private void ApplyIntroSpawnRotation()
    {
        if (playerMover == null)
            return;

        var euler = playerMover.transform.eulerAngles;
        euler.x = 0f;
        euler.y += introSpawnYawOffsetDegrees;
        euler.z = 0f;
        playerMover.transform.eulerAngles = euler;
    }

    private void CacheOriginalPlayerScale()
    {
        if (cachedOriginalPlayerScale)
            return;

        var visualRoot = GetPlayerVisualRoot();
        if (visualRoot == null)
            return;

        originalPlayerScale = visualRoot.localScale;
        originalPlayerLocalPosition = visualRoot.localPosition;
        originalPlayerLocalRotation = visualRoot.localRotation;
        cachedOriginalPlayerScale = true;
    }

    private void SetPlayerScale(float progress)
    {
        var visualRoot = GetPlayerVisualRoot();
        if (visualRoot == null)
            return;

        CacheOriginalPlayerScale();
        visualRoot.localScale = Vector3.Lerp(Vector3.zero, originalPlayerScale, Mathf.Clamp01(progress));
    }

    private void SetPlayerTeleportTransition(float progress, bool appearing)
    {
        var visualRoot = GetPlayerVisualRoot();
        if (visualRoot == null)
            return;

        CacheOriginalPlayerScale();

        var t = Mathf.Clamp01(progress);
        var visibleProgress = appearing ? t : 1f - t;
        var smoothVisibleProgress = Mathf.SmoothStep(0f, 1f, visibleProgress);
        var scaleMultiplier = appearing ? CalculateTeleportAppearScale(t) : smoothVisibleProgress;
        var offsetDirection = appearing ? 1f - t : t;
        var yawDirection = appearing ? 1f - t : -t;

        visualRoot.localScale = originalPlayerScale * scaleMultiplier;
        visualRoot.localPosition = originalPlayerLocalPosition + Vector3.up * teleportRiseOffset * offsetDirection;
        visualRoot.localRotation = originalPlayerLocalRotation * Quaternion.Euler(0f, teleportYawDegrees * yawDirection, 0f);

        if (t >= 1f)
        {
            visualRoot.localPosition = originalPlayerLocalPosition;
            visualRoot.localRotation = originalPlayerLocalRotation;
            visualRoot.localScale = appearing ? originalPlayerScale : Vector3.zero;
        }
    }

    private float CalculateTeleportAppearScale(float progress)
    {
        var t = Mathf.Clamp01(progress);
        var overshoot = Mathf.Max(1f, teleportScaleOvershoot);
        if (t < 0.82f)
            return Mathf.Lerp(0f, overshoot, Mathf.SmoothStep(0f, 1f, t / 0.82f));

        return Mathf.Lerp(overshoot, 1f, Mathf.SmoothStep(0f, 1f, (t - 0.82f) / 0.18f));
    }

    private void SetPlayerVisible(bool visible)
    {
        var visualRoot = GetPlayerVisualRoot();
        if (visualRoot == null)
            return;

        foreach (var renderer in visualRoot.GetComponentsInChildren<Renderer>(true))
            renderer.enabled = visible;
    }

    private Transform GetPlayerVisualRoot()
    {
        if (cachedPlayerVisualRoot != null)
            return cachedPlayerVisualRoot;

        if (playerDeathAnimation != null)
        {
            cachedPlayerVisualRoot = playerDeathAnimation.transform;
            return cachedPlayerVisualRoot;
        }

        if (playerMover != null)
        {
            cachedPlayerVisualRoot = playerMover.VisualRoot;
            return cachedPlayerVisualRoot;
        }

        return null;
    }

    private void PlayPlayerDeathAnimation()
    {
        EnsureReferences();
        if (playerDeathAnimation == null)
        {
            Debug.LogWarning("InteractionFlowManager could not play the player death animation because PlayerMovement is not wired. Run Tools > Lead The Way > Scene > Wire Current Scene References.", this);
            return;
        }

        playerDeathAnimation.Kill();
    }

    private bool IsPlayerOnGoalTile()
    {
        EnsureReferences();

        if (boardManager == null || playerObject == null)
            return false;

        boardManager.RebuildRegistry();
        return IsGoalTileForPlayer(playerObject.TilePosition);
    }

    private bool IsGoalTileForPlayer(Vector2Int anchorTile)
    {
        if (boardManager == null || playerObject == null)
            return false;

        foreach (var goalObject in boardManager.GetGoalObjects())
        {
            if (goalObject == null)
                continue;

            foreach (var playerTile in playerObject.GetOccupiedTiles(anchorTile))
            {
                if (goalObject.GetOccupiedTiles().Contains(playerTile))
                    return true;
            }
        }

        return false;
    }

    private void BeginDeathCinematic(Vector2Int direction)
    {
        if (flowState != LevelFlowState.Playing)
            return;

        flowState = LevelFlowState.DeathCinematic;
        DeathCinematicVisibilityChanged?.Invoke(true);

        if (interactionResolutionRoutine != null)
        {
            StopCoroutine(interactionResolutionRoutine);
            interactionResolutionRoutine = null;
        }

        if (deathCinematicRoutine != null)
            StopCoroutine(deathCinematicRoutine);

        deathCinematicRoutine = StartCoroutine(RunDeathCinematic(direction));
    }

    private IEnumerator RunDeathCinematic(Vector2Int direction)
    {
        EnsureReferences();

        if (deathCinematicCamera != null && playerMover != null)
        {
            yield return deathCinematicCamera.MoveIntoShot(playerMover.transform);

            if (direction == Vector2Int.zero)
            {
                deathCinematicCamera.ApplyShot(playerMover.transform);
            }
            else
            {
                var movement = playerMover.PlayCinematicStep(
                    direction,
                    deathCinematicCamera.FatalMoveDurationMultiplier,
                    deathCinematicCamera.FatalAnimationSpeedMultiplier);

                while (movement.MoveNext())
                {
                    deathCinematicCamera.ApplyShot(playerMover.transform);
                    yield return movement.Current;
                }

                deathCinematicCamera.ApplyShot(playerMover.transform);
            }
        }
        else if (playerMover != null)
        {
            if (direction != Vector2Int.zero)
            {
                playerMover.TryStep(direction);
                while (playerMover.IsMoving)
                    yield return null;
            }
        }

        CompleteLevel(false);
        if (deathCinematicCamera != null)
            deathCinematicCamera.ClearShotLock();

        deathCinematicRoutine = null;
    }

    private bool IsPlayerOnActiveHazard()
    {
        EnsureReferences();

        if (boardManager == null || playerObject == null)
            return false;

        boardManager.RebuildRegistry();
        foreach (var boardObject in boardManager.GetObjectsAt(playerObject.TilePosition))
        {
            if (boardObject == null || boardObject.ObjectType != BoardObjectType.Hazard)
                continue;

            var spikeToggle = boardObject.GetComponent<SpikeToggle>();
            if (spikeToggle != null && spikeToggle.IsRaised)
                return true;
        }

        return false;
    }

    private bool IsFatalHazardTileForPlayer(Vector2Int anchorTile)
    {
        EnsureReferences();

        if (boardManager == null || playerObject == null)
            return false;

        boardManager.RebuildRegistry();
        foreach (var occupiedTile in playerObject.GetOccupiedTiles(anchorTile))
        {
            if (IsActiveHazardTile(occupiedTile))
                return true;
        }

        return false;
    }

    private bool IsActiveHazardTile(Vector2Int tile)
    {
        foreach (var boardObject in boardManager.GetObjectsAt(tile))
        {
            if (boardObject == null || boardObject.ObjectType != BoardObjectType.Hazard)
                continue;

            var spikeToggle = boardObject.GetComponent<SpikeToggle>();
            if (spikeToggle != null && spikeToggle.IsRaised)
                return true;
        }

        return false;
    }

    private void ReloadCurrentLevel()
    {
        var activeScene = SceneManager.GetActiveScene();
        if (activeScene.buildIndex >= 0)
            SceneManager.LoadScene(activeScene.buildIndex);
        else
            SceneManager.LoadScene(activeScene.name);
    }

    private static Vector2Int ReconstructFirstDirection(Vector2Int start, Vector2Int goal, Dictionary<Vector2Int, Vector2Int> cameFrom)
    {
        var current = goal;
        while (cameFrom.TryGetValue(current, out var previous) && previous != start)
            current = previous;

        return current - start;
    }

    private void EnsureReferences()
    {
        if (playerDeathAnimation == null)
        {
            if (playerMover != null)
                playerDeathAnimation = playerMover.GetComponentInChildren<PlayerMovement>(true);

            if (playerDeathAnimation == null && playerObject != null)
                playerDeathAnimation = playerObject.GetComponentInChildren<PlayerMovement>(true);
        }

        if (deathCinematicCamera == null)
            deathCinematicCamera = FindAnyObjectByType<DeathCinematicCamera>();

        if (introCameraTransition == null)
            introCameraTransition = FindAnyObjectByType<IntroCameraTransition>();

        if (loggedMissingReferences || HasRequiredGameplayReferences())
            return;

        loggedMissingReferences = true;
        Debug.LogWarning("InteractionFlowManager is missing one or more required scene references. Run Tools > Lead The Way > Scene > Wire Current Scene References.", this);
    }
}

#pragma warning restore 0649
