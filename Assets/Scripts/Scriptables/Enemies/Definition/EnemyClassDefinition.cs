using System;
using UnityEngine;
using Enemy;

namespace Scriptables.Enemies
{
    /// <summary>
    /// Defines an enemy archetype with durability, mobility, offense, rewards, and optional special effects.
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyClass", menuName = "Scriptables/Enemies/Enemy Class")]
    public class EnemyClassDefinition : ScriptableObject
    {
        #region Variables And Properties
        #region Serialized Fields

        [Tooltip("Unique identifier used to register this enemy archetype across systems.")]
        [Header("Identity")]
        [SerializeField] private string key = "enemy_default";

        [Tooltip("Display name shown in UI when the enemy is referenced.")]
        [SerializeField] private string displayName = "New Enemy";

        [Tooltip("Description presented in codex or debug panels.")]
        [SerializeField, TextArea] private string description;

        [Tooltip("Icon used for UI previews and wave lists.")]
        [SerializeField] private Sprite icon;

        [Tooltip("Runtime prefab spawned for this enemy. Must implement PooledEnemy.")]
        [Header("Prefabs And Pools")]
        [SerializeField] private PooledEnemy enemyPrefab;

        [Tooltip("Pool asset handling the lifecycle of enemy instances.")]
        [SerializeField] private EnemyPoolSO enemyPool;

        [Tooltip("Durability settings defining health and incoming damage scaling.")]
        [Header("Stats")]
        [SerializeField] private DurabilitySettings durability = new DurabilitySettings(100f, 0f);

        [Tooltip("Movement configuration determining travel speed on paths.")]
        [SerializeField] private MobilitySettings mobility = new MobilitySettings(2.5f);

        [Tooltip("Offensive payload delivered to player health on goal reach.")]
        [SerializeField] private OffenseSettings offense = new OffenseSettings(15f);

        [Tooltip("Rewards granted to the player upon destroying this enemy.")]
        [SerializeField] private RewardSettings rewards = new RewardSettings(10);

        [Tooltip("Contact policy applied when the enemy reaches players within range.")]
        [Header("Contact")]
        [SerializeField] private ContactSettings contact = new ContactSettings(EnemySpecialEffect.None, 0f);

        #endregion

        #region Properties

        /// <summary>
        /// Returns the unique identifier for this enemy archetype.
        /// </summary>
        public string Key
        {
            get { return key; }
        }

        /// <summary>
        /// Returns the display name for UI usage.
        /// </summary>
        public string DisplayName
        {
            get { return displayName; }
        }

        /// <summary>
        /// Returns the descriptive text for codex or menus.
        /// </summary>
        public string Description
        {
            get { return description; }
        }

        /// <summary>
        /// Returns the icon assigned to this archetype.
        /// </summary>
        public Sprite Icon
        {
            get { return icon; }
        }

        /// <summary>
        /// Returns the pooled prefab reference.
        /// </summary>
        public PooledEnemy EnemyPrefab
        {
            get { return enemyPrefab; }
        }

        /// <summary>
        /// Returns the pool handling this enemy type.
        /// </summary>
        public EnemyPoolSO EnemyPool
        {
            get { return enemyPool; }
        }

        /// <summary>
        /// Returns durability-related settings.
        /// </summary>
        public DurabilitySettings Durability
        {
            get { return durability; }
        }

        /// <summary>
        /// Returns mobility-related settings.
        /// </summary>
        public MobilitySettings Mobility
        {
            get { return mobility; }
        }

        /// <summary>
        /// Returns offense-related settings.
        /// </summary>
        public OffenseSettings Offense
        {
            get { return offense; }
        }

        /// <summary>
        /// Returns reward-related settings.
        /// </summary>
        public RewardSettings Rewards
        {
            get { return rewards; }
        }

        /// <summary>
        /// Returns contact settings including range and effect.
        /// </summary>
        public ContactSettings Contact
        {
            get { return contact; }
        }

        #endregion
        #endregion

        #region Nested Types

        [Serializable]
        public struct DurabilitySettings
        {
            [Tooltip("Maximum health points; immutable after spawn.")]
            [SerializeField] private float maxHealth;

            [Tooltip("Percentual damage negation applied to incoming damage; negative values amplify damage.")]
            [SerializeField] private float damageNegationPercent;

            public float MaxHealth
            {
                get { return maxHealth; }
            }

            public float DamageNegationPercent
            {
                get { return damageNegationPercent; }
            }

            public DurabilitySettings(float maxHealth, float damageNegationPercent)
            {
                this.maxHealth = Mathf.Max(1f, maxHealth);
                this.damageNegationPercent = damageNegationPercent;
            }
        }

        [Serializable]
        public struct MobilitySettings
        {
            [Tooltip("Base movement speed along the navigation path.")]
            [SerializeField] private float movementSpeed;

            public float MovementSpeed
            {
                get { return movementSpeed; }
            }

            public MobilitySettings(float movementSpeed)
            {
                this.movementSpeed = Mathf.Max(0.1f, movementSpeed);
            }
        }

        [Serializable]
        public struct OffenseSettings
        {
            [Tooltip("Damage applied to player health when the enemy reaches the goal; immutable by modifiers.")]
            [SerializeField] private float shieldDamage;

            public float ShieldDamage
            {
                get { return shieldDamage; }
            }

            public OffenseSettings(float shieldDamage)
            {
                this.shieldDamage = Mathf.Max(0f, shieldDamage);
            }
        }

        [Serializable]
        public struct RewardSettings
        {
            [Tooltip("Scraps granted on destruction before player modifiers.")]
            [SerializeField] private int scrapValue;

            public int ScrapValue
            {
                get { return scrapValue; }
            }

            public RewardSettings(int scrapValue)
            {
                this.scrapValue = Mathf.Max(0, scrapValue);
            }
        }

        [Serializable]
        public struct ContactSettings
        {
            [Tooltip("Special behavior executed when the enemy enters its trigger range.")]
            [SerializeField] private EnemySpecialEffect effect;

            [Tooltip("World-space radius used to trigger contact damage and special effects.")]
            [SerializeField] private float contactRange;

            public EnemySpecialEffect Effect
            {
                get { return effect; }
            }

            public float ContactRange
            {
                get { return contactRange; }
            }

            public ContactSettings(EnemySpecialEffect effect, float contactRange)
            {
                this.effect = effect;
                this.contactRange = Mathf.Max(0f, contactRange);
            }
        }

        #endregion
    }
}
