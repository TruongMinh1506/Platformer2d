using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Platformer.Managers;
using Platformer.Player;

namespace Platformer.UI
{
    /// <summary>
    /// UI controller for the Upgrade / Skill Shop panel.
    /// Allows the player to spend persistently banked coins on:
    /// 1. Max Health (Extra Hearts)
    /// 2. Dash Cooldown Booster
    /// 3. Coin Magnet (Automatic Coin Vacuum)
    /// 4. Blade Mastery (Reduced Mana Cost & Enhanced Damage)
    /// Accessible from both Main Menu and In-Game Pause Menu.
    /// </summary>
    public class UpgradeShopUI : MonoBehaviour
    {
        [Header("Root Panel")]
        [SerializeField] private GameObject shopPanel;
        [SerializeField] private TextMeshProUGUI coinsDisplayText;
        [SerializeField] private Button closeButton;

        [Header("Perk 1: Health Upgrade")]
        [SerializeField] private TextMeshProUGUI healthLevelText;
        [SerializeField] private TextMeshProUGUI healthCostText;
        [SerializeField] private Button healthBuyButton;

        [Header("Perk 2: Dash Booster")]
        [SerializeField] private TextMeshProUGUI dashLevelText;
        [SerializeField] private TextMeshProUGUI dashCostText;
        [SerializeField] private Button dashBuyButton;

        [Header("Perk 3: Coin Magnet")]
        [SerializeField] private TextMeshProUGUI magnetLevelText;
        [SerializeField] private TextMeshProUGUI magnetCostText;
        [SerializeField] private Button magnetBuyButton;

        [Header("Perk 4: Blade Mastery")]
        [SerializeField] private TextMeshProUGUI bladeLevelText;
        [SerializeField] private TextMeshProUGUI bladeCostText;
        [SerializeField] private Button bladeBuyButton;

        // Upgrade Cost Tables
        private readonly int[] healthCosts = new int[] { 15, 30 };
        private readonly int[] dashCosts = new int[] { 15, 30 };
        private readonly int[] magnetCosts = new int[] { 20, 35 };
        private readonly int bladeCost = 25;

        private void Start()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(CloseShop);
            }

            if (healthBuyButton != null)
            {
                healthBuyButton.onClick.AddListener(BuyHealthUpgrade);
            }

            if (dashBuyButton != null)
            {
                dashBuyButton.onClick.AddListener(BuyDashUpgrade);
            }

            if (magnetBuyButton != null)
            {
                magnetBuyButton.onClick.AddListener(BuyMagnetUpgrade);
            }

            if (bladeBuyButton != null)
            {
                bladeBuyButton.onClick.AddListener(BuyBladeUpgrade);
            }

