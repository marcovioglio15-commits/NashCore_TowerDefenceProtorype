using System;
using UnityEngine;

namespace Scriptables.Turrets
{
    /// <summary>
    /// Defines projectile archetypes used by turret fire modes including damage model and pooling metadata.
    /// </summary>
    [CreateAssetMenu(fileName = "Projectile", menuName = "Scriptables/Turrets/Projectile")]
    public class ProjectileDefinition : ScriptableObject
    {
        #region Serialized Fields

        [Header("Identity")]
        [Tooltip("Unique identifier for this projectile archetype.")]
        [SerializeField]private string key = "projectile_default";
        [Tooltip("Optional visual icon used for UI previews.")]
        [SerializeField]private Sprite icon;

        [Header("Prefab And Pool")]
        [Tooltip("Pooled projectile prefab implementing PooledProjectile.")]
        [SerializeField] private PooledProjectile projectilePrefab;
        [Tooltip("Pool asset used to spawn projectiles of this type.")]
        [SerializeField] private ProjectilePoolSO pool;

        [Header("Damage Model")]
        [Tooltip("Base damage applied to each target hit.")]
        [SerializeField] private float damage = 12f;
        [Tooltip("Critical strike probability [0-1].")]
        [SerializeField, Range(0f, 1f)] private float criticalChance = 0.1f;
        [Tooltip("Damage multiplier applied on a successful critical strike.")]
        [SerializeField]private float criticalMultiplier = 1.5f;
        [Tooltip("Damage falloff per additional pierced target.")]
        [SerializeField] private float pierceFalloffRatio = 0.2f;

        [Header("Flight Behaviour")]
        [Tooltip("Units per second and direction speed of the projectile.")]
        [SerializeField] private float speed = 22f;
        [Tooltip("Number of enemies this projectile can pierce before despawning.")]
        [SerializeField]private int maxPiercedTargets = 1;
        [Tooltip("Lifetime in seconds before the projectile is automatically despawned.")]
        [SerializeField]private float lifetimeSeconds = 5f;
        [Tooltip("Maximum travel distance before forcing despawn even if lifetime remains.")]
        [SerializeField]private float maxDistance = 25f;
        [Tooltip("Radius for impact-based area damage. Set to zero for pure single target shots.")]
        [SerializeField]private float splashRadius = 0f;
        [Tooltip("Probability that this projectile applies a stagger or status effect on hit.")]
        [SerializeField] private float statusChance = 0.15f;
        [Tooltip("Seconds the applied status effect remains active.")]
        [SerializeField]private float statusDurationSeconds = 1.75f;
        #endregion

        #region Public Properties (Get)
        public string Key
        {
            get { return key; }
        }

        public Sprite Icon
        {
            get { return icon; }
        }

        public PooledProjectile ProjectilePrefab
        {
            get { return projectilePrefab; }
        }

        public ProjectilePoolSO Pool
        {
            get { return pool; }
        }

        public float Damage
        {
            get { return damage; }
        }

        public float CriticalChance
        {
            get { return criticalChance; }
        }

        public float CriticalMultiplier
        {
            get { return criticalMultiplier; }
        }

        public float PierceFalloffRatio
        {
            get { return pierceFalloffRatio; }
        }

        public float Speed
        {
            get { return speed; }
        }

        public int MaxPiercedTargets
        {
            get { return maxPiercedTargets; }
        }

        public float LifetimeSeconds
        {
            get { return lifetimeSeconds; }
        }

        public float MaxDistance
        {
            get { return maxDistance; }
        }

        public float SplashRadius
        {
            get { return splashRadius; }
        }

        public float StatusChance
        {
            get { return statusChance; }
        }

        public float StatusDurationSeconds
        {
            get { return statusDurationSeconds; }
        }
        #endregion
    }

    [Serializable]
    public struct ProjectileSpawnContext
    {
        [SerializeField]
        [Tooltip("Definition applied at spawn. When null the projectile's default definition is used.")]
        private ProjectileDefinition definition;

        [SerializeField]
        [Tooltip("World-space spawn position.")]
        private Vector3 position;

        [SerializeField]
        [Tooltip("World-space launch direction.")]
        private Vector3 direction;

        [SerializeField]
        [Tooltip("Multiplier applied to definition speed when spawned.")]
        private float speedMultiplier;

        [SerializeField]
        [Tooltip("Transform used as parent for organizational purposes.")]
        private Transform parent;

        public ProjectileDefinition Definition { get { return definition; } }
        public Vector3 Position { get { return position; } }
        public Vector3 Direction { get { return direction; } }
        public float SpeedMultiplier { get { return speedMultiplier; } }
        public Transform Parent { get { return parent; } }

        public ProjectileSpawnContext(ProjectileDefinition definition, Vector3 position, Vector3 direction, float speedMultiplier = 1f, Transform parent = null)
        {
            this.definition = definition;
            this.position = position;
            this.direction = direction.sqrMagnitude > 0f ? direction.normalized : Vector3.forward;
            this.speedMultiplier = Mathf.Max(0f, speedMultiplier);
            this.parent = parent;
        }

        public ProjectileSpawnContext WithDefinition(ProjectileDefinition projectileDefinition)
        {
            ProjectileSpawnContext updated = this;
            updated.definition = projectileDefinition;
            return updated;
        }
    }
}
