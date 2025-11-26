using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Grid;
using Scriptables.Enemies;
using Enemy;
using Player;

/// <summary>
/// Coordinates ordered hordes of waves, spawns enemies from grid-defined spawn nodes, and drives phase transitions.
/// </summary>
public class HordesManager : Singleton<HordesManager>
{
    #region Variables And Properties
    #region Serialized Fields
    [Header("Dependencies")]
    [Tooltip("Grid used to resolve spawn nodes and optional spawn point bindings.")]
    [SerializeField] private Grid3D grid;
    [Tooltip("Game manager controlling phase flow.")]
    [SerializeField] private GameManager gameManager;

    [Header("Hordes")]
    [Tooltip("Ordered list of hordes. Each entry is executed when entering the defence phase.")]
    [SerializeField] private List<HordeDefinition> hordes = new List<HordeDefinition>();

    [Header("Timing")]
    [Tooltip("Seconds waited after entering defence before the first wave of a horde begins.")]
    [SerializeField] private float defenceStartDelay = 0.5f;

    [Header("Player")]
    [Tooltip("Reference to PlayerHealth component")]
    [SerializeField] private PlayerHealth cachedPlayerHealth;
    #endregion

    #region Runtime
    private int currentHordeIndex = -1;
    private int activeEnemies;
    private Coroutine hordeRoutine;
    private bool hordeActive;
    private readonly List<SpawnPointDoor> spawnDoors = new List<SpawnPointDoor>();
    private readonly HashSet<SpawnPointDoor> previewDoorBuffer = new HashSet<SpawnPointDoor>();
    private readonly List<WaveEnemyType> enemyTypesBuffer = new List<WaveEnemyType>();
    private readonly List<WaveSpawnAssignment> spawnAssignmentBuffer = new List<WaveSpawnAssignment>();
    private readonly List<Vector2Int> previewNodesBuffer = new List<Vector2Int>();
    private readonly List<WaveEnemyTypeState> enemyTypeStatesBuffer = new List<WaveEnemyTypeState>();
    #endregion

    #region Properties
    public bool HasPendingHordes
    {
        get { return currentHordeIndex + 1 < hordes.Count; }
    }
    #endregion
    #endregion

    #region Methods
    #region Unity
    /// <summary>
    /// Subscribes to phase changes to automatically start hordes.
    /// </summary>
    protected override void OnEnable()
    {
        base.OnEnable();
        EventsManager.GamePhaseChanged += HandlePhaseChanged;
        CacheSpawnDoors();
    }

    /// <summary>
    /// Removes subscriptions when disabled.
    /// </summary>
    private void OnDisable()
    {
        EventsManager.GamePhaseChanged -= HandlePhaseChanged;
        spawnDoors.Clear();
        previewDoorBuffer.Clear();
    }
    #endregion

    #region Public
    /// <summary>
    /// Signals the manager that an enemy has spawned.
    /// </summary>
    public void NotifyEnemySpawned(PooledEnemy enemy)
    {
        if (!hordeActive)
            return;

        activeEnemies++;
    }

    /// <summary>
    /// Signals the manager that an enemy has despawned.
    /// </summary>
    public void NotifyEnemyDespawned(PooledEnemy enemy)
    {
        if (!hordeActive)
            return;

        if (activeEnemies > 0)
            activeEnemies--;
    }
    #endregion

    #region Internal
    private void HandlePhaseChanged(GamePhase phase)
    {
        if (phase == GamePhase.Defence)
        {
            //CloseAllSpawnDoors();
            BeginNextHorde();
            return;
        }

        if (phase == GamePhase.Building)
            PreviewNextWaveDoors();
    }

