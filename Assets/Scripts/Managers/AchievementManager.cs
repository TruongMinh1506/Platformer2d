using System;
using System.Collections.Generic;
using UnityEngine;

namespace Platformer.Managers
{
    [Serializable]
    public class AchievementData
    {
        public string id;
        public string title;
        public string description;
        public string icon;
        public bool isUnlocked;
    }

    /// <summary>
    /// Persistent achievement tracking system across game sessions.
    /// Broadcasts unlock events to display rich in-game trophy banners.
    /// </summary>
    public class AchievementManager : MonoBehaviour
    {
        private static AchievementManager instance;

        public static AchievementManager Instance
        {
            get
            {
                if (instance == null)
                {
                    var existing = UnityEngine.Object.FindFirstObjectByType<AchievementManager>();
                    if (existing != null)
                    {
                        instance = existing;
                    }
                    else
                    {
                        GameObject go = new GameObject("[AchievementManager]");
                        instance = go.AddComponent<AchievementManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }

        public event Action<AchievementData> OnAchievementUnlocked;

        private readonly Dictionary<string, AchievementData> achievements = new Dictionary<string, AchievementData>()
        {
            {
                "ACH_PARRY_MASTER",
                new AchievementData { id = "ACH_PARRY_MASTER", title = "Parry Master", description = "Deflect incoming projectiles or execute a melee counter!", icon = "shield" }
            },
            {
                "ACH_UNTOUCHABLE",
                new AchievementData { id = "ACH_UNTOUCHABLE", title = "Untouchable", description = "Clear a level without sustaining any damage!", icon = "feather" }
            },
            {
                "ACH_TITAN_SLAYER",
                new AchievementData { id = "ACH_TITAN_SLAYER", title = "Titan Slayer", description = "Vanquish Ignis, the Volcanic Titan of the Sky Fortress!", icon = "skull" }
            },
            {
                "ACH_ARCHAEOLOGIST",
                new AchievementData { id = "ACH_ARCHAEOLOGIST", title = "Archaeologist", description = "Discover an ancient relic behind a secret wall!", icon = "gem" }
            },
            {
                "ACH_COMBO_KING",
                new AchievementData { id = "ACH_COMBO_KING", title = "Combo King", description = "Achieve the prestigious SSS Sensational Style Rank!", icon = "crown" }
            },
            {
                "ACH_STALACTITE_CRUSH",
                new AchievementData { id = "ACH_STALACTITE_CRUSH", title = "Watch Your Head!", description = "Crush an enemy using a falling environmental stalactite!", icon = "rock" }
            },
            {
                "ACH_BLADE_TEMPEST",
                new AchievementData { id = "ACH_BLADE_TEMPEST", title = "Tempest Unleashed", description = "Execute the devastating Blade Tempest screen-clearing ultimate!", icon = "sword" }
            },
            {
                "ACH_ELITE_SLAYER",
                new AchievementData { id = "ACH_ELITE_SLAYER", title = "Elite Slayer", description = "Slay a powerful Elite Champion enemy in combat!", icon = "target" }
            }
        };

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            DontDestroyOnLoad(gameObject);

            LoadAchievements();
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        private void LoadAchievements()
        {
            foreach (var kvp in achievements)
            {
                kvp.Value.isUnlocked = PlayerPrefs.GetInt(kvp.Key, 0) == 1 || PlayerPrefs.GetInt("ACH_" + kvp.Key, 0) == 1;
            }
        }

        public bool IsUnlocked(string id)
        {
            return achievements.TryGetValue(id, out var ach) && ach.isUnlocked;
        }

        public void Unlock(string id)
        {
            if (achievements.TryGetValue(id, out var ach))
            {
                if (!ach.isUnlocked)
                {
                    ach.isUnlocked = true;
                    PlayerPrefs.SetInt(id, 1);
                    PlayerPrefs.Save();

                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.PlaySound("achievement");
                    }

                    OnAchievementUnlocked?.Invoke(ach);
                }
            }
        }

        public IEnumerable<AchievementData> GetAllAchievements()
        {
            return achievements.Values;
        }
    }
}
