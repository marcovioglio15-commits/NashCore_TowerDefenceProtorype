using System.Collections;
using Scriptables.Turrets;
using UnityEngine;

namespace Player.Build
{
    /// <summary>
    /// Manages manual turret possession, camera interpolation, and gesture reinterpretation for free-aim mode.
    /// </summary>
    public class TurretFreeAimController : MonoBehaviour
    {
        #region Variables And Properties
        #region Serialized Fields
        [Header("Camera")]
        [Tooltip("Camera interpolated into turret perspective during free-aim control.")] [SerializeField] private Camera targetCamera;
        [Tooltip("Local offset from the turret anchor applied to the camera.")] [SerializeField] private Vector3 cameraLocalOffset = new Vector3(0f, 0.1f, -0.85f);
        [Tooltip("Seconds used for linear interpolation when entering free-aim.")] [SerializeField] private float enterLerpSeconds = 0.4f;
        [Tooltip("Seconds used for linear interpolation when exiting free-aim.")] [SerializeField] private float exitLerpSeconds = 0.35f;

        [Header("Rotation")]
        [Tooltip("Degrees of yaw applied per pixel of horizontal drag or swipe.")] [SerializeField] private float yawSensitivity = 0.35f;
        [Tooltip("Degrees of pitch applied per pixel of vertical drag or swipe.")] [SerializeField] private float pitchSensitivity = 0.3f;
        [Tooltip("Minimum pitch angle allowed while manually aiming.")] [SerializeField] private float minPitchDegrees = -30f;
        [Tooltip("Maximum pitch angle allowed while manually aiming.")] [SerializeField] private float maxPitchDegrees = 55f;

        [Header("Firing")]
        [Tooltip("Smallest cadence allowed to process manual tap firing.")] [SerializeField] private float minTapCadenceSeconds = 0.05f;
        #endregion

        #region Runtime State
        private PooledTurret activeTurret;
        private TurretAutoController cachedAutoController;
        private bool cachedAutoEnabled;
        private Transform cachedCameraParent;
        private Vector3 cachedCameraPosition;
        private Quaternion cachedCameraRotation;
        private bool hasCameraCache;
        private Coroutine cameraRoutine;
        private float fireCooldownTimer;
        private bool freeAimActive;
        #endregion
        #endregion

        #region Methods
        #region Unity
        /// <summary>
        /// Ensures the controlled camera reference is available.
        /// </summary>
        private void Awake()
        {
            if (targetCamera == null)
                targetCamera = Camera.main;
        }

        /// <summary>
        /// Wires up event handlers for perspective requests and gesture streams.
        /// </summary>
        private void OnEnable()
        {
            EventsManager.TurretPerspectiveRequested += HandlePerspectiveRequested;
            EventsManager.Drag += HandleAngularDrag;
            EventsManager.Swipe += HandleAngularSwipe;
            EventsManager.Tap += HandleTap;
            EventsManager.TurretFreeAimExitRequested += HandleExitRequested;
        }

        /// <summary>
        /// Cleans up listeners and restores any hijacked camera or turret state.
        /// </summary>
        private void OnDisable()
        {
            EventsManager.TurretPerspectiveRequested -= HandlePerspectiveRequested;
            EventsManager.Drag -= HandleAngularDrag;
            EventsManager.Swipe -= HandleAngularSwipe;
            EventsManager.Tap -= HandleTap;
            EventsManager.TurretFreeAimExitRequested -= HandleExitRequested;
            if (freeAimActive)
                EventsManager.InvokeTurretFreeAimEnded(activeTurret);
            StopActiveCameraRoutine();
            RestoreCameraTransform();
            ReleaseTurret();
        }

        /// <summary>
        /// Maintains cooldowns and safety exits without per-frame heavy work.
        /// </summary>
        private void Update()
        {
            if (fireCooldownTimer > 0f)
                fireCooldownTimer = Mathf.Max(0f, fireCooldownTimer - Time.deltaTime);

            if (freeAimActive && activeTurret != null && activeTurret.HasDefinition)
                activeTurret.CooldownHeat(Time.deltaTime);

            if (freeAimActive && (activeTurret == null || !activeTurret.gameObject.activeInHierarchy))
                ExitFreeAim();
        }
        #endregion

