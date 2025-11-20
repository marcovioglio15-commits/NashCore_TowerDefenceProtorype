using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// <summary>
/// Centralized mobile gesture manager built on the new Input System (tap, hold, drag, swipe, pinch).
/// </summary>
[DefaultExecutionOrder(-200)]
public class InputManager : Singleton<InputManager>
{
    #region Variables And Properties
    #region Serialized Configuration
    [Header("Swipe detection")]
    [Tooltip("Minimum travel distance in pixels to classify the gesture as a swipe.")]
    [SerializeField] private float swipeDistanceThreshold = 80f;
    [Tooltip("Maximum time in seconds allowed to cover the swipe distance.")]
    [SerializeField] private float swipeTimeThreshold = 0.35f;

    [Header("Drag detection")]
    [Tooltip("Minimum travel distance in pixels before a drag begins transmitting delta.")]
    [SerializeField] private float dragStartDistanceThreshold = 8f;

    [Header("Hold detection")]
    [Tooltip("Minimum press duration in seconds before a hold is emitted.")]
    [SerializeField] private float holdDurationThreshold = 0.5f;

    [Header("Pinch detection")]
    [Tooltip("Minimum change in distance between the two touches to register a pinch.")]
    [SerializeField] private float pinchDistanceThreshold = 5f;
    #endregion

    #region Runtime State
    public float SwipeDistanceThreshold => swipeDistanceThreshold;
    public float SwipeTimeThreshold => swipeTimeThreshold;
    public float DragStartDistanceThreshold => dragStartDistanceThreshold;
    public float PinchDistanceThreshold => pinchDistanceThreshold;

    private bool primaryGestureActive;
    private int primaryFingerId = -1;
    private Vector2 primaryStartPosition;
    private Vector2 primaryLastPosition;
    private double primaryStartTime;
    private bool holdRaised;
    private bool swipeRaised;
    private bool dragActive;

    private bool pinchActive;
    private Vector2 pinchPreviousVector;
    #endregion
    #endregion

    #region Methods
    #region Unity Lifecycle
    /// <summary>
    /// Ensures enhanced touch is available before gameplay.
    /// </summary>
    protected override void Awake()
    {
        base.Awake();
        EnableTouchPipeline();
    }

    /// <summary>
    /// Keeps touch helpers active when the manager is enabled.
    /// </summary>
    protected override void OnEnable()
    {
        base.OnEnable();
        EnableTouchPipeline();
    }

    /// <summary>
    /// Shuts down touch helpers on disable.
    /// </summary>
    private void OnDisable()
    {
        ResetGestureState();
        ResetPinchState();

        EnhancedTouchSupport.Disable();
        TouchSimulation.Disable();
    }

    /// <summary>
    /// Evaluates gesture state every frame.
    /// </summary>
    private void Update()
    {
        if (!EnhancedTouchSupport.enabled)
            EnableTouchPipeline();

        ProcessTouchGestures();
    }
    #endregion

    #region Public Configuration
    /// <summary>
    /// Updates the swipe distance threshold.
    /// </summary>
    public void SetSwipeDistanceThreshold(float value)
    {
        swipeDistanceThreshold = Mathf.Max(0f, value);
    }

    /// <summary>
    /// Updates the swipe time window.
    /// </summary>
    public void SetSwipeTimeThreshold(float value)
    {
        swipeTimeThreshold = Mathf.Max(0.01f, value);
    }

    /// <summary>
    /// Updates the drag activation distance.
    /// </summary>
    public void SetDragStartDistanceThreshold(float value)
    {
        dragStartDistanceThreshold = Mathf.Max(0f, value);
    }

    /// <summary>
    /// Updates the pinch activation delta.
    /// </summary>
    public void SetPinchDistanceThreshold(float value)
    {
        pinchDistanceThreshold = Mathf.Max(0f, value);
    }
    #endregion

    #region Update Loop
    /// <summary>
    /// Central touch evaluator that routes to pinch or single-finger processing.
    /// </summary>
    private void ProcessTouchGestures()
    {
        if (!EnhancedTouchSupport.enabled)
            return;

        if (Touch.activeTouches.Count >= 2)
        {
            HandlePinchGesture();
            CancelPrimaryGesture();
            return;
        }

        ResetPinchState();

        if (Touch.activeTouches.Count == 0)
        {
            if (primaryGestureActive)
                FinalizePrimaryGesture();

            return;
        }

        Touch primaryTouch = GetPrimaryTouch();

        if (!primaryTouch.isInProgress)
        {
            if (primaryGestureActive)
                FinalizePrimaryGesture();

            return;
        }

        if (!primaryGestureActive)
            BeginPrimaryGesture(primaryTouch);

        UpdatePrimaryGesture(primaryTouch);
    }
    #endregion

    #region Touch Pipeline
    /// <summary>
    /// Activates enhanced touch support and mouse-to-touch simulation when available.
    /// </summary>
    private void EnableTouchPipeline()
    {
        EnhancedTouchSupport.Enable();
        TouchSimulation.Enable();
    }

