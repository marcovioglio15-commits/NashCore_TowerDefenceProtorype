using UnityEngine;

/// <summary>
/// Tracks the player's gold currency, handles spend and earn requests, and broadcasts updates.
/// </summary>
public class PlayerResourcesManager : Singleton<PlayerResourcesManager>
{
    #region Variables And Properties
    #region Serialized Fields
    [Header("Gold Settings")]
    [Tooltip("Amount of gold granted to the player at the start of the session.")]
    [SerializeField] private int startingGold = 500;
    [Tooltip("Maximum gold allowed; zero means no cap.")]
    [SerializeField] private int goldCap;
    #endregion

    #region Runtime
    private int currentGold;
    #endregion
    #endregion

    #region Properties
    /// <summary>
    /// Current gold balance.
    /// </summary>
    public int CurrentGold
    {
        get { return currentGold; }
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
        currentGold = Mathf.Max(0, startingGold);
    }

    /// <summary>
    /// Subscribes to earn events and emits the initial balance.
    /// </summary>
    protected override void OnEnable()
    {
        base.OnEnable();
        EventsManager.PlayerGoldEarned += HandleGoldEarned;
        BroadcastGold();
    }

    /// <summary>
    /// Cleans up listeners.
    /// </summary>
    private void OnDisable()
    {
        EventsManager.PlayerGoldEarned -= HandleGoldEarned;
    }
    #endregion

    #region Public
    /// <summary>
    /// Returns true when the player can afford the requested cost.
    /// </summary>
    public bool CanAfford(int cost)
    {
        return cost <= currentGold;
    }

    /// <summary>
    /// Attempts to spend gold; returns false if insufficient funds.
    /// </summary>
    public bool TrySpend(int cost)
    {
        if (cost <= 0)
            return true;

        if (!CanAfford(cost))
            return false;

        currentGold -= cost;
        BroadcastGold();
        return true;
    }

    /// <summary>
    /// Adds gold and clamps to the configured cap.
    /// </summary>
    public void Earn(int amount)
    {
        if (amount == 0)
            return;

        currentGold = Mathf.Max(0, currentGold + amount);
        if (goldCap > 0)
            currentGold = Mathf.Min(currentGold, goldCap);

        BroadcastGold();
    }
    #endregion

    #region Helpers
    /// <summary>
    /// Responds to external earn events.
    /// </summary>
    private void HandleGoldEarned(int amount)
    {
        Earn(amount);
    }

    /// <summary>
    /// Emits the current gold balance through the global event pipeline.
    /// </summary>
    private void BroadcastGold()
    {
        EventsManager.InvokePlayerGoldChanged(currentGold);
    }

    /// <summary>
    /// Clamps serialized fields to valid ranges.
    /// </summary>
    private void ClampConfiguration()
    {
        if (startingGold < 0)
            startingGold = 0;

        if (goldCap < 0)
            goldCap = 0;
    }
    #endregion
    #endregion
}
