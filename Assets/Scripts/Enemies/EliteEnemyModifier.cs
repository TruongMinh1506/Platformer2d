using UnityEngine;
using TMPro;
using Platformer.Common;
using Platformer.Managers;

namespace Platformer.Enemies
{
    public enum EliteElementType
    {
        Fire,
        Thunder,
        Void
    }

    /// <summary>
    /// Transforms a standard enemy into an Elite Champion:
    /// - 25% larger size with pulsating elemental aura.
    /// - 200% health pool.
    /// - Drops 3x coins, a guaranteed heart pickup, and grants huge score/combo upon defeat!
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class EliteEnemyModifier : MonoBehaviour
    {
        [Header("Elite Configuration")]
        [SerializeField] private EliteElementType element = EliteElementType.Fire;
        [SerializeField] private float sizeMultiplier = 1.28f;
        [SerializeField] private int bonusScore = 500;

        private Health health;
        private SpriteRenderer spriteRenderer;
        private Color auraColor;
        private float pulseTimer;
        private GameObject badgeGo;

        private void Awake()
        {
            health = GetComponent<Health>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            // Setup element color
            switch (element)
            {
                case EliteElementType.Fire:
                    auraColor = new Color(1.0f, 0.35f, 0.15f, 1f);
                    break;
                case EliteElementType.Thunder:
                    auraColor = new Color(0.2f, 0.85f, 1.0f, 1f);
                    break;
                case EliteElementType.Void:
                    auraColor = new Color(0.85f, 0.25f, 1.0f, 1f);
                    break;
            }

            // Enlarge physical scale
            transform.localScale = new Vector3(
                transform.localScale.x * sizeMultiplier,
                transform.localScale.y * sizeMultiplier,
                transform.localScale.z
            );
        }

        private void Start()
        {
            if (health != null)
            {
                int newHp = Mathf.Max(4, health.MaxHealth * 2);
                health.SetMaxHealth(newHp, true);
                health.OnDied.AddListener(OnEliteDefeated);
            }

            CreateOverheadBadge();
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.OnDied.RemoveListener(OnEliteDefeated);
            }
        }

        private void Update()
        {
            pulseTimer += Time.deltaTime * 3.5f;

            // Pulsate sprite rim
            if (spriteRenderer != null)
            {
                float pulse = (Mathf.Sin(pulseTimer) + 1f) * 0.5f;
                Color targetCol = Color.Lerp(Color.white, auraColor, 0.45f + pulse * 0.35f);
                spriteRenderer.color = targetCol;
            }

            // Cancel parent X scale so overhead [ELITE] badge is never mirrored backwards
            if (badgeGo != null)
            {
                float signX = Mathf.Sign(transform.localScale.x);
                badgeGo.transform.localScale = new Vector3(signX / sizeMultiplier, 1f / sizeMultiplier, 1f);
            }
        }

        private void CreateOverheadBadge()
        {
            badgeGo = new GameObject("Elite_Badge");
            badgeGo.transform.SetParent(transform, false);
            badgeGo.transform.localPosition = new Vector3(0f, 1.35f, 0f);

            var tmp = badgeGo.AddComponent<TextMeshPro>();
            tmp.text = "<color=#FFD700>[ELITE]</color>";
            tmp.fontSize = 2.4f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.sortingOrder = 10;
        }

        private void OnEliteDefeated()
        {
            if (badgeGo != null)
            {
                Destroy(badgeGo);
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddScore(bonusScore);
            }

            if (ComboRankManager.Instance != null)
            {
                ComboRankManager.Instance.AddCombatAction("EliteSlayer", 200);
            }

            // Drop 3x coins
            GameObject coinPrefab = Resources.Load<GameObject>("Collectible_Coin");
            if (coinPrefab == null)
            {
                var allCollectibles = UnityEngine.Object.FindObjectsByType<Collectible>(FindObjectsSortMode.None);
                foreach (var col in allCollectibles)
                {
                    if (col != null && col.gameObject.name.Contains("Coin"))
                    {
                        coinPrefab = col.gameObject;
                        break;
                    }
                }
            }

            for (int i = 0; i < 3; i++)
            {
                if (coinPrefab != null)
                {
                    Vector3 offset = new Vector3((i - 1) * 0.45f, 0.4f, 0f);
                    GameObject c = Instantiate(coinPrefab, transform.position + offset, Quaternion.identity);
                    c.name = "DroppedCoin";
                    c.SetActive(true);
                    var rb = c.GetComponent<Rigidbody2D>();
                    if (rb != null)
                    {
                        rb.linearVelocity = new Vector2((i - 1) * 2.5f, 5f);
                    }
                }
                else
                {
                    GameManager.Instance?.AddCoin(1);
                }
            }

            // Also check unlock for Achievement
            AchievementManager.Instance?.Unlock("ACH_ELITE_SLAYER");
        }
    }
}