    /// <summary>
    /// Returns the active touch tracked as the primary gesture.
    /// </summary>
    private Touch GetPrimaryTouch()
    {
        for (int i = 0; i < Touch.activeTouches.Count; i++)
        {
            Touch candidate = Touch.activeTouches[i];
            if (primaryGestureActive && candidate.finger.index == primaryFingerId)
                return candidate;
        }

        return Touch.activeTouches[0];
    }
    #endregion

    #region Single Finger Gestures
    /// <summary>
    /// Initializes state for a new primary contact.
    /// </summary>
    private void BeginPrimaryGesture(Touch primaryTouch)
    {
        primaryGestureActive = true;
        primaryFingerId = primaryTouch.finger.index;
        primaryStartPosition = primaryTouch.screenPosition;
        primaryLastPosition = primaryStartPosition;
        primaryStartTime = Time.timeAsDouble;
        holdRaised = false;
        swipeRaised = false;
        dragActive = false;
    }

    /// <summary>
    /// Processes primary contact movement, hold, swipe, and drag.
    /// </summary>
    private void UpdatePrimaryGesture(Touch primaryTouch)
    {
        Vector2 currentPosition = primaryTouch.screenPosition;
        Vector2 displacement = currentPosition - primaryStartPosition;
        double elapsed = Time.timeAsDouble - primaryStartTime;

        if (!holdRaised)
        {
            bool minimalMovement = displacement.magnitude <= dragStartDistanceThreshold;
            if (elapsed >= holdDurationThreshold && minimalMovement)
            {
                EventsManager.InvokeHold();
                holdRaised = true;
            }
        }

        if (!swipeRaised)
        {
            bool swipeReady = displacement.magnitude >= swipeDistanceThreshold && elapsed <= swipeTimeThreshold;
            if (swipeReady)
            {
                EventsManager.InvokeSwipe(displacement);
                swipeRaised = true;
            }
        }

        if (!dragActive && !swipeRaised && displacement.magnitude >= dragStartDistanceThreshold)
            dragActive = true;

        if (dragActive)
        {
            Vector2 frameDelta = currentPosition - primaryLastPosition;
            if (frameDelta.sqrMagnitude > 0f)
                EventsManager.InvokeDrag(frameDelta);
        }

        primaryLastPosition = currentPosition;

        if (primaryTouch.phase == UnityEngine.InputSystem.TouchPhase.Ended || primaryTouch.phase == UnityEngine.InputSystem.TouchPhase.Canceled)
            FinalizePrimaryGesture();
    }

    /// <summary>
    /// Dispatches final gesture events on release and resets state.
    /// </summary>
    private void FinalizePrimaryGesture()
    {
        Vector2 totalDisplacement = primaryLastPosition - primaryStartPosition;
        double elapsed = Time.timeAsDouble - primaryStartTime;

        bool tapped = !swipeRaised && !dragActive && !holdRaised && totalDisplacement.magnitude <= dragStartDistanceThreshold;
        if (tapped)
            EventsManager.InvokeTap(primaryLastPosition);

        bool qualifiesLateSwipe = !swipeRaised && totalDisplacement.magnitude >= swipeDistanceThreshold && elapsed <= swipeTimeThreshold;
        if (qualifiesLateSwipe)
            EventsManager.InvokeSwipe(totalDisplacement);

        ResetGestureState();
    }

    /// <summary>
    /// Cancels the primary gesture without dispatching taps.
    /// </summary>
    private void CancelPrimaryGesture()
    {
        ResetGestureState();
    }

    /// <summary>
    /// Clears primary gesture tracking data.
    /// </summary>
    private void ResetGestureState()
    {
        primaryGestureActive = false;
        primaryFingerId = -1;
        primaryStartPosition = Vector2.zero;
        primaryLastPosition = Vector2.zero;
        primaryStartTime = 0d;
        holdRaised = false;
        swipeRaised = false;
        dragActive = false;
    }
    #endregion

    #region Pinch Gestures
    /// <summary>
    /// Handles pinch detection and dispatch.
    /// </summary>
    private void HandlePinchGesture()
    {
        if (Touch.activeTouches.Count < 2)
        {
            ResetPinchState();
            return;
        }

        Touch firstTouch = Touch.activeTouches[0];
        Touch secondTouch = Touch.activeTouches[1];

        if (!firstTouch.isInProgress || !secondTouch.isInProgress)
        {
            ResetPinchState();
            return;
        }

        Vector2 currentVector = secondTouch.screenPosition - firstTouch.screenPosition;

        if (!pinchActive)
        {
            pinchPreviousVector = currentVector;
            pinchActive = true;
            return;
        }

        float magnitudeDelta = Mathf.Abs(currentVector.magnitude - pinchPreviousVector.magnitude);
        if (magnitudeDelta >= pinchDistanceThreshold)
        {
            Vector2 delta = currentVector - pinchPreviousVector;
            if (currentVector.magnitude < pinchPreviousVector.magnitude)
                EventsManager.InvokePinchIn(delta);
            else
                EventsManager.InvokePinchOut(delta);
        }

        pinchPreviousVector = currentVector;
    }

    /// <summary>
    /// Resets pinch tracking.
    /// </summary>
    private void ResetPinchState()
    {
        pinchActive = false;
        pinchPreviousVector = Vector2.zero;
    }
    #endregion
    #endregion
}
