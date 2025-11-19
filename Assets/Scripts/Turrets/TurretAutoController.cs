using System.Collections;
using UnityEngine;

namespace Scriptables.Turrets
{
    /// <summary>
    /// Automatic targeting and firing controller that keeps pooled turrets engaging enemies without manual input.
    /// </summary>
    [DisallowMultipleComponent]
    public class TurretAutoController : MonoBehaviour
    {
        #region Variables And Properties
        #region Serialized Fields
        [Tooltip("Turret component supplying placement, transforms and fire configuration.")][SerializeField] private PooledTurret turret;
        [Tooltip("Layer mask representing enemies that should be tracked and engaged.")][SerializeField] private LayerMask enemyLayers = ~0;
        [Tooltip("Maximum number of colliders processed per scan iteration.")][SerializeField] private int maxScanColliders = 16;
        [Tooltip("Fallback cadence used whenever the definition cadence is missing or invalid.")][SerializeField] private float fallbackCadenceSeconds = 0.35f;
        [Tooltip("Draws targeting debug gizmos when the turret is selected.")][SerializeField] private bool drawDebugGizmos = true;
        #endregion

        #region Runtime
        private Collider[] scanBuffer;
        private Collider activeTarget;
        private float retargetTimer;
        private float fireTimer;
        private Coroutine burstRoutine;
        private Vector3 lastAimPoint;
        #endregion
        #endregion

        #region Methods
        #region Unity
        /// <summary>
        /// Ensures runtime dependencies are cached before spawning from pools.
        /// </summary>
        private void Awake()
        {
            if (turret == null)
                turret = GetComponent<PooledTurret>();

            int bufferSize = Mathf.Max(1, maxScanColliders);
            scanBuffer = new Collider[bufferSize];
        }

        /// <summary>
        /// Resets runtime timers when the component becomes active.
        /// </summary>
        private void OnEnable()
        {
            retargetTimer = 0f;
            fireTimer = 0f;
            activeTarget = null;
            lastAimPoint = Vector3.zero;
        }

        /// <summary>
        /// Stops firing routines whenever the turret is disabled or despawned.
        /// </summary>
        private void OnDisable()
        {
            if (burstRoutine != null)
                StopCoroutine(burstRoutine);

            burstRoutine = null;
            activeTarget = null;
        }

        /// <summary>
        /// Handles retargeting cadence, aim blending and fire scheduling.
        /// </summary>
        private void Update()
        {
            if (turret == null || !turret.HasDefinition)
                return;

            float deltaTime = Time.deltaTime;
            turret.CooldownHeat(deltaTime);

            retargetTimer -= deltaTime;
            if (retargetTimer <= 0f)
            {
                retargetTimer = Mathf.Max(0.05f, turret.Definition.Targeting.RetargetInterval);
                AcquireTarget();
            }

            if (!ValidateActiveTarget())
                return;

            Vector3 aimPosition = activeTarget.bounds.center;
            lastAimPoint = aimPosition;

            Vector3 direction = aimPosition - turret.transform.position;
            if (direction.sqrMagnitude <= Mathf.Epsilon)
                return;

            turret.AimTowards(direction, deltaTime);

            fireTimer -= deltaTime;
            float cadence = Mathf.Max(fallbackCadenceSeconds, turret.Definition.AutomaticFire.CadenceSeconds);
            if (fireTimer <= 0f)
            {
                fireTimer = cadence;
                BeginVolley(direction.normalized);
            }
        }
        #endregion

        #region Targeting
        /// <summary>
        /// Attempts to select the closest valid enemy within the cone of fire.
        /// </summary>
        private void AcquireTarget()
        {
            if (turret == null || !turret.HasDefinition)
                return;

            int hits = Physics.OverlapSphereNonAlloc(turret.transform.position, turret.Definition.Targeting.Range, scanBuffer, enemyLayers, QueryTriggerInteraction.Ignore);
            float closestDistance = float.MaxValue;
            Collider bestCollider = null;

            for (int i = 0; i < hits; i++)
            {
                Collider candidate = scanBuffer[i];
                if (candidate == null || !candidate.gameObject.activeInHierarchy)
                    continue;

                Vector3 offset = candidate.bounds.center - turret.transform.position;
                float distance = offset.magnitude;
                if (distance <= turret.Definition.Targeting.DeadZoneRadius || distance >= closestDistance)
                    continue;

                bestCollider = candidate;
                closestDistance = distance;
            }

            activeTarget = bestCollider;
        }

