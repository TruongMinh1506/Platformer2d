using UnityEngine;

namespace Platformer.Managers
{
    /// <summary>
    /// Static manager handling persistent progress using PlayerPrefs:
    /// - High score
    /// - Unlocked level progression (Level 1..3)
    /// - 3-Star rating per level (Completion, Coin target, Speedrun target)
    /// - Best recorded completion times and coin counts per level
    /// </summary>
    public static class ProgressManager
    {
        private const string KeyBestScore = "Platformer_BestScore";
        private const string KeyUnlockedLevel = "Platformer_UnlockedLevel";
        private const string KeyLevelStarsPrefix = "Platformer_Stars_Level_";
        private const string KeyLevelTimePrefix = "Platformer_Time_Level_";
        private const string KeyLevelCoinsPrefix = "Platformer_Coins_Level_";

        // Upgrade & Economy Keys
        private const string KeyBankedCoins = "Platformer_BankedCoins";
        private const string KeyUpgradeHealth = "Platformer_Upgrade_Health";
        private const string KeyUpgradeDash = "Platformer_Upgrade_Dash";
        private const string KeyUpgradeMagnet = "Platformer_Upgrade_Magnet";
        private const string KeyUpgradeBlade = "Platformer_Upgrade_Blade";
        private const string KeyInitializedEconomy = "Platformer_InitEconomy";

        /// <summary>
        /// Retrieves the player's total persistent banked coins for the Upgrade Shop.
        /// Starts with a 15-coin starter gift on first run.
        /// </summary>
        public static int GetBankedCoins()
        {
            if (PlayerPrefs.GetInt(KeyInitializedEconomy, 0) == 0)
            {
                PlayerPrefs.SetInt(KeyInitializedEconomy, 1);
                PlayerPrefs.SetInt(KeyBankedCoins, 15); // Starter bonus coins!
                PlayerPrefs.Save();
            }
            return PlayerPrefs.GetInt(KeyBankedCoins, 0);
        }

        public static void AddBankedCoins(int amount)
        {
            if (amount <= 0) return;
            int current = GetBankedCoins();
            PlayerPrefs.SetInt(KeyBankedCoins, current + amount);
            PlayerPrefs.Save();
        }

        public static bool SpendBankedCoins(int amount)
        {
            if (amount <= 0) return true;
            int current = GetBankedCoins();
            if (current >= amount)
            {
                PlayerPrefs.SetInt(KeyBankedCoins, current - amount);
                PlayerPrefs.Save();
                return true;
            }
            return false;
        }

        public static int GetHealthUpgradeLevel() => PlayerPrefs.GetInt(KeyUpgradeHealth, 0);
        public static void SetHealthUpgradeLevel(int level)
        {
            PlayerPrefs.SetInt(KeyUpgradeHealth, Mathf.Clamp(level, 0, 2));
            PlayerPrefs.Save();
        }

        public static int GetDashUpgradeLevel() => PlayerPrefs.GetInt(KeyUpgradeDash, 0);
        public static void SetDashUpgradeLevel(int level)
        {
            PlayerPrefs.SetInt(KeyUpgradeDash, Mathf.Clamp(level, 0, 2));
            PlayerPrefs.Save();
        }

        public static int GetMagnetUpgradeLevel() => PlayerPrefs.GetInt(KeyUpgradeMagnet, 0);
        public static void SetMagnetUpgradeLevel(int level)
        {
            PlayerPrefs.SetInt(KeyUpgradeMagnet, Mathf.Clamp(level, 0, 2));
            PlayerPrefs.Save();
        }

