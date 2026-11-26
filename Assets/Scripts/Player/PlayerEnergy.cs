using System;
using UnityEngine;

namespace Platformer.Player
{
    /// <summary>
    /// Manages player energy / mana pool used for special ranged skills (Energy Blade Slash).
    /// Energy replenishes via melee sword strikes (+20), enemy stomps (+25),
    /// dash piercing (+20), and passive regeneration.
    /// </summary>
    public class PlayerEnergy : MonoBehaviour
    {
        [Header("Energy Stats")]
        [Tooltip("Maximum energy capacity.")]
        [SerializeField] private float maxEnergy = 100f;

        [Tooltip("Initial energy upon spawn.")]
        [SerializeField] private float initialEnergy = 100f;

        [Tooltip("Passive energy recovered per second.")]
        [SerializeField] private float passiveRegenRate = 4.0f;

        public float MaxEnergy => maxEnergy;
        public float CurrentEnergy { get; private set; }

        public event Action<float, float> OnEnergyChanged;

        private void Awake()
        {
            CurrentEnergy = Mathf.Clamp(initialEnergy, 0f, maxEnergy);
        }

        private void Start()
        {
            OnEnergyChanged?.Invoke(CurrentEnergy, maxEnergy);
        }

        private void Update()
        {
            if (CurrentEnergy < maxEnergy)
            {
                AddEnergy(passiveRegenRate * Time.deltaTime);
            }
        }

        public bool TryConsumeEnergy(float amount)
        {
            if (CurrentEnergy >= amount)
            {
                CurrentEnergy -= amount;
                OnEnergyChanged?.Invoke(CurrentEnergy, maxEnergy);
                return true;
            }
            return false;
        }

        public void AddEnergy(float amount)
        {
            if (amount <= 0f) return;
            float previous = CurrentEnergy;
            CurrentEnergy = Mathf.Clamp(CurrentEnergy + amount, 0f, maxEnergy);
            if (!Mathf.Approximately(previous, CurrentEnergy))
            {
                OnEnergyChanged?.Invoke(CurrentEnergy, maxEnergy);
            }
        }

        public void SetMaxEnergy(float newMax, bool fillToMax = false)
        {
            maxEnergy = newMax;
            if (fillToMax) CurrentEnergy = maxEnergy;
            else CurrentEnergy = Mathf.Min(CurrentEnergy, maxEnergy);
            OnEnergyChanged?.Invoke(CurrentEnergy, maxEnergy);
        }
    }
}
