using UnityEngine;

namespace Grid
{
    [System.Serializable]
    /// <summary>
    /// Represents a single logical cell of the gameplay grid using a compact bitmask state.
    /// </summary>
    public class GridNode
    {
        #region Properties

        // Flattened index inside grid array.
        public int Index { get; private set; }

        // Grid coordinate X.
        public int X { get; private set; }

        // Grid coordinate Z.
        public int Z { get; private set; }

        // World position of the node.
        public Vector3 WorldPosition { get; private set; }

        // Current bitmask state.
        public NodeState State { get; private set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Creates a new grid node with provided indices, world position and initial state flags.
        /// </summary>
        public GridNode(int index, int x, int z, Vector3 worldPosition, NodeState initialState)
        {
            Index = index;
            X = x;
            Z = z;
            WorldPosition = worldPosition;
            State = initialState;
        }

        #endregion

        #region State Helpers

        /// <summary>
        /// Returns true if the node has the given flag.
        /// </summary>
        public bool Is(NodeState flag)
        {
            return (State & flag) != 0;
        }

        /// <summary>
        /// Assigns or removes a specific state flag.
        /// </summary>
        public void SetState(NodeState flag, bool value)
        {
            if (value)
                State |= flag;
            else
                State &= ~flag;
        }

        /// <summary>
        /// Updates the node world position.
        /// </summary>
        public void SetWorldPosition(Vector3 position)
        {
            WorldPosition = position;
        }

        #endregion
    }
}