    /// <summary>
    /// Starts the next configured horde when defence begins.
    /// </summary>
    private void BeginNextHorde()
    {
        if (hordeActive)
            return;

        if (!HasPendingHordes)
        {
            EventsManager.InvokeGameVictoryAchieved();
            return;
        }

        if (hordeRoutine != null)
            StopCoroutine(hordeRoutine);

        currentHordeIndex++;
        HordeDefinition definition = hordes[currentHordeIndex];
        hordeRoutine = StartCoroutine(RunHorde(definition));
    }

    /// <summary>
    /// Opens doors for spawn points used by the next scheduled wave and closes the rest.
    /// </summary>
    private void PreviewNextWaveDoors()
    {
        if (grid == null)
            return;

        if (spawnDoors.Count == 0)
            CacheSpawnDoors();

        if (spawnDoors.Count == 0)
            return;

        IReadOnlyList<Vector2Int> previewNodes = ResolveNextWaveSpawnNodes();
        if (previewNodes == null || previewNodes.Count == 0)
        {
            //CloseAllSpawnDoors();
            return;
        }

        previewDoorBuffer.Clear();
        int previewCount = previewNodes.Count;
        for (int i = 0; i < previewCount; i++)
        {
            SpawnPointDoor door = grid.GetSpawnDoor(previewNodes[i]);
            if (door != null)
                previewDoorBuffer.Add(door);
        }

        int trackedCount = spawnDoors.Count;
        for (int i = 0; i < trackedCount; i++)
        {
            SpawnPointDoor door = spawnDoors[i];
            if (door == null)
                continue;

            if (previewDoorBuffer.Contains(door))
                door.OpenDoor();
            else
                door.CloseDoor();
        }

        previewDoorBuffer.Clear();
    }

    /// <summary>
    /// Closes all known spawn doors, used before defence begins or when no preview is available.
    /// </summary>
    private void CloseAllSpawnDoors()
    {
        if (spawnDoors.Count == 0)
            CacheSpawnDoors();

        int doorCount = spawnDoors.Count;
        for (int i = 0; i < doorCount; i++)
        {
            SpawnPointDoor door = spawnDoors[i];
            if (door != null)
                door.CloseDoor();
        }
    }

    /// <summary>
    /// Builds the cached list of doors using grid bindings to avoid repeated lookups.
    /// </summary>
    private void CacheSpawnDoors()
    {
        spawnDoors.Clear();

        if (grid == null)
            return;

        Vector2Int[] spawnCoords = grid.GetEnemySpawnCoords();
        if (spawnCoords == null || spawnCoords.Length == 0)
            return;

        int coordCount = spawnCoords.Length;
        for (int i = 0; i < coordCount; i++)
        {
            SpawnPointDoor door = grid.GetSpawnDoor(spawnCoords[i]);
            if (door != null && !spawnDoors.Contains(door))
                spawnDoors.Add(door);
        }
    }

    /// <summary>
    /// Returns the spawn nodes of the next upcoming wave when available.
    /// </summary>
    private IReadOnlyList<Vector2Int> ResolveNextWaveSpawnNodes()
    {
        previewNodesBuffer.Clear();

        if (hordes == null || hordes.Count == 0)
            return null;

        int nextHordeIndex = currentHordeIndex + 1;
        if (nextHordeIndex < 0 || nextHordeIndex >= hordes.Count)
            return null;

        HordeDefinition horde = hordes[nextHordeIndex];
        IReadOnlyList<HordeWave> waves = horde.Waves;
        if (waves == null || waves.Count == 0)
            return null;

        int waveCount = waves.Count;
        for (int i = 0; i < waveCount; i++)
        {
            IReadOnlyList<WaveSpawnAssignment> assignments = waves[i].SpawnAssignments;
            if (assignments != null && assignments.Count > 0)
            {
                previewNodesBuffer.Clear();
                int assignmentCount = assignments.Count;
                for (int a = 0; a < assignmentCount; a++)
                {
                    Vector2Int node = assignments[a].SpawnNode;
                    if (!previewNodesBuffer.Contains(node))
                        previewNodesBuffer.Add(node);
                }

                if (previewNodesBuffer.Count > 0)
                    return previewNodesBuffer;
            }

            IReadOnlyList<Vector2Int> nodes = waves[i].SpawnNodes;
            if (nodes != null && nodes.Count > 0)
                return nodes;
        }

        return null;
    }