        public static int GetBladeUpgradeLevel() => PlayerPrefs.GetInt(KeyUpgradeBlade, 0);
        public static void SetBladeUpgradeLevel(int level)
        {
            PlayerPrefs.SetInt(KeyUpgradeBlade, Mathf.Clamp(level, 0, 1));
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Retrieves the highest recorded overall score.
        /// </summary>
        public static int GetBestScore()
        {
            return PlayerPrefs.GetInt(KeyBestScore, 0);
        }

        /// <summary>
        /// Saves the given score if it surpasses the existing high score.
        /// </summary>
        public static void SaveScore(int currentScore)
        {
            int best = GetBestScore();
            if (currentScore > best)
            {
                PlayerPrefs.SetInt(KeyBestScore, currentScore);
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// Retrieves the index of the highest unlocked level (1 = Level 1).
        /// </summary>
        public static int GetUnlockedLevelIndex()
        {
            return PlayerPrefs.GetInt(KeyUnlockedLevel, 1);
        }

        /// <summary>
        /// Unlocks the specified level index if greater than current progression.
        /// </summary>
        public static void UnlockLevel(int levelNumber)
        {
            int currentUnlocked = GetUnlockedLevelIndex();
            if (levelNumber > currentUnlocked)
            {
                PlayerPrefs.SetInt(KeyUnlockedLevel, levelNumber);
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// Retrieves the number of stars earned for a specific level (0..3).
        /// </summary>
        public static int GetLevelStars(int levelNumber)
        {
            return PlayerPrefs.GetInt(KeyLevelStarsPrefix + levelNumber, 0);
        }

        /// <summary>
        /// Saves earned stars if greater than previous record.
        /// </summary>
        public static void SaveLevelStars(int levelNumber, int stars)
        {
            int current = GetLevelStars(levelNumber);
            if (stars > current)
            {
                PlayerPrefs.SetInt(KeyLevelStarsPrefix + levelNumber, Mathf.Clamp(stars, 0, 3));
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// Retrieves the fastest completion time for a specific level in seconds (0 = uncompleted).
        /// </summary>
        public static float GetLevelBestTime(int levelNumber)
        {
            return PlayerPrefs.GetFloat(KeyLevelTimePrefix + levelNumber, 0f);
        }

        /// <summary>
        /// Saves completion time if it beats the previous best time.
        /// </summary>
        public static void SaveLevelBestTime(int levelNumber, float time)
        {
            if (time <= 0f) return;
            float currentBest = GetLevelBestTime(levelNumber);
            if (currentBest <= 0f || time < currentBest)
            {
                PlayerPrefs.SetFloat(KeyLevelTimePrefix + levelNumber, time);
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// Retrieves the maximum coins collected in a single run for a specific level.
        /// </summary>
        public static int GetLevelBestCoins(int levelNumber)
        {
            return PlayerPrefs.GetInt(KeyLevelCoinsPrefix + levelNumber, 0);
        }

        /// <summary>
        /// Saves coin count if higher than previous record.
        /// </summary>
        public static void SaveLevelBestCoins(int levelNumber, int coins)
        {
            int current = GetLevelBestCoins(levelNumber);
            if (coins > current)
            {
                PlayerPrefs.SetInt(KeyLevelCoinsPrefix + levelNumber, coins);
                PlayerPrefs.Save();
            }
        }

        /// <summary>
        /// Computes the sum of stars achieved across all 3 levels (max 9).
        /// </summary>
        public static int GetTotalStars()
        {
            int total = 0;
            for (int i = 1; i <= 3; i++)
            {
                total += GetLevelStars(i);
            }
            return total;
        }

        /// <summary>
        /// Returns the star target requirements for a given level:
        /// Star 1: Complete the level.
        /// Star 2: Collect at least targetCoins.
        /// Star 3: Finish within targetTime seconds.
        /// </summary>
        public static void GetStarRequirements(int levelNumber, out int targetCoins, out float targetTime)
        {
            switch (levelNumber)
            {
                case 1:
                    targetCoins = 10;
                    targetTime = 45f;
                    break;
                case 2:
                    targetCoins = 12;
                    targetTime = 65f;
                    break;
                case 3:
                default:
                    targetCoins = 14;
                    targetTime = 90f;
                    break;
            }
        }

        /// <summary>
        /// Evaluates star criteria and returns the total stars earned (1..3).
        /// </summary>
        public static int CalculateStars(int levelNumber, int coins, float time, out bool starClear, out bool starCoin, out bool starTime)
        {
            GetStarRequirements(levelNumber, out int targetCoins, out float targetTime);

            starClear = true; // Reaching the goal clears the level
            starCoin = coins >= targetCoins;
            starTime = time <= targetTime;

            int stars = 1;
            if (starCoin) stars++;
            if (starTime) stars++;

            return stars;
        }

        /// <summary>
        /// Returns a formatted TMP string with colored stars (e.g. 2 stars -> 2 gold, 1 grey).
        /// </summary>
        public static string FormatStarsString(int stars, string goldColor = "#FFD700", string dimColor = "#555555")
        {
            int s = Mathf.Clamp(stars, 0, 3);
            string result = "";
            for (int i = 1; i <= 3; i++)
            {
                if (i <= s)
                    result += $"<size=120%><color={goldColor}><b>*</b></color></size> ";
                else
                    result += $"<size=120%><color={dimColor}><b>*</b></color></size> ";
            }
            return result.TrimEnd();
        }

        /// <summary>
        /// Clears all saved platformer progression.
        /// </summary>
        public static void ResetProgress(bool resetUpgrades = false)
        {
            PlayerPrefs.DeleteKey(KeyBestScore);
            PlayerPrefs.DeleteKey(KeyUnlockedLevel);

            for (int i = 1; i <= 3; i++)
            {
                PlayerPrefs.DeleteKey(KeyLevelStarsPrefix + i);
                PlayerPrefs.DeleteKey(KeyLevelTimePrefix + i);
                PlayerPrefs.DeleteKey(KeyLevelCoinsPrefix + i);
            }

            if (resetUpgrades)
            {
                PlayerPrefs.DeleteKey(KeyBankedCoins);
                PlayerPrefs.DeleteKey(KeyUpgradeHealth);
                PlayerPrefs.DeleteKey(KeyUpgradeDash);
                PlayerPrefs.DeleteKey(KeyUpgradeMagnet);
                PlayerPrefs.DeleteKey(KeyUpgradeBlade);
                PlayerPrefs.DeleteKey(KeyInitializedEconomy);
            }

            PlayerPrefs.Save();
        }
    }
}
