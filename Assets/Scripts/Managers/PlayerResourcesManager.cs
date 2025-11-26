using UnityEngine;

/// <summary>
/// Tracks the player's Scrap currency, handles spend and earn requests, and broadcasts updates.
/// </summary>
public class PlayerResourcesManager : Singleton<PlayerResourcesManager>
{
    #region Variables And Properties
    #region Serialized Fields
    [Header("Scrap Settings")]
    [Tooltip("Amount of Scrap granted to the player at the start of the session.")]
    [SerializeField] private int startingScrap = 500;
    [Tooltip("Maximum Scrap allowed; zero means no cap.")]
    [SerializeField] private int ScrapCap;
    #endregion

    #region Runtime
    private int currentScrap;
    #endregion
    #endregion

    #region Properties
    /// <summary>
    /// Current Scrap balance.
    /// </summary>
    public int CurrentScrap
    {
        get { return currentScrap; }
    }
    #endregion

    #region Methods
    #region Unity
    /// <summary>
    /// Initializes the starting balance.
    /// </summary>
    protected override void Awake()
    {
        base.Awake();
        ClampConfiguration();
        currentScrap = Mathf.Max(0, startingScrap);
    }

    /// <summary>
    /// Subscribes to earn events and emits the initial balance.
    /// </summary>
    protected override void OnEnable()
    {
        base.OnEnable();
        EventsManager.PlayerScrapEarned += HandleScrapEarned;
        BroadcastScrap();
    }

    /// <summary>
    /// Cleans up listeners.
    /// </summary>
    private void OnDisable()
    {
        EventsManager.PlayerScrapEarned -= HandleScrapEarned;
    }
    #endregion

    #region Public
    /// <summary>
    /// Returns true when the player can afford the requested cost.
    /// </summary>
    public bool CanAfford(int cost)
    {
        return cost <= currentScrap;
    }

    /// <summary>
    /// Attempts to spend Scrap; returns false if insufficient funds.
    /// </summary>
    public bool TrySpend(int cost)
    {
        if (cost <= 0)
            return true;

        if (!CanAfford(cost))
            return false;

        currentScrap -= cost;
        BroadcastScrap();
        return true;
    }

    /// <summary>
    /// Adds Scrap and clamps to the configured cap.
    /// </summary>
    public void Earn(int amount)
    {
        if (amount == 0)
            return;

        currentScrap = Mathf.Max(0, currentScrap + amount);
        if (ScrapCap > 0)
            currentScrap = Mathf.Min(currentScrap, ScrapCap);

        BroadcastScrap();
    }
    #endregion

    #region Helpers
    /// <summary>
    /// Responds to external earn events.
    /// </summary>
    private void HandleScrapEarned(int amount)
    {
        Earn(amount);
    }

    /// <summary>
    /// Emits the current Scrap balance through the global event pipeline.
    /// </summary>
    private void BroadcastScrap()
    {
        EventsManager.InvokePlayerScrapChanged(currentScrap);
    }

    /// <summary>
    /// Clamps serialized fields to valid ranges.
    /// </summary>
    private void ClampConfiguration()
    {
        if (startingScrap < 0)
            startingScrap = 0;

        if (ScrapCap < 0)
            ScrapCap = 0;
    }
    #endregion
    #endregion
}