        #region Event Handlers
        /// <summary>
        /// Starts possession flow when the player holds a turret.
        /// </summary>
        private void HandlePerspectiveRequested(PooledTurret turret)
        {
            if (turret == null)
                return;

            if (freeAimActive && turret == activeTurret)
                return;

            if (freeAimActive)
                ExitFreeAim();

            BeginFreeAim(turret);
        }

        /// <summary>
        /// Applies yaw and pitch input from drag gestures.
        /// </summary>
        private void HandleAngularDrag(Vector2 delta)
        {
            if (!freeAimActive)
                return;

            ApplyAngularInput(delta);
        }

        /// <summary>
        /// Applies yaw and pitch input from swipe gestures.
        /// </summary>
        private void HandleAngularSwipe(Vector2 delta)
        {
            if (!freeAimActive)
                return;

            ApplyAngularInput(delta);
        }

        /// <summary>
        /// Issues a manual fire attempt when tapping in free-aim.
        /// </summary>
        private void HandleTap()
        {
            if (!freeAimActive)
                return;

            TryFire();
        }

        /// <summary>
        /// Leaves free-aim when the UI exit hold completes.
        /// </summary>
        private void HandleExitRequested()
        {
            if (!freeAimActive)
                return;

            ExitFreeAim();
        }
        #endregion

        #region Free-Aim Flow
        /// <summary>
        /// Performs state setup and camera interpolation for manual control.
        /// </summary>
        private void BeginFreeAim(PooledTurret targetTurret)
        {
            if (targetTurret == null || !targetTurret.HasDefinition)
                return;

            activeTurret = targetTurret;
            cachedAutoController = activeTurret.GetComponent<TurretAutoController>();
            cachedAutoEnabled = cachedAutoController != null && cachedAutoController.enabled;
            if (cachedAutoController != null)
                cachedAutoController.enabled = false;

            activeTurret.SetFreeAimState(true);
            freeAimActive = true;
            fireCooldownTimer = 0f;
            CacheCameraState();
            StartCameraLerpToTurret();
            EventsManager.InvokeTurretFreeAimStarted(activeTurret);
        }

        /// <summary>
        /// Restores previous state and camera placement.
        /// </summary>
        private void ExitFreeAim()
        {
            PooledTurret turretToRelease = activeTurret;
            if (freeAimActive && activeTurret != null)
                activeTurret.SetFreeAimState(false);

            RestoreAutoController();
            freeAimActive = false;
            activeTurret = null;
            fireCooldownTimer = 0f;
            StartCameraReturn();
            EventsManager.InvokeTurretFreeAimEnded(turretToRelease);
        }

        /// <summary>
        /// Restores turret and camera immediately when disabling the controller.
        /// </summary>
        private void ReleaseTurret()
        {
            if (activeTurret != null)
                activeTurret.SetFreeAimState(false);

            RestoreAutoController();
            activeTurret = null;
            freeAimActive = false;
            fireCooldownTimer = 0f;
        }
        #endregion

        #region Rotation And Fire
        /// <summary>
        /// Rotates the turret respecting sensitivity and turn rate constraints.
        /// </summary>
        private void ApplyAngularInput(Vector2 delta)
        {
            if (activeTurret == null)
                return;

            Transform yawTransform = activeTurret.YawPivot;
            Transform pitchTransform = activeTurret.PitchPivot;
            if (yawTransform == null && pitchTransform == null)
                return;

            TurretStatSnapshot stats = activeTurret.ActiveStats;
            float maxDegrees = stats.TurnRate * Time.deltaTime;
            if (maxDegrees <= 0f)
                return;

            float yawDelta = Mathf.Clamp(delta.x * yawSensitivity, -maxDegrees, maxDegrees);
            float pitchDelta = Mathf.Clamp(-delta.y * pitchSensitivity, -maxDegrees, maxDegrees);

            if (yawTransform != null)
                yawTransform.Rotate(Vector3.up, yawDelta, Space.World);

            if (pitchTransform != null)
            {
                Vector3 euler = pitchTransform.localEulerAngles;
                float normalized = NormalizeAngle(euler.x);
                normalized = Mathf.Clamp(normalized + pitchDelta, minPitchDegrees, maxPitchDegrees);
                pitchTransform.localRotation = Quaternion.Euler(normalized, 0f, 0f);
            }
        }

