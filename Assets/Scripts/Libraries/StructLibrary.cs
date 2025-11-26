using Enemy;
using Scriptables.Enemies;
using Scriptables.Turrets;
using System.Collections.Generic;
using UnityEngine;

namespace Player.Inventory
{
    /// <summary>
    /// Data object describing a placement preview result broadcast to the UI.
    /// </summary>
    public readonly struct BuildPreviewData
    {
        #region Variables And Properties
        #region Serialized-Like Fields
        public TurretClassDefinition Definition { get; }
        public Vector3 WorldPosition { get; }
        public Vector2Int Cell { get; }
        public bool HasValidCell { get; }
        #endregion
        #endregion

        #region Methods
        /// <summary>
        /// Builds a new immutable preview description.
        /// </summary>
        public BuildPreviewData(TurretClassDefinition definition, Vector3 worldPosition, Vector2Int cell, bool hasValidCell)
        {
            Definition = definition;
            WorldPosition = worldPosition;
            Cell = cell;
            HasValidCell = hasValidCell;
        }
        #endregion
    }

    /// <summary>
    /// Result payload emitted whenever a placement attempt finishes.
    /// </summary>
    public readonly struct BuildPlacementResult
    {
        #region Variables And Properties
        #region Serialized-Like Fields
        public TurretClassDefinition Definition { get; }
        public bool Success { get; }
        public string FailureReason { get; }
        public Vector3 WorldPosition { get; }
        public Vector2Int Cell { get; }
        #endregion
        #endregion

        #region Methods
        /// <summary>
        /// Creates a new immutable placement result.
        /// </summary>
        public BuildPlacementResult(TurretClassDefinition definition, bool success, string failureReason, Vector3 worldPosition, Vector2Int cell)
        {
            Definition = definition;
            Success = success;
            FailureReason = failureReason;
            WorldPosition = worldPosition;
            Cell = cell;
        }
        #endregion
    }
}

/// <summary>
/// Holds distance and predecessor information computed by Dijkstra traversal.
/// </summary>
[System.Serializable]
public readonly struct DijkstraInfo
{
    public int[] Distances { get; }
    public int[] Previous { get; }

    public DijkstraInfo(in int[] distances, in int[] previous)
    {
        Distances = distances;
        Previous = previous;
    }
}

/// <summary>
/// Describes an enemy archetype used inside a wave and its specific parameters.
/// </summary>
[System.Serializable]
public struct WaveEnemyType
{
    [Tooltip("Optional friendly name shown in inspectors.")]
    [SerializeField] private string label;

    [Tooltip("Enemy archetype spawned for this entry.")]
    [SerializeField] private EnemyClassDefinition enemyDefinition;

    [Tooltip("Runtime modifiers applied to this enemy type on spawn.")]
    [SerializeField] private EnemyRuntimeModifiers runtimeModifiers;

    [Tooltip("Total number of enemies spawned for this type.")]
    [SerializeField] private int enemyCount;

    [Tooltip("Offset applied to the resolved spawn position for this enemy type.")]
    [SerializeField] private Vector3 spawnOffset;

    public string Label { get { return string.IsNullOrWhiteSpace(label) ? (enemyDefinition != null ? enemyDefinition.name : "Enemy") : label; } }
    public EnemyClassDefinition EnemyDefinition { get { return enemyDefinition; } }
    public EnemyRuntimeModifiers RuntimeModifiers { get { return runtimeModifiers; } }
    public int EnemyCount { get { return enemyCount; } }
    public Vector3 SpawnOffset { get { return spawnOffset; } }

    public WaveEnemyType(EnemyClassDefinition definition, EnemyRuntimeModifiers runtimeModifiers, int enemyCount, Vector3 spawnOffset)
    {
        label = definition != null ? definition.name : string.Empty;
        enemyDefinition = definition;
        this.runtimeModifiers = runtimeModifiers;
        this.enemyCount = enemyCount;
        this.spawnOffset = spawnOffset;
    }
}

/// <summary>
/// Maps a spawn node to the list of enemy type indices that can use it.
/// </summary>
[System.Serializable]
public struct WaveSpawnAssignment
{
    [Tooltip("Grid coordinate for this spawn lane.")]
    [SerializeField] private Vector2Int spawnNode;

