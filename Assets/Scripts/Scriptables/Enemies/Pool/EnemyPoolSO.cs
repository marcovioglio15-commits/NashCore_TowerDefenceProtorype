using UnityEngine;
using Enemy;

namespace Scriptables.Enemies
{
    /// <summary>
    /// Pool for enemy instances, providing warmup utilities and default definition binding.
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyPool", menuName = "Scriptables/Enemies/Enemy Pool")]
    public class EnemyPoolSO : APooInterface<PooledEnemy, EnemySpawnContext>
    {
        #region Serialized Fields

        [Tooltip("Default definition used when spawn contexts omit a definition reference.")]
        [Header("Defaults")]
        [SerializeField] private EnemyClassDefinition fallbackDefinition;

        [Tooltip("Instances pre-created on Initialize to avoid runtime allocations.")]
        [SerializeField] private int warmupCount = 6;

        #endregion

        #region Public API

        /// <summary>
        /// Prepares the pool by instantiating a configurable number of enemies.
        /// </summary>
        public void Warmup()
        {
            Initialize(warmupCount);
        }

        /// <summary>
        /// Spawns an enemy using the provided context and optional overriding definition.
        /// </summary>
        public PooledEnemy Spawn(EnemyClassDefinition definition, EnemySpawnContext context)
        {
            EnemySpawnContext resolved = context.WithDefinition(definition != null ? definition : fallbackDefinition);
            PooledEnemy enemyInstance = Spawn(resolved);
            return enemyInstance;
        }

        /// <summary>
        /// Updates the fallback definition that will be injected during reset.
        /// </summary>
        public void SetFallbackDefinition(EnemyClassDefinition definition)
        {
            fallbackDefinition = definition;
        }

        #endregion

        #region Overrides

        public override void BindPoolable(PooledEnemy poolable)
        {
            if (poolable == null)
            {
                return;
            }

            poolable.Despawn += Despawn;
        }

        public override void ResetPoolable(PooledEnemy poolable)
        {
            if (poolable == null)
            {
                return;
            }

            poolable.AssignDefaultDefinition(fallbackDefinition);
            poolable.ResetPoolable();
        }

        #endregion
    }
}
