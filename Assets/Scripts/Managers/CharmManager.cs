using System;
using System.Collections.Generic;
using UnityEngine;

namespace Platformer.Managers
{
    public enum CharmType
    {
        VampiricFang,
        ThunderStep,
        MagnetRing,
        BerserkerCrest
    }

    [System.Serializable]
    public struct CharmInfo
    {
        public CharmType type;
        public string name;
        public string subtitle;
        public string description;

        public CharmInfo(CharmType type, string name, string subtitle, string description)
        {
            this.type = type;
            this.name = name;
            this.subtitle = subtitle;
            this.description = description;
        }
    }

    /// <summary>
    /// Manages the 3-slot Relic & Charm Socketing System (Hollow Knight style).
    /// Provides passive combat and exploration perks, persisted through PlayerPrefs.
    /// </summary>
    public class CharmManager : MonoBehaviour
    {
        private static CharmManager instance;
        public static CharmManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<CharmManager>();
                    if (instance == null)
                    {
                        var go = new GameObject("[CharmManager]");
                        instance = go.AddComponent<CharmManager>();
                        if (Application.isPlaying)
                        {
                            DontDestroyOnLoad(go);
                        }
                    }
                }
                return instance;
            }
        }

        public const int MaxSlots = 3;

        private readonly HashSet<CharmType> equippedCharms = new HashSet<CharmType>();

        public int EquippedCount => equippedCharms.Count;
        public event Action OnCharmsChanged;

        private readonly List<CharmInfo> allCharms = new List<CharmInfo>
        {
            new CharmInfo(
                CharmType.VampiricFang,
                "Nanh Ma Cà Rồng (Vampiric Fang)",
                "Hồi máu khi hạ địch",
                "Hồi phục +1 HP cho nhân vật mỗi khi tiêu diệt được bất kỳ quái vật nào."
            ),
            new CharmInfo(
                CharmType.ThunderStep,
                "Lôi Ảnh Bộ (Thunder Step)",
                "Sét giật khi lướt (Shadow Dash)",
                "Phóng luồng điện giật gây 25 sát thương diện rộng khi thực hiện Shadow Dash (Shift)."
            ),
            new CharmInfo(
                CharmType.MagnetRing,
                "Nhẫn Hút Vàng (Magnet Ring)",
                "Gấp 3 bán kính hút tiền",
                "Gia tăng gấp 3 lần bán kính tự động hút tiền xu vàng rơi ra từ quái và rương."
            ),
            new CharmInfo(
                CharmType.BerserkerCrest,
                "Huy Hiệu Cuồng Nộ (Berserker Crest)",
                "+50% sát thương khi nguy kịch",
                "Tăng thêm 50% sát thương đòn đánh kiếm cận chiến khi máu của bạn dưới 35%."
            )
        };

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
            }

            LoadCharms();
        }

        public IReadOnlyList<CharmInfo> GetAllCharms()
        {
            return allCharms;
        }

        public bool IsEquipped(CharmType charm)
        {
            return equippedCharms.Contains(charm);
        }

        public bool ToggleCharm(CharmType charm)
        {
            if (equippedCharms.Contains(charm))
            {
                equippedCharms.Remove(charm);
                PlayerPrefs.SetInt("Charm_" + charm.ToString(), 0);
                PlayerPrefs.Save();
                AudioManager.Instance?.PlaySound("charm_unequip");
                OnCharmsChanged?.Invoke();
                return false;
            }
            else
            {
                if (equippedCharms.Count >= MaxSlots)
                {
                    AudioManager.Instance?.PlaySound("ui_deny", 0.6f);
                    return false;
                }

                equippedCharms.Add(charm);
                PlayerPrefs.SetInt("Charm_" + charm.ToString(), 1);
                PlayerPrefs.Save();
                AudioManager.Instance?.PlaySound("charm_equip");
                OnCharmsChanged?.Invoke();
                return true;
            }
        }

        private void LoadCharms()
        {
            equippedCharms.Clear();
            foreach (var charm in allCharms)
            {
                if (PlayerPrefs.GetInt("Charm_" + charm.type.ToString(), 0) == 1)
                {
                    if (equippedCharms.Count < MaxSlots)
                    {
                        equippedCharms.Add(charm.type);
                    }
                }
            }
        }
    }
}