    [Tooltip("Indices into the wave's enemy type list that this spawner may emit.")]
    [SerializeField] private List<int> allowedEnemyTypeIndices;

    public Vector2Int SpawnNode { get { return spawnNode; } }
    public IReadOnlyList<int> AllowedEnemyTypeIndices { get { return allowedEnemyTypeIndices != null ? allowedEnemyTypeIndices : System.Array.Empty<int>(); } }

    public WaveSpawnAssignment(Vector2Int spawnNode, List<int> allowedEnemyTypeIndices)
    {
        this.spawnNode = spawnNode;
        this.allowedEnemyTypeIndices = allowedEnemyTypeIndices;
    }
}

/// <summary>
/// Groups multiple waves executed during a single defence phase.
/// </summary>
[System.Serializable]
public struct HordeDefinition
{
    [Tooltip("Identifier used in debug panels or logs.")]
    [SerializeField] private string key;

    [Tooltip("Waves executed sequentially during this horde.")]
    [SerializeField] private List<HordeWave> waves;

    public string Key { get { return key; } }
    public IReadOnlyList<HordeWave> Waves { get { return waves != null ? waves : System.Array.Empty<HordeWave>(); } }
}

/// <summary>
/// Configures a single wave with enemy type, spawn cadence, and start mode.
/// </summary>
[System.Serializable]
public struct HordeWave
{
    [Tooltip("Enemy archetypes spawned in this wave.")]
    [SerializeField] private List<WaveEnemyType> enemyTypes;

    [Tooltip("Seconds between spawns for this wave.")]
    [SerializeField] private float spawnCadenceSeconds;

    [Tooltip("Spawn nodes used for this wave. Nodes must be marked as enemy spawns in the grid.")]
    [SerializeField] private List<Vector2Int> spawnNodes;

    [Tooltip("Optional per-spawner restrictions to dictate which enemy types each node can emit. When empty, all enemy types are allowed on every spawn node defined above.")]
    [SerializeField] private List<WaveSpawnAssignment> spawnAssignments;

    [Tooltip("Mode controlling when the next wave begins.")]
    [SerializeField] private WaveAdvanceMode advanceMode;

    [Tooltip("Delay applied before the next wave starts. Applied after the last enemy spawn or after full clear based on the advance mode.")]
    [SerializeField] private float advanceDelaySeconds;

    [Tooltip("Legacy single-enemy definition used for scenes created before multi-type waves existed. Leave empty when using Enemy Types.")]
    [SerializeField, HideInInspector] private EnemyClassDefinition enemyDefinition;

    [SerializeField, HideInInspector] private EnemyRuntimeModifiers runtimeModifiers;
    [SerializeField, HideInInspector] private int enemyCount;
    [SerializeField, HideInInspector] private Vector3 spawnOffset;

    public IReadOnlyList<WaveEnemyType> EnemyTypes { get { return enemyTypes != null ? enemyTypes : System.Array.Empty<WaveEnemyType>(); } }
    public IReadOnlyList<WaveSpawnAssignment> SpawnAssignments { get { return spawnAssignments != null ? spawnAssignments : System.Array.Empty<WaveSpawnAssignment>(); } }
    public IReadOnlyList<Vector2Int> SpawnNodes { get { return spawnNodes != null ? spawnNodes : System.Array.Empty<Vector2Int>(); } }
    public float SpawnCadenceSeconds { get { return spawnCadenceSeconds; } }
    public WaveAdvanceMode AdvanceMode { get { return advanceMode; } }
    public float AdvanceDelaySeconds { get { return advanceDelaySeconds; } }

    public bool HasLegacyEnemy { get { return enemyDefinition != null; } }
    public EnemyClassDefinition LegacyEnemyDefinition { get { return enemyDefinition; } }
    public EnemyRuntimeModifiers LegacyRuntimeModifiers { get { return runtimeModifiers; } }
    public int LegacyEnemyCount { get { return enemyCount; } }
    public Vector3 LegacySpawnOffset { get { return spawnOffset; } }
}
