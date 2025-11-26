using UnityEngine;
using Scriptables.Enemies;

namespace Enemy
{
    /// <summary>
    /// Immutable aggregate holding enemy statistics after applying runtime modifiers.
    /// </summary>
    public readonly struct EnemyStatSnapshot
    {
        #region Variables And Properties
        #region Durability
        public float MaxHealth { get; }
        public float DamageNegationPercent { get; }
        #endregion

        #region Mobility
        public float MovementSpeed { get; }
        #endregion

        #region Offense
        public float ShieldDamage { get; }
        #endregion

        #region Rewards
        public int ScrapValue { get; }
        #endregion

        #region Special Effects
        public EnemySpecialEffect SpecialEffect { get; }
        public float ContactRange { get; }
        #endregion
        #endregion

        #region Methods
        #region Constructors

        public EnemyStatSnapshot(float maxHealth, float damageNegationPercent, float movementSpeed, float shieldDamage, int scrapValue, EnemySpecialEffect specialEffect, float contactRange)
        {
            MaxHealth = maxHealth;
            DamageNegationPercent = damageNegationPercent;
            MovementSpeed = movementSpeed;
            ShieldDamage = shieldDamage;
            ScrapValue = scrapValue;
            SpecialEffect = specialEffect;
            ContactRange = contactRange;
        }

        #endregion

        #region Factory

        public static EnemyStatSnapshot Create(EnemyClassDefinition definition, EnemyRuntimeModifiers modifiers)
        {
            if (definition == null)
            {
                return default;
            }

            float maxHealth = Mathf.Max(1f, definition.Durability.MaxHealth);
            float damageNegationPercent = Mathf.Clamp(definition.Durability.DamageNegationPercent + modifiers.DamageNegationDelta, -1f, 0.95f);
            float movementSpeedMultiplier = modifiers.MovementSpeedMultiplier > 0f ? modifiers.MovementSpeedMultiplier : 1f;
            float movementSpeed = Mathf.Max(0.01f, definition.Mobility.MovementSpeed * movementSpeedMultiplier);
            float shieldDamage = Mathf.Max(0f, definition.Offense.ShieldDamage);
            float scrapValueMultiplier = modifiers.ScrapValueMultiplier > 0f ? modifiers.ScrapValueMultiplier : 1f;
            int scrapValue = Mathf.Max(0, Mathf.RoundToInt(definition.Rewards.ScrapValue * scrapValueMultiplier));
            float contactRange = Mathf.Max(0f, definition.Contact.ContactRange);

            EnemyStatSnapshot snapshot = new EnemyStatSnapshot(maxHealth, damageNegationPercent, movementSpeed, shieldDamage, scrapValue, definition.Contact.Effect, contactRange);
            return snapshot;
        }

        #endregion
        #endregion
    }

    /// <summary>
    /// Runtime modifiers permitted on enemy statistics; health and shield damage remain immutable.
    /// </summary>
    [System.Serializable]
    public struct EnemyRuntimeModifiers
    {
        #region Variables And Properties

        [Tooltip("Additive modifier applied to damage negation (percentage value).")]
        [SerializeField] private float damageNegationDelta;

        [Tooltip("Multiplier applied to base movement speed.")]
        [SerializeField] private float movementSpeedMultiplier;

        [Tooltip("Multiplier applied to scrap rewards.")]
        [SerializeField] private float scrapValueMultiplier;

        public float DamageNegationDelta { get { return damageNegationDelta; } }
        public float MovementSpeedMultiplier { get { return movementSpeedMultiplier; } }
        public float ScrapValueMultiplier { get { return scrapValueMultiplier; } }

        #endregion

        #region Methods
        #region Constructors

        public EnemyRuntimeModifiers(float damageNegationDelta, float movementSpeedMultiplier, float scrapValueMultiplier)
        {
            this.damageNegationDelta = damageNegationDelta;
            this.movementSpeedMultiplier = movementSpeedMultiplier;
            this.scrapValueMultiplier = scrapValueMultiplier;
        }

        #endregion

        #region Static Builders

        public static EnemyRuntimeModifiers Identity
        {
            get { return new EnemyRuntimeModifiers(0f, 1f, 1f); }
        }

        #endregion
        #endregion
    }
}
