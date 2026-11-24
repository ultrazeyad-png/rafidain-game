using UnityEngine;

namespace Rafidain
{
    public interface IDamageable
    {
        /// <returns>true if the hit landed (false when dead or invulnerable).</returns>
        bool TakeDamage(float amount, Vector3 sourcePosition);
        bool IsDead { get; }
    }
}