    /// <summary>
    /// Sequentially executes all waves in the provided horde.
    /// </summary>
    private IEnumerator RunHorde(HordeDefinition definition)
    {
        hordeActive = true;
        activeEnemies = 0;
        if (defenceStartDelay > 0f)
            yield return new WaitForSeconds(defenceStartDelay);

        IReadOnlyList<HordeWave> waves = definition.Waves;
        for (int i = 0; i < waves.Count; i++)
        {
            HordeWave wave = waves[i];
            yield return StartCoroutine(SpawnWave(wave));

            if (wave.AdvanceMode == WaveAdvanceMode.AfterClear)
            {
                yield return new WaitUntil(() => activeEnemies == 0);
                if (wave.AdvanceDelaySeconds > 0f)
                    yield return new WaitForSeconds(wave.AdvanceDelaySeconds);
            }
            else
            {
                if (wave.AdvanceDelaySeconds > 0f)
                    yield return new WaitForSeconds(wave.AdvanceDelaySeconds);
            }
        }

        yield return new WaitUntil(() => activeEnemies == 0);
        FinalizeHordeCompletion();
        hordeActive = false;
        hordeRoutine = null;
    }

    /// <summary>
    /// Spawns all enemies for a single wave honoring cadence.
    /// </summary>
    private IEnumerator SpawnWave(HordeWave wave)
    {
        List<WaveEnemyType> enemyTypes = BuildEnemyTypesForWave(wave);
        if (enemyTypes.Count == 0)
            yield break;

        List<WaveSpawnAssignment> spawnAssignments = BuildSpawnAssignments(wave, enemyTypes.Count);
        if (spawnAssignments.Count == 0)
            yield break;

        enemyTypeStatesBuffer.Clear();
        int totalRemaining = 0;
        for (int i = 0; i < enemyTypes.Count; i++)
        {
            WaveEnemyType type = enemyTypes[i];
            int count = Mathf.Max(0, type.EnemyCount);
            enemyTypeStatesBuffer.Add(new WaveEnemyTypeState(type.EnemyDefinition, type.RuntimeModifiers, type.SpawnOffset, count));
            totalRemaining += count;
        }

        if (totalRemaining == 0)
            yield break;

        float cadence = Mathf.Max(0.05f, wave.SpawnCadenceSeconds);
        while (totalRemaining > 0)
        {
            bool spawnedThisCycle = false;
            int assignmentCount = spawnAssignments.Count;
            for (int i = 0; i < assignmentCount && totalRemaining > 0; i++)
            {
                WaveSpawnAssignment assignment = spawnAssignments[i];
                int typeIndex = ResolveNextEnemyTypeIndex(assignment, enemyTypeStatesBuffer);
                if (typeIndex < 0)
                    continue;

                WaveEnemyTypeState state = enemyTypeStatesBuffer[typeIndex];
                if (state.Definition == null || state.Definition.EnemyPool == null || state.RemainingCount <= 0)
                    continue;

                SpawnEnemyInstance(state.Definition, assignment.SpawnNode, state.Modifiers, state.SpawnOffset);
                state.RemainingCount--;
                enemyTypeStatesBuffer[typeIndex] = state;
                totalRemaining--;
                spawnedThisCycle = true;
            }

            if (totalRemaining > 0)
            {
                if (!spawnedThisCycle)
                {
                    Debug.LogWarning("Wave spawn aborted: remaining enemies could not be matched to any spawn assignments. Check per-spawner enemy type lists.");
                    yield break;
                }

                yield return new WaitForSeconds(cadence);
            }
        }
    }

