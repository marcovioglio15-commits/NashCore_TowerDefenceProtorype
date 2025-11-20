using UnityEngine;

namespace Utils.Combat
{
    /// <summary>
    /// Describes an entity capable of inflicting damage.
    /// </summary>
    public interface IDamage
    {
        #region Properties
        float DamageAmount { get; }
        float CriticalChance { get; }
        float CriticalMultiplier { get; }
        GameObject Source { get; }
        #endregion
    }
}
