using UnityEngine;
using Platformer.Common;
using Platformer.Managers;

namespace Platformer.Player
{
    /// <summary>
    /// Reads persistent skill upgrades from ProgressManager and applies them to the Player:
    /// - Max Health Expansion (+1 or +2 hearts)
    /// - Dash Cooldown Booster (0.5s -> 0.38s -> 0.25s)
    /// - Coin Magnet (0m -> 3.5m -> 6.0m radius)
    /// - Blade Mastery (Lower mana cost & increased slash power)
    /// </summary>
    [RequireComponent(typeof(Health), typeof(PlayerController), typeof(PlayerCombat))]
    public class PlayerPerksApplier : MonoBehaviour
    {
        private Health playerHealth;
        private PlayerController playerController;
        private PlayerCombat playerCombat;
        private PlayerCoinMagnet coinMagnet;

        private void Awake()
        {
            playerHealth = GetComponent<Health>();
            playerController = GetComponent<PlayerController>();
            playerCombat = GetComponent<PlayerCombat>();
            coinMagnet = GetComponent<PlayerCoinMagnet>();
            if (coinMagnet == null) coinMagnet = gameObject.AddComponent<PlayerCoinMagnet>();
        }

        private void Start()
        {
            ApplyAllPerks();
        }

        public void ApplyAllPerks()
        {
            // 1. Health Upgrade
            int hpLevel = ProgressManager.GetHealthUpgradeLevel();
            if (playerHealth != null)
            {
                int targetMax = 3 + hpLevel;
                if (playerHealth.MaxHealth != targetMax)
                {
                    playerHealth.SetMaxHealth(targetMax, true);
                }
            }

            // 2. Dash Cooldown Upgrade
            int dashLevel = ProgressManager.GetDashUpgradeLevel();
            if (playerController != null)
            {
                float cd = dashLevel == 2 ? 0.25f : (dashLevel == 1 ? 0.38f : 0.50f);
                playerController.SetDashCooldown(cd);
            }

            // 3. Coin Magnet Upgrade
            int magnetLevel = ProgressManager.GetMagnetUpgradeLevel();
            if (coinMagnet != null)
            {
                float radius = magnetLevel == 2 ? 6.0f : (magnetLevel == 1 ? 3.5f : 0f);
                coinMagnet.SetRadius(radius);
            }

            // 4. Blade Mastery
            int bladeLevel = ProgressManager.GetBladeUpgradeLevel();
            if (playerCombat != null)
            {
                // If blade upgraded, reduce mana cost
                float cost = bladeLevel >= 1 ? 20f : 30f;
                playerCombat.SetEnergySlashCost(cost);
            }
        }
    }
}
