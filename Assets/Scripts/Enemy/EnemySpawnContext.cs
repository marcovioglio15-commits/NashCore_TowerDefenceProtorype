using System;
using UnityEngine;
using Scriptables.Enemies;

namespace Enemy
{
    /// <summary>
    /// Captures spawn parameters and runtime modifiers for pooled enemy instances.
    /// </summary>
    [Serializable]
    public struct EnemySpawnContext
    {
        #region Variables And Properties
        #region Serialized Fields

        [Tooltip("Definition used by the spawned enemy. When null, the pool default is applied.")]
        [Header("Definition")]
        [SerializeField] private EnemyClassDefinition definition;

        [Tooltip("Spawn world position.")]
        [Header("Transform")]
        [SerializeField] private Vector3 position;

        [Tooltip("Spawn rotation.")]
        [SerializeField] private Quaternion rotation;

        [Tooltip("Optional parent transform for hierarchy organization.")]
        [SerializeField] private Transform parent;

        [Tooltip("Runtime modifiers applied to allowed enemy statistics on spawn.")]
        [Header("Runtime Modifiers")]
        [SerializeField] private EnemyRuntimeModifiers runtimeModifiers;

        #endregion

        #region Properties

        public EnemyClassDefinition Definition
        {
            get { return definition; }
        }

        public Vector3 Position
        {
            get { return position; }
        }

        public Quaternion Rotation
        {
            get { return rotation; }
        }

        public Transform Parent
        {
            get { return parent; }
        }

        public EnemyRuntimeModifiers RuntimeModifiers
        {
            get { return runtimeModifiers; }
        }

        #endregion
        #endregion

        #region Methods
        #region Constructors

        public EnemySpawnContext(EnemyClassDefinition definition, Vector3 position, Quaternion rotation, Transform parent, EnemyRuntimeModifiers runtimeModifiers)
        {
            this.definition = definition;
            this.position = position;
            this.rotation = rotation;
            this.parent = parent;
            this.runtimeModifiers = runtimeModifiers;
        }

        public EnemySpawnContext(EnemyClassDefinition definition, Vector3 position, Quaternion rotation, Transform parent) : this(definition, position, rotation, parent, EnemyRuntimeModifiers.Identity)
        {
        }

        public EnemySpawnContext(EnemyClassDefinition definition, Vector3 position, Quaternion rotation) : this(definition, position, rotation, null, EnemyRuntimeModifiers.Identity)
        {
        }

        #endregion

        #region Helpers

        public EnemySpawnContext WithDefinition(EnemyClassDefinition newDefinition)
        {
            EnemySpawnContext updated = this;
            updated.definition = newDefinition;
            return updated;
        }

        public EnemySpawnContext WithParent(Transform newParent)
        {
            EnemySpawnContext updated = this;
            updated.parent = newParent;
            return updated;
        }

        public EnemySpawnContext WithRuntimeModifiers(EnemyRuntimeModifiers modifiers)
        {
            EnemySpawnContext updated = this;
            updated.runtimeModifiers = modifiers;
            return updated;
        }

        #endregion
        #endregion
    }
}