        /// <summary>
        /// Fires the turret once if cadence allows it.
        /// </summary>
        private void TryFire()
        {
            if (activeTurret == null || !activeTurret.HasDefinition)
                return;

            if (fireCooldownTimer > 0f)
                return;

            TurretStatSnapshot stats = activeTurret.ActiveStats;
            float cadence = Mathf.Max(minTapCadenceSeconds, stats.FreeAimCadenceSeconds);
            fireCooldownTimer = cadence;
            StartCoroutine(FireBurstRoutine(stats));
        }

        /// <summary>
        /// Executes manual burst spawning using free-aim fire settings.
        /// </summary>
        private IEnumerator FireBurstRoutine(TurretStatSnapshot stats)
        {
            if (activeTurret == null || !activeTurret.HasDefinition)
                yield break;

            int projectiles = Mathf.Max(1, stats.FreeAimProjectilesPerShot);
            TurretFirePattern pattern = stats.FreeAimPattern;
            bool useDelay = pattern == TurretFirePattern.Consecutive && stats.FreeAimInterProjectileDelay > 0f;
            WaitForSeconds delay = useDelay ? new WaitForSeconds(stats.FreeAimInterProjectileDelay) : null;
            Vector3 forward = ResolveFireForward();

            for (int i = 0; i < projectiles; i++)
            {
                Vector3 direction = TurretFireUtility.ResolveProjectileDirection(forward, pattern, stats.FreeAimConeAngleDegrees, i, projectiles);
                TurretFireUtility.SpawnProjectile(activeTurret, direction);

                bool shouldDelay = useDelay && i < projectiles - 1;
                if (shouldDelay && delay != null)
                    yield return delay;
            }
        }
        #endregion

        #region Camera
        /// <summary>
        /// Caches the camera transform before possession.
        /// </summary>
        private void CacheCameraState()
        {
            if (targetCamera == null)
                return;

            Transform cameraTransform = targetCamera.transform;
            cachedCameraParent = cameraTransform.parent;
            cachedCameraPosition = cameraTransform.position;
            cachedCameraRotation = cameraTransform.rotation;
            hasCameraCache = true;
        }

        /// <summary>
        /// Stops any active camera interpolation routines.
        /// </summary>
        private void StopActiveCameraRoutine()
        {
            if (cameraRoutine != null)
            {
                StopCoroutine(cameraRoutine);
                cameraRoutine = null;
            }
        }

        /// <summary>
        /// Starts interpolation into the turret viewpoint.
        /// </summary>
        private void StartCameraLerpToTurret()
        {
            if (targetCamera == null)
                return;

            StopActiveCameraRoutine();
            cameraRoutine = StartCoroutine(LerpCameraToTurret());
        }

        /// <summary>
        /// Starts interpolation back to the cached camera pose.
        /// </summary>
        private void StartCameraReturn()
        {
            if (targetCamera == null)
                return;

            if (!hasCameraCache)
                return;

            StopActiveCameraRoutine();
            cameraRoutine = StartCoroutine(LerpCameraToOriginal());
        }

        /// <summary>
        /// Interpolates the camera into the turret anchor and parents it.
        /// </summary>
        private IEnumerator LerpCameraToTurret()
        {
            if (activeTurret == null || targetCamera == null)
                yield break;

            Transform cameraTransform = targetCamera.transform;
            Transform anchor = ResolveCameraAnchor();
            if (anchor == null)
                yield break;

            Vector3 startPosition = cameraTransform.position;
            Quaternion startRotation = cameraTransform.rotation;
            float duration = Mathf.Max(0f, Mathf.Max(enterLerpSeconds, activeTurret.ActiveStats.ModeSwitchSeconds));
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float normalized = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
                Vector3 targetPosition = anchor.position + anchor.TransformVector(cameraLocalOffset);
                Quaternion targetRotation = anchor.rotation;
                cameraTransform.position = Vector3.Lerp(startPosition, targetPosition, normalized);
                cameraTransform.rotation = Quaternion.Lerp(startRotation, targetRotation, normalized);
                elapsed += Time.deltaTime;
                yield return null;
            }

