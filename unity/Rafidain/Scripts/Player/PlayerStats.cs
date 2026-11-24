using System;
using UnityEngine;

namespace Rafidain
{
    /// <summary>Furat's health, stamina, healing flasks and embers (the game's currency).</summary>
    public class PlayerStats : MonoBehaviour, IDamageable
    {
        [Header("Health / Stamina")]
        public float maxHealth = 100f;
        public float maxStamina = 100f;
        public float staminaRegen = 35f;
        public float staminaRegenDelay = 0.6f;

        [Header("Flasks")]
        public int maxFlasks = 4;
        [Range(0f, 1f)] public float flaskHealPercent = 0.45f;

        public float Health { get; private set; }
        public float Stamina { get; private set; }
        public int Flasks { get; private set; }
        public int Embers { get; set; }
        public bool IsDead { get; private set; }

        public float HealthRatio => maxHealth > 0 ? Health / maxHealth : 0f;
        public float StaminaRatio => maxStamina > 0 ? Stamina / maxStamina : 0f;

        public event Action OnDamaged;

        float lastStaminaUse = -10f;
        PlayerController controller;

        void Awake()
        {
            controller = GetComponent<PlayerController>();
            RestoreAll();
        }

        void Update()
        {
            if (IsDead) return;
            bool sprinting = controller != null && controller.IsSprinting;
            if (!sprinting && Time.time - lastStaminaUse > staminaRegenDelay)
                Stamina = Mathf.Min(maxStamina, Stamina + staminaRegen * Time.deltaTime);
        }

        /// <summary>Souls rule: any action is allowed while stamina is above zero.</summary>
        public bool TryUseStamina(float amount)
        {
            if (Stamina <= 0.01f) return false;
            DrainStamina(amount);
            return true;
        }

        public void DrainStamina(float amount)
        {
            Stamina = Mathf.Max(0f, Stamina - amount);
            lastStaminaUse = Time.time;
        }

        public bool TakeDamage(float amount, Vector3 sourcePosition)
        {
            if (IsDead) return false;
            if (controller != null && controller.IsInvulnerable) return false;

            Health -= amount;
            OnDamaged?.Invoke();
            if (controller != null) controller.ApplyHitStagger(sourcePosition);

            if (Health <= 0f)
            {
                Health = 0f;
                IsDead = true;
                if (GameManager.Instance != null) GameManager.Instance.OnPlayerDied();
            }
            return true;
        }

        public bool UseFlask()
        {
            if (Flasks <= 0 || Health >= maxHealth) return false;
            Flasks--;
            return true;
        }

        public void HealPercent(float percent)
        {
            if (IsDead) return;
            Health = Mathf.Min(maxHealth, Health + maxHealth * percent);
        }

        public void AddEmbers(int amount) => Embers += amount;

        public void RestoreAll()
        {
            Health = maxHealth;
            Stamina = maxStamina;
            Flasks = maxFlasks;
            IsDead = false;
        }
    }
}
