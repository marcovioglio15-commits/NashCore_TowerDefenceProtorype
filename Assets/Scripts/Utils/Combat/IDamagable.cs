using UnityEngine;

namespace Utils.Combat
{
    /// <summary>
    /// Describes a target that can receive damage from an IDamage source.
    /// </summary>
    public interface IDamagable
    {
        #region Methods
        void ApplyDamage(IDamage damageSource, Vector3 hitPoint);
        #endregion
    }
}
