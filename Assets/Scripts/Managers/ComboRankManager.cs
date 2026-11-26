using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Platformer.Common;
using Platformer.Enemies;
using Platformer.Player;

namespace Platformer.Managers
{
    public enum ComboRank
    {
        None = 0,
        D = 1,
        C = 2,
        B = 3,
        A = 4,
        S = 5,
        SSS = 6
    }

    /// <summary>
    /// Manages real-time combat Style Meter & Overdrive:
    /// - Tracks combo streak (D -> C -> B -> A -> S -> SSS) with time decay.
    /// - Rewards flawless aggression and parrying with bonus score and Overdrive gauge.
    /// - Unleashes the screen-clearing "Blade Tempest" ultimate ability upon reaching 100% Overdrive!
    /// </summary>
    public class ComboRankManager : MonoBehaviour
    {
        public static ComboRankManager Instance { get; private set; }

        [Header("Combo Configuration")]
        [SerializeField] private float decayDuration = 3.5f;
        [SerializeField] private float[] rankThresholds = new float[] { 0f, 100f, 250f, 500f, 850f, 1300f, 2000f };

        [Header("Overdrive Configuration")]
        [SerializeField] private float maxOverdrive = 100f;

        public event Action<ComboRank, float> OnRankChanged;
        public event Action<float> OnOverdriveChanged;
        public event Action OnBladeTempest;

        private float comboScore = 0f;
        private ComboRank currentRank = ComboRank.None;
        private float decayTimer = 0f;
        private float overdrive = 0f;
        private bool isOverdriveActive = false;

        public ComboRank CurrentRank => currentRank;
        public float OverdrivePercent => Mathf.Clamp01(overdrive / maxOverdrive);
        public bool IsOverdriveReady => overdrive >= maxOverdrive && !isOverdriveActive;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (decayTimer > 0f)
            {
                decayTimer -= Time.deltaTime;
                if (decayTimer <= 0f)
                {
                    DecayCombo();
                }
            }
        }

        public void AddCombatAction(string actionType, int basePoints = 50)
        {
            decayTimer = decayDuration;

            float multiplier = 1f + ((int)currentRank * 0.25f);
            float addedScore = basePoints * multiplier;
            comboScore += addedScore;

            // Overdrive gain increases with rank
            float overdriveGain = (basePoints * 0.08f) * multiplier;
            overdrive = Mathf.Clamp(overdrive + overdriveGain, 0f, maxOverdrive);
            OnOverdriveChanged?.Invoke(OverdrivePercent);

            EvaluateRank();
        }

        public void OnPlayerDamaged()
        {
            // Drop combo by 2 tiers upon getting hit
            int targetRankIndex = Mathf.Max(0, (int)currentRank - 2);
            comboScore = rankThresholds[targetRankIndex];
            decayTimer = decayDuration * 0.5f;
            EvaluateRank();
        }

        private void EvaluateRank()
        {
            ComboRank newRank = ComboRank.None;
            for (int i = rankThresholds.Length - 1; i >= 1; i--)
            {
                if (comboScore >= rankThresholds[i])
                {
                    newRank = (ComboRank)i;
                    break;
                }
            }

            if (newRank != currentRank)
            {
                bool rankedUp = newRank > currentRank;
                currentRank = newRank;

                if (rankedUp)
                {
                    if (currentRank == ComboRank.SSS)
                    {
                        if (AudioManager.Instance != null) AudioManager.Instance.PlaySound("combo_sss");
                        AchievementManager.Instance?.Unlock("ACH_COMBO_KING");
                    }
                    else
                    {
                        if (AudioManager.Instance != null) AudioManager.Instance.PlaySound("combo_up", 0.7f);
                    }
                }

                OnRankChanged?.Invoke(currentRank, comboScore);
            }
        }

        private void DecayCombo()
        {
            if (currentRank > ComboRank.None)
            {
                int lowerIndex = (int)currentRank - 1;
                comboScore = rankThresholds[lowerIndex];
                decayTimer = decayDuration * 0.75f;
                EvaluateRank();
            }
        }

        /// <summary>
        /// Unleashes the ultimate Blade Tempest attack!
        /// </summary>
        public bool TryActivateBladeTempest(Transform playerTransform)
        {
            if (!IsOverdriveReady || isOverdriveActive) return false;

            StartCoroutine(BladeTempestRoutine(playerTransform));
            return true;
        }

        private IEnumerator BladeTempestRoutine(Transform playerTransform)
        {
            isOverdriveActive = true;
            overdrive = 0f;
            OnOverdriveChanged?.Invoke(0f);
            OnBladeTempest?.Invoke();

            // 1. Slow motion bullet-time & Camera Zoom
            CombatJuice.SlowMotion(0.18f, 1.35f);
            CombatJuice.CinematicZoom(5.2f, 0.35f, 1.0f);
            CameraFollow.Shake(0.6f, 0.7f);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound("blade_tempest");
            }

            // 2. Clear enemy projectiles on screen
            var projectiles = FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None);
            foreach (var proj in projectiles)
            {
                if (proj != null)
                {
                    proj.Deflect(Vector2.right);
                }
            }

            // 3. Perform 7 rapid screen-slashes against all enemies within range
            var allHealths = FindObjectsByType<Health>(FindObjectsSortMode.None);
            List<Health> targetEnemies = new List<Health>();

            Vector2 center = playerTransform != null ? (Vector2)playerTransform.position : Vector2.zero;
            foreach (var h in allHealths)
            {
                if (h != null && !h.IsDead && h.gameObject.layer != LayerMask.NameToLayer("Player"))
                {
                    if (h.GetComponent<EnemyProjectile>() != null) continue;
                    if (Vector2.Distance(center, h.transform.position) <= 14f)
                    {
                        targetEnemies.Add(h);
                    }
                }
            }

            for (int i = 0; i < 7; i++)
            {
                Vector2 randomOffset = UnityEngine.Random.insideUnitCircle * 2.5f;
                CombatJuice.DirectionalShake(randomOffset, 0.12f, 0.4f);

                foreach (var enemy in targetEnemies)
                {
                    if (enemy != null && !enemy.IsDead)
                    {
                        enemy.TakeDamage(1, randomOffset.normalized * 5f);
                        UI.FloatingTextManager.ShowDamage(enemy.transform.position, 1, true);
                    }
                }

                yield return new WaitForSecondsRealtime(0.12f);
            }

            // Final blast
            CameraFollow.Shake(0.7f, 0.8f);
            if (playerTransform != null)
            {
                UI.FloatingTextManager.ShowDamage(playerTransform.position, 0, true);
            }

            AchievementManager.Instance?.Unlock("ACH_BLADE_TEMPEST");

            yield return new WaitForSecondsRealtime(0.3f);
            isOverdriveActive = false;
        }

        public string GetRankTitle(ComboRank rank)
        {
            switch (rank)
            {
                case ComboRank.D: return "DARING";
                case ComboRank.C: return "COOL";
                case ComboRank.B: return "BRUTAL";
                case ComboRank.A: return "ANARCHIC";
                case ComboRank.S: return "SUPREME";
                case ComboRank.SSS: return "SENSATIONAL!!";
                default: return "";
            }
        }
    }
}