            RefreshUI();
        }

        private void Update()
        {
            if (shopPanel != null && shopPanel.activeSelf)
            {
                bool escape = false;
#if ENABLE_INPUT_SYSTEM
                var kb = UnityEngine.InputSystem.Keyboard.current;
                if (kb != null && kb.escapeKey.wasPressedThisFrame) escape = true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
                if (Input.GetKeyDown(KeyCode.Escape)) escape = true;
#endif
                if (escape)
                {
                    CloseShop();
                }
            }
        }

        private void OnDestroy()
        {
            if (closeButton != null) closeButton.onClick.RemoveListener(CloseShop);
            if (healthBuyButton != null) healthBuyButton.onClick.RemoveListener(BuyHealthUpgrade);
            if (dashBuyButton != null) dashBuyButton.onClick.RemoveListener(BuyDashUpgrade);
            if (magnetBuyButton != null) magnetBuyButton.onClick.RemoveListener(BuyMagnetUpgrade);
            if (bladeBuyButton != null) bladeBuyButton.onClick.RemoveListener(BuyBladeUpgrade);
        }

        public void OpenShop()
        {
            if (shopPanel != null)
            {
                shopPanel.SetActive(true);
            }
            RefreshUI();
        }

        public void CloseShop()
        {
            if (shopPanel != null)
            {
                shopPanel.SetActive(false);
            }

            // Immediately apply newly purchased perks to active player in scene if present
            var playerApplier = UnityEngine.Object.FindFirstObjectByType<PlayerPerksApplier>();
            if (playerApplier != null)
            {
                playerApplier.ApplyAllPerks();
            }
        }

        public void RefreshUI()
        {
            int bankedCoins = ProgressManager.GetBankedCoins();
            if (coinsDisplayText != null)
            {
                coinsDisplayText.text = $"COINS: {bankedCoins}";
            }

            // 1. Health
            int hpLevel = ProgressManager.GetHealthUpgradeLevel();
            if (hpLevel >= 2)
            {
                if (healthLevelText != null) healthLevelText.text = "LEVEL: MAX (+2 HP)";
                if (healthCostText != null) healthCostText.text = "PURCHASED";
                if (healthBuyButton != null) healthBuyButton.interactable = false;
            }
            else
            {
                int cost = healthCosts[hpLevel];
                if (healthLevelText != null) healthLevelText.text = $"LEVEL: {hpLevel} / 2 (+{hpLevel} HP)";
                if (healthCostText != null) healthCostText.text = $"{cost} COINS";
                if (healthBuyButton != null) healthBuyButton.interactable = bankedCoins >= cost;
            }

            // 2. Dash
            int dashLevel = ProgressManager.GetDashUpgradeLevel();
            if (dashLevel >= 2)
            {
                if (dashLevelText != null) dashLevelText.text = "LEVEL: MAX (0.25s CD)";
                if (dashCostText != null) dashCostText.text = "PURCHASED";
                if (dashBuyButton != null) dashBuyButton.interactable = false;
            }
            else
            {
                int cost = dashCosts[dashLevel];
                float cd = dashLevel == 1 ? 0.38f : 0.50f;
                if (dashLevelText != null) dashLevelText.text = $"LEVEL: {dashLevel} / 2 ({cd}s CD)";
                if (dashCostText != null) dashCostText.text = $"{cost} COINS";
                if (dashBuyButton != null) dashBuyButton.interactable = bankedCoins >= cost;
            }

            // 3. Magnet
            int magnetLevel = ProgressManager.GetMagnetUpgradeLevel();
            if (magnetLevel >= 2)
            {
                if (magnetLevelText != null) magnetLevelText.text = "LEVEL: MAX (6.0m Radius)";
                if (magnetCostText != null) magnetCostText.text = "PURCHASED";
                if (magnetBuyButton != null) magnetBuyButton.interactable = false;
            }
            else
            {
                int cost = magnetCosts[magnetLevel];
                float rad = magnetLevel == 1 ? 3.5f : 0f;
                if (magnetLevelText != null) magnetLevelText.text = $"LEVEL: {magnetLevel} / 2 ({rad}m Radius)";
                if (magnetCostText != null) magnetCostText.text = $"{cost} COINS";
                if (magnetBuyButton != null) magnetBuyButton.interactable = bankedCoins >= cost;
            }

            // 4. Blade Mastery
            int bladeLevel = ProgressManager.GetBladeUpgradeLevel();
            if (bladeLevel >= 1)
            {
                if (bladeLevelText != null) bladeLevelText.text = "LEVEL: MAX (Enhanced Wave)";
                if (bladeCostText != null) bladeCostText.text = "PURCHASED";
                if (bladeBuyButton != null) bladeBuyButton.interactable = false;
            }
            else
            {
                if (bladeLevelText != null) bladeLevelText.text = "LEVEL: 0 / 1 (30 Mana Cost)";
                if (bladeCostText != null) bladeCostText.text = $"{bladeCost} COINS";
                if (bladeBuyButton != null) bladeBuyButton.interactable = bankedCoins >= bladeCost;
            }
        }

        private void BuyHealthUpgrade()
        {
            int lvl = ProgressManager.GetHealthUpgradeLevel();
            if (lvl < 2)
            {
                int cost = healthCosts[lvl];
                if (ProgressManager.SpendBankedCoins(cost))
                {
                    ProgressManager.SetHealthUpgradeLevel(lvl + 1);
                    RefreshUI();
                }
            }
        }

        private void BuyDashUpgrade()
        {
            int lvl = ProgressManager.GetDashUpgradeLevel();
            if (lvl < 2)
            {
                int cost = dashCosts[lvl];
                if (ProgressManager.SpendBankedCoins(cost))
                {
                    ProgressManager.SetDashUpgradeLevel(lvl + 1);
                    RefreshUI();
                }
            }
        }

        private void BuyMagnetUpgrade()
        {
            int lvl = ProgressManager.GetMagnetUpgradeLevel();
            if (lvl < 2)
            {
                int cost = magnetCosts[lvl];
                if (ProgressManager.SpendBankedCoins(cost))
                {
                    ProgressManager.SetMagnetUpgradeLevel(lvl + 1);
                    RefreshUI();
                }
            }
        }

        private void BuyBladeUpgrade()
        {
            int lvl = ProgressManager.GetBladeUpgradeLevel();
            if (lvl < 1)
            {
                if (ProgressManager.SpendBankedCoins(bladeCost))
                {
                    ProgressManager.SetBladeUpgradeLevel(lvl + 1);
                    RefreshUI();
                }
            }
        }

        public void SetReferences(
            GameObject panel, TextMeshProUGUI coinsText, Button closeBtn,
            TextMeshProUGUI hpLvl, TextMeshProUGUI hpCost, Button hpBtn,
            TextMeshProUGUI dashLvl, TextMeshProUGUI dashCost, Button dashBtn,
            TextMeshProUGUI magLvl, TextMeshProUGUI magCost, Button magBtn,
            TextMeshProUGUI bladeLvl, TextMeshProUGUI bladeCost, Button bladeBtn)
        {
            shopPanel = panel;
            coinsDisplayText = coinsText;
            closeButton = closeBtn;
            healthLevelText = hpLvl;
            healthCostText = hpCost;
            healthBuyButton = hpBtn;
            dashLevelText = dashLvl;
            dashCostText = dashCost;
            dashBuyButton = dashBtn;
            magnetLevelText = magLvl;
            magnetCostText = magCost;
            magnetBuyButton = magBtn;
            bladeLevelText = bladeLvl;
            bladeCostText = bladeCost;
            bladeBuyButton = bladeBtn;
        }
    }
}