            Vector3 finalPosition = anchor.position + anchor.TransformVector(cameraLocalOffset);
            cameraTransform.SetPositionAndRotation(finalPosition, anchor.rotation);
            cameraTransform.SetParent(anchor, true);
            cameraRoutine = null;
        }

        /// <summary>
        /// Interpolates the camera back to the cached pose and parent.
        /// </summary>
        private IEnumerator LerpCameraToOriginal()
        {
            Transform cameraTransform = targetCamera.transform;
            cameraTransform.SetParent(null, true);

            Vector3 startPosition = cameraTransform.position;
            Quaternion startRotation = cameraTransform.rotation;
            Vector3 targetPosition = cachedCameraPosition;
            Quaternion targetRotation = cachedCameraRotation;
            float duration = Mathf.Max(0f, exitLerpSeconds);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float normalized = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;
                cameraTransform.position = Vector3.Lerp(startPosition, targetPosition, normalized);
                cameraTransform.rotation = Quaternion.Lerp(startRotation, targetRotation, normalized);
                elapsed += Time.deltaTime;
                yield return null;
            }

            cameraTransform.SetPositionAndRotation(targetPosition, targetRotation);
            cameraTransform.SetParent(cachedCameraParent, true);
            hasCameraCache = false;
            cameraRoutine = null;
        }

        /// <summary>
        /// Restores the cached camera transform immediately.
        /// </summary>
        private void RestoreCameraTransform()
        {
            if (!hasCameraCache || targetCamera == null)
                return;

            Transform cameraTransform = targetCamera.transform;
            cameraTransform.SetParent(null, true);
            cameraTransform.SetPositionAndRotation(cachedCameraPosition, cachedCameraRotation);
            cameraTransform.SetParent(cachedCameraParent, true);
            hasCameraCache = false;
        }

        /// <summary>
        /// Resolves the transform used as camera anchor.
        /// </summary>
        private Transform ResolveCameraAnchor()
        {
            if (activeTurret == null)
                return null;

            if (activeTurret.Muzzle != null)
                return activeTurret.Muzzle;

            return activeTurret.transform;
        }
        #endregion

        #region Helpers
        /// <summary>
        /// Maps the current muzzle forward as firing direction fallback.
        /// </summary>
        private Vector3 ResolveFireForward()
        {
            if (activeTurret == null)
                return Vector3.forward;

            if (activeTurret.Muzzle != null)
                return activeTurret.Muzzle.forward;

            return activeTurret.transform.forward;
        }

        /// <summary>
        /// Normalizes angles into [-180, 180].
        /// </summary>
        private float NormalizeAngle(float angle)
        {
            float normalized = angle % 360f;
            if (normalized > 180f)
                normalized -= 360f;

            return normalized;
        }

        /// <summary>
        /// Restores the autonomous controller if it was previously active.
        /// </summary>
        private void RestoreAutoController()
        {
            if (cachedAutoController == null)
                return;

            cachedAutoController.enabled = cachedAutoEnabled;
            cachedAutoController = null;
            cachedAutoEnabled = false;
        }
        #endregion

        #region Gizmos
        /// <summary>
        /// Shows the target camera offset relative to the active turret anchor.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (activeTurret == null || targetCamera == null)
                return;

            Transform anchor = ResolveCameraAnchor();
            if (anchor == null)
                return;

            Vector3 offsetPosition = anchor.position + anchor.TransformVector(cameraLocalOffset);
            Gizmos.color = new Color(0.2f, 0.85f, 1f, 0.35f);
            Gizmos.DrawWireSphere(offsetPosition, 0.15f);
            Gizmos.DrawLine(anchor.position, offsetPosition);
        }
        #endregion
        #endregion
    }
}