        /// <summary>
        /// Validates that current target is still available and within range.
        /// </summary>
        private bool ValidateActiveTarget()
        {
            if (turret == null || !turret.HasDefinition)
                return false;

            if (activeTarget == null || !activeTarget.gameObject.activeInHierarchy)
                return false;

            Vector3 offset = activeTarget.bounds.center - turret.transform.position;
            float sqrDistance = offset.sqrMagnitude;
            float maxRange = turret.Definition.Targeting.Range;
            if (sqrDistance > maxRange * maxRange)
                return false;

            if (sqrDistance < turret.Definition.Targeting.DeadZoneRadius * turret.Definition.Targeting.DeadZoneRadius)
                return false;

            return true;
        }
        #endregion

        #region Firing
        /// <summary>
        /// Starts a new volley respecting definition parameters.
        /// </summary>
        private void BeginVolley(Vector3 forward)
        {
            if (burstRoutine != null)
                StopCoroutine(burstRoutine);

            burstRoutine = StartCoroutine(FireBurstRoutine(forward));
        }

        /// <summary>
        /// Executes projectile spawning respecting pattern and delays.
        /// </summary>
        private IEnumerator FireBurstRoutine(Vector3 forward)
        {
            if (turret == null || !turret.HasDefinition)
                yield break;

            TurretClassDefinition.FireModeSettings fireMode = turret.Definition.AutomaticFire;
            int projectiles = Mathf.Max(1, fireMode.ProjectilesPerShot);
            WaitForSeconds interDelay = null;
            bool useDelay = fireMode.Pattern == TurretFirePattern.Consecutive && fireMode.InterProjectileDelay > 0f;
            if (useDelay)
                interDelay = new WaitForSeconds(fireMode.InterProjectileDelay);

            for (int i = 0; i < projectiles; i++)
            {
                Vector3 direction = ResolveProjectileDirection(forward, fireMode, i, projectiles);
                SpawnProjectile(direction);

                bool shouldDelay = useDelay && i < projectiles - 1;
                if (shouldDelay && interDelay != null)
                    yield return interDelay;
            }
        }

        /// <summary>
        /// Computes the direction for a projectile respecting the configured fire pattern.
        /// </summary>
        private Vector3 ResolveProjectileDirection(Vector3 forward, TurretClassDefinition.FireModeSettings fireMode, int index, int total)
        {
            if (total <= 1 || fireMode.Pattern != TurretFirePattern.Cone)
                return forward;

            float totalAngle = fireMode.ConeAngleDegrees;
            if (total == 1 || totalAngle <= 0f)
                return forward;

            float step = totalAngle / (total - 1);
            float startAngle = -totalAngle * 0.5f;
            float angle = startAngle + step * index;
            Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);
            Vector3 adjusted = rotation * forward;
            return adjusted.normalized;
        }

        /// <summary>
        /// Spawns a projectile using the turret definition pools.
        /// </summary>
        private void SpawnProjectile(Vector3 direction)
        {
            if (turret == null || !turret.HasDefinition)
                return;

            TurretClassDefinition definition = turret.Definition;
            ProjectileDefinition projectileDefinition = definition.Projectile;
            ProjectilePoolSO pool = definition.ProjectilePool != null ? definition.ProjectilePool : projectileDefinition != null ? projectileDefinition.Pool : null;
            if (pool == null || projectileDefinition == null)
                return;

            Transform muzzle = turret.Muzzle != null ? turret.Muzzle : turret.transform;
            Vector3 position = muzzle.position;
            ProjectileSpawnContext context = new ProjectileSpawnContext(projectileDefinition, position, direction, 1f, null);
            pool.Spawn(projectileDefinition, context);
        }
        #endregion

        #region Gizmos
        /// <summary>
        /// Draws range and aim gizmos for debugging.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            if (!drawDebugGizmos || turret == null || !turret.HasDefinition)
                return;

            Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.35f);
            Gizmos.DrawWireSphere(turret.transform.position, turret.Definition.Targeting.Range);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(turret.transform.position, turret.Definition.Targeting.DeadZoneRadius);

            if (lastAimPoint != Vector3.zero)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(turret.transform.position, lastAimPoint);
            }
        }
        #endregion
        #endregion
    }
}