    private List<WaveEnemyType> BuildEnemyTypesForWave(HordeWave wave)
    {
        enemyTypesBuffer.Clear();

        IReadOnlyList<WaveEnemyType> configured = wave.EnemyTypes;
        if (configured != null && configured.Count > 0)
        {
            int configuredCount = configured.Count;
            for (int i = 0; i < configuredCount; i++)
            {
                WaveEnemyType type = configured[i];
                if (type.EnemyDefinition != null && type.EnemyDefinition.EnemyPool != null && type.EnemyCount > 0)
                    enemyTypesBuffer.Add(type);
            }
        }

        if (enemyTypesBuffer.Count == 0 && wave.HasLegacyEnemy && wave.LegacyEnemyDefinition != null && wave.LegacyEnemyDefinition.EnemyPool != null && wave.LegacyEnemyCount > 0)
            enemyTypesBuffer.Add(new WaveEnemyType(wave.LegacyEnemyDefinition, wave.LegacyRuntimeModifiers, wave.LegacyEnemyCount, wave.LegacySpawnOffset));

        return enemyTypesBuffer;
    }

    private List<WaveSpawnAssignment> BuildSpawnAssignments(HordeWave wave, int enemyTypeCount)
    {
        spawnAssignmentBuffer.Clear();
        if (enemyTypeCount <= 0)
            return spawnAssignmentBuffer;

        IReadOnlyList<WaveSpawnAssignment> configured = wave.SpawnAssignments;
        if (configured != null && configured.Count > 0)
        {
            int configuredCount = configured.Count;
            for (int i = 0; i < configuredCount; i++)
            {
                WaveSpawnAssignment assignment = configured[i];
                List<int> allowedTypes = BuildValidatedAllowedTypes(assignment.AllowedEnemyTypeIndices, enemyTypeCount);
                spawnAssignmentBuffer.Add(new WaveSpawnAssignment(assignment.SpawnNode, allowedTypes));
            }
        }

        if (spawnAssignmentBuffer.Count == 0)
        {
            IReadOnlyList<Vector2Int> nodes = wave.SpawnNodes;
            if (nodes != null && nodes.Count > 0)
            {
                List<int> defaultAllowedTypes = BuildDefaultAllowedTypes(enemyTypeCount);
                int nodeCount = nodes.Count;
                for (int i = 0; i < nodeCount; i++)
                    spawnAssignmentBuffer.Add(new WaveSpawnAssignment(nodes[i], new List<int>(defaultAllowedTypes)));
            }
        }

        return spawnAssignmentBuffer;
    }

    private List<int> BuildValidatedAllowedTypes(IReadOnlyList<int> source, int enemyTypeCount)
    {
        List<int> result = new List<int>();
        if (enemyTypeCount <= 0)
            return result;

        if (source != null)
        {
            int sourceCount = source.Count;
            for (int i = 0; i < sourceCount; i++)
            {
                int index = source[i];
                if (index >= 0 && index < enemyTypeCount && !result.Contains(index))
                    result.Add(index);
            }
        }

        if (result.Count == 0)
        {
            for (int i = 0; i < enemyTypeCount; i++)
                result.Add(i);
        }

        return result;
    }

    private List<int> BuildDefaultAllowedTypes(int enemyTypeCount)
    {
        List<int> result = new List<int>(enemyTypeCount);
        for (int i = 0; i < enemyTypeCount; i++)
            result.Add(i);
        return result;
    }

    private int ResolveNextEnemyTypeIndex(in WaveSpawnAssignment assignment, List<WaveEnemyTypeState> states)
    {
        IReadOnlyList<int> allowedTypes = assignment.AllowedEnemyTypeIndices;
        if (allowedTypes == null || allowedTypes.Count == 0)
            return GetFirstAvailableEnemyTypeIndex(states);

        int allowedCount = allowedTypes.Count;
        for (int i = 0; i < allowedCount; i++)
        {
            int typeIndex = allowedTypes[i];
            if (typeIndex >= 0 && typeIndex < states.Count && states[typeIndex].RemainingCount > 0)
                return typeIndex;
        }

        return -1;
    }

