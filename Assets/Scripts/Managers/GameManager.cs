using Player.Build;
using Player.Inventory;
using UnityEngine;

/// <summary>
/// Orchestrates the high-level game loop by alternating between build and combat phases and notifying systems.
/// </summary>
public class GameManager : Singleton<GameManager>
{
    #region Variables And Properties
    #region Serialized Fields
    [Header("Phase Flow")]
    [Tooltip("Phase used at startup before the player triggers any phase changes.")]
    [SerializeField] private GamePhase initialPhase = GamePhase.Building;
    [Tooltip("Placement inventory that is toggled on during the build phase and silenced during defence.")]
    [SerializeField] private BuildablesInventory buildablesInventory;
    [Tooltip("Turret interaction controller used for reposition and possession gating.")]
    [SerializeField] private TurretInteractionController turretInteractionController;
    #endregion

    #region Runtime
    private GamePhase currentPhase;
    private bool isPaused;
    #endregion
    #endregion

    #region Properties
    /// <summary>
    /// Current active phase.
    /// </summary>
    public GamePhase CurrentPhase
    {
        get { return currentPhase; }
    }

    /// <summary>
    /// True when another phase switch is permitted by external flow controllers.
    /// </summary>
    public bool CanRequestPhaseChange
    {
        get
        {
            bool buildingPhase = currentPhase == GamePhase.Building;
            bool hordesAvailable = true;
            HordesManager hordeManager = HordesManager.Instance;
            if (hordeManager != null)
                hordesAvailable = hordeManager.HasPendingHordes;

            return buildingPhase && hordesAvailable;
        }
    }

    /// <summary>
    /// True when gameplay is currently paused.
    /// </summary>
    public bool IsGamePaused
    {
        get { return isPaused; }
    }
    #endregion

    #region Methods
    #region Unity
    /// <summary>
    /// Ensures the singleton wiring is intact before runtime.
    /// </summary>
    protected override void Awake()
    {
        base.Awake();
    }

    /// <summary>
    /// Subscribes to UI-driven phase requests.
    /// </summary>
    protected override void OnEnable()
    {
        base.OnEnable();
        EventsManager.GamePhaseAdvanceRequested += HandlePhaseAdvanceRequested;
    }

    /// <summary>
    /// Removes subscriptions when the manager is disabled.
    /// </summary>
    private void OnDisable()
    {
        EventsManager.GamePhaseAdvanceRequested -= HandlePhaseAdvanceRequested;
    }

    /// <summary>
    /// Applies the initial phase after all dependencies are ready.
    /// </summary>
    private void Start()
    {
        ApplyPhase(initialPhase, true);
    }
    #endregion

    #region Phase Flow
    /// <summary>
    /// Called by UI or external systems to move to the opposite phase.
    /// </summary>
    public void RequestPhaseAdvance()
    {
        if (!CanRequestPhaseChange)
            return;

        ApplyPhase(GamePhase.Defence, false);
    }

    /// <summary>
    /// Applies the requested phase, updates dependants, and broadcasts events.
    /// </summary>
    private void ApplyPhase(GamePhase phase, bool force)
    {
        if (!force && currentPhase == phase)
            return;

        currentPhase = phase;

        RefreshPhaseDependants(phase);
        EventsManager.InvokeGamePhaseChanged(phase);
    }

    /// <summary>
    /// Distributes phase state to build, interaction, and turret automation systems.
    /// </summary>
    private void RefreshPhaseDependants(GamePhase phase)
    {
        bool buildActive = phase == GamePhase.Building;

        if (buildablesInventory != null)
            buildablesInventory.SetBuildPhaseActive(buildActive);

        if (turretInteractionController != null)
            turretInteractionController.SetPhaseCapabilities(buildActive, !buildActive);
    }
    #endregion

    #region Pause
    public bool TogglePause()
    {
        ForcePause(!isPaused);
        return isPaused;
    }

    public void ForcePause(bool shouldPause, bool isInitialization = false)
    {
        if (isPaused == shouldPause)
            return;
        if (shouldPause)
        {
            Time.timeScale = 0f;
            isPaused = true;
            return;
        }

        Time.timeScale = 1f;
        isPaused = false;
    }
    #endregion

    #region Helpers
    /// <summary>
    /// Handles phase requests coming through the global event pipeline.
    /// </summary>
    private void HandlePhaseAdvanceRequested()
    {
        RequestPhaseAdvance();
    }

    /// <summary>
    /// Forces a phase change driven by external controllers like the horde manager.
    /// </summary>
    public void ForcePhase(GamePhase phase)
    {
        ApplyPhase(phase, false);
    }
    #endregion
    #endregion
}
