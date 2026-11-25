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
        public event Action OnBlocked;
        public event Action OnGuardBroken;

        [Header("Shield")]
        [Range(0f, 1f)] public float blockDamageTaken = 0.1f;
        public float blockStaminaPerDamage = 1.1f;
        public float blockAngle = 70f;
        [Tooltip("Raise the shield this close to a melee hit to parry it: no damage, the attacker staggers.")]
        public float parryWindow = 0.18f;
        public event Action OnParry;

        float lastStaminaUse = -10f;
        PlayerController controller;

        public float DamageMultiplier { get; private set; } = 1f;

        const float BaseHealth = 100f, BaseStamina = 100f;

        void Awake()
        {
            controller = GetComponent<PlayerController>();
            ApplyLevels();
            RestoreAll();
        }

        /// <summary>Recalculate max health/stamina/damage from the levels bought at the flames.</summary>
        public void ApplyLevels()
        {
            var d = SaveSystem.Data;
            maxHealth = BaseHealth + d.vigor * 8f;
            maxStamina = BaseStamina + d.endurance * 5f;
            DamageMultiplier = 1f + d.strength * 0.045f;
            maxFlasks = 4 + (d.sacredOils != null ? d.sacredOils.Count : 0);
        }

        public static int LevelCost(int level) => Mathf.RoundToInt(260f * Mathf.Pow(1.13f, level - 1));

        void Update()
        {
            if (IsDead) return;
            bool sprinting = controller != null && controller.IsSprinting;
            bool blocking = controller != null && controller.IsBlocking;
            if (!sprinting && Time.time - lastStaminaUse > staminaRegenDelay)
                Stamina = Mathf.Min(maxStamina, Stamina + staminaRegen * (blocking ? 0.4f : 1f) * Time.deltaTime);
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

            // shield block: attacks from the front cost stamina instead of health
            if (controller != null && controller.IsBlocking)
            {
                Vector3 toSource = sourcePosition - transform.position;
                toSource.y = 0f;
                if (Vector3.Angle(transform.forward, toSource) <= blockAngle)
                {
                    // parry: shield raised at the very last moment against a melee attack
                    var attacker = Time.time - controller.BlockStartTime <= parryWindow ? EnemyAI.AttackerAt(sourcePosition) : null;
                    if (attacker != null)
                    {
                        attacker.Parried();
                        OnParry?.Invoke();
                        if (AudioManager.Instance != null) AudioManager.At(AudioManager.Instance.block, transform.position, 1f, 0.3f);
                        HitFX.Hit(transform.position + transform.forward * 0.6f + Vector3.up * 0.4f, true, new Color(1f, 0.95f, 0.7f));
                        HitFX.Ring(transform.position + Vector3.up * 0.1f, 1.6f, new Color(1f, 0.9f, 0.6f));
                        if (ThirdPersonCamera.Instance != null) ThirdPersonCamera.Instance.Shake(0.15f);
                        if (HUD.Instance != null) HUD.Instance.ShowMessage(ArabicText.Fix("صد متقن"), 0.9f);
                        return false;
                    }

                    float cost = amount * blockStaminaPerDamage;
                    if (Stamina >= cost)
                    {
                        DrainStamina(cost);
                        amount *= blockDamageTaken;
                        OnBlocked?.Invoke();
                        if (AudioManager.Instance != null) AudioManager.At(AudioManager.Instance.block, transform.position, 1f);
                        HitFX.Hit(transform.position + transform.forward * 0.5f + Vector3.up * 0.3f, false, new Color(1f, 0.85f, 0.45f));
                        if (ThirdPersonCamera.Instance != null) ThirdPersonCamera.Instance.Shake(0.08f);
                        controller.ApplyHitStagger(sourcePosition, 0.12f, 1.5f);
                        Health -= amount;
                        if (Health <= 0f) { Health = 0f; IsDead = true; if (GameManager.Instance != null) GameManager.Instance.OnPlayerDied(); }
                        return true;
                    }
                    // guard break
                    DrainStamina(Stamina);
                    amount *= 0.5f;
                    OnGuardBroken?.Invoke();
                    if (HUD.Instance != null) HUD.Instance.ShowMessage(ArabicText.Fix("انكسر صدك"), 1.2f);
                    Health -= amount;
                    OnDamaged?.Invoke();
                    controller.ApplyHitStagger(sourcePosition, 0.9f, 2f);
                    if (Health <= 0f) { Health = 0f; IsDead = true; if (GameManager.Instance != null) GameManager.Instance.OnPlayerDied(); }
                    return true;
                }
            }

            Health -= amount;
            OnDamaged?.Invoke();
            if (AudioManager.Instance != null) AudioManager.At(AudioManager.Instance.hit, transform.position, 0.9f);
            HitFX.Hit(transform.position + Vector3.up * 0.3f, amount >= 30f, new Color(0.7f, 0.12f, 0.08f));
            if (ThirdPersonCamera.Instance != null) ThirdPersonCamera.Instance.Shake(0.15f);
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