    private int GetFirstAvailableEnemyTypeIndex(List<WaveEnemyTypeState> states)
    {
        int count = states.Count;
        for (int i = 0; i < count; i++)
        {
            if (states[i].RemainingCount > 0)
                return i;
        }

        return -1;
    }

    /// <summary>
    /// Resolves context data and spawns one enemy instance at the requested spawn node.
    /// </summary>
    private void SpawnEnemyInstance(EnemyClassDefinition definition, Vector2Int coords, EnemyRuntimeModifiers modifiers, Vector3 spawnOffset)
    {
        EnemyPoolSO pool = definition.EnemyPool;
        if (pool == null)
            return;

        Vector3 position = ResolveSpawnPosition(coords);
        Quaternion rotation = ResolveSpawnRotation(coords);
        Transform parent = ResolveSpawnParent(coords);
        EnemySpawnContext context = new EnemySpawnContext(definition, position, rotation, parent, modifiers, spawnOffset);
        pool.Spawn(definition, context);
    }

    /// <summary>
    /// Returns the spawn position for a spawn coordinate.
    /// </summary>
    private Vector3 ResolveSpawnPosition(Vector2Int coords)
    {
        if (grid == null)
            return Vector3.zero;

        return grid.GridToWorld(coords.x, coords.y);
    }

    /// <summary>
    /// Returns the spawn rotation for a spawn coordinate.
    /// </summary>
    private Quaternion ResolveSpawnRotation(Vector2Int coords)
    {
        if (grid == null)
            return Quaternion.identity;

        return Quaternion.identity;
    }

    /// <summary>
    /// Returns the parent transform for a spawn coordinate.
    /// </summary>
    private Transform ResolveSpawnParent(Vector2Int coords)
    {
        if (grid == null)
            return null;

        return grid.transform;
    }

    /// <summary>
    /// Handles post-horde cleanup, victory, or phase rollback to building.
    /// </summary>
    private void FinalizeHordeCompletion()
    {
        bool defeatRegistered = false;
        Player.PlayerHealth health = GetPlayerHealth();
        if (health != null)
        {
            health.RegisterHordeDefeat();
            defeatRegistered = true;
        }

        if (!defeatRegistered)
            EventsManager.InvokeIncreaseCompletedHordesCounter();

        if (HasPendingHordes)
        {
            GameManager targetManager = gameManager != null ? gameManager : GameManager.Instance;
            if (targetManager != null)
                targetManager.ForcePhase(GamePhase.Building);
        }
        else
        {
            EventsManager.InvokeGameVictoryAchieved();
        }
    }

    /// <summary>
    /// Locates the player health component once and caches it.
    /// </summary>
    private PlayerHealth GetPlayerHealth()
    {
        if (cachedPlayerHealth != null)
            return cachedPlayerHealth;

       // Debug.LogError("Missing serialized variable 'cachedPlayerHealth' in HordesManager. Recurring to reflection as an emergency measure.");
        cachedPlayerHealth = FindFirstObjectByType<Player.PlayerHealth>(FindObjectsInactive.Exclude);
        return cachedPlayerHealth;
    }

    private struct WaveEnemyTypeState
    {
        public EnemyClassDefinition Definition;
        public EnemyRuntimeModifiers Modifiers;
        public Vector3 SpawnOffset;
        public int RemainingCount;

        public WaveEnemyTypeState(EnemyClassDefinition definition, EnemyRuntimeModifiers modifiers, Vector3 spawnOffset, int remainingCount)
        {
            Definition = definition;
            Modifiers = modifiers;
            SpawnOffset = spawnOffset;
            RemainingCount = remainingCount;
        }
    }
    #endregion
    #endregion
}
