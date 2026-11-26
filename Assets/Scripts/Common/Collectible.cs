using UnityEngine;
using Platformer.Managers;

namespace Platformer.Common
{
    public enum CollectibleType
    {
        Coin,
        Heart
    }

    /// <summary>
    /// Collectible item (Coin or Heart) with procedural sinusoidal bobbing animation in code.
    /// Grants score/coins or heals player upon contact.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Collectible : MonoBehaviour
    {
        [Header("Collectible Settings")]
        [SerializeField] private CollectibleType type = CollectibleType.Coin;

        [Tooltip("Score granted when collected.")]
        [SerializeField] private int scoreValue = 100;
        public int ScoreValue => scoreValue;

        [Tooltip("Coins count added when collected.")]
        [SerializeField] private int coinAmount = 1;

        [Tooltip("Health restored when collecting a Heart.")]
        [SerializeField] private int healAmount = 1;

        [Header("Cosmetic Effects")]
        [Tooltip("Particle effect instantiated upon collection.")]
        [SerializeField] private GameObject pickupEffectPrefab;

        [Header("Bobbing Animation")]
        [Tooltip("Maximum vertical offset displacement during bobbing.")]
        [SerializeField] private float bobAmplitude = 0.12f;

        [Tooltip("Speed/frequency of the sinusoidal bobbing.")]
        [SerializeField] private float bobFrequency = 3.2f;

        private Vector3 basePosition;
        private float randomOffset;

        private void Start()
        {
            basePosition = transform.position;
            // Desynchronize neighboring collectibles so they don't bob in lockstep
            randomOffset = (transform.position.x * 1.7f) % 6.28f;
        }

        private void Update()
        {
            // Procedural sine-wave bobbing in code
            float verticalOffset = Mathf.Sin((Time.time * bobFrequency) + randomOffset) * bobAmplitude;
            transform.position = basePosition + new Vector3(0f, verticalOffset, 0f);
        }

        /// <summary>
        /// Moves the collectible towards a target while preserving procedural bobbing (for Coin Magnet).
        /// </summary>
        public void MoveTowards(Vector3 target, float maxDelta)
        {
            basePosition = Vector3.MoveTowards(basePosition, target, maxDelta);
            float verticalOffset = Mathf.Sin((Time.time * bobFrequency) + randomOffset) * bobAmplitude;
            transform.position = basePosition + new Vector3(0f, verticalOffset, 0f);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player") && other.gameObject.layer != LayerMask.NameToLayer("Player")) return;

            if (pickupEffectPrefab != null)
            {
                Instantiate(pickupEffectPrefab, transform.position, Quaternion.identity);
            }

            if (gameObject.name.Contains("Relic"))
            {
                AchievementManager.Instance?.Unlock("ACH_ARCHAEOLOGIST");
            }

            if (type == CollectibleType.Coin)
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.AddCoin(coinAmount);
                }
                Destroy(gameObject);
            }
            else if (type == CollectibleType.Heart)
            {
                Health playerHealth = other.GetComponentInParent<Health>();
                if (playerHealth != null)
                {
                    if (playerHealth.CurrentHealth < playerHealth.MaxHealth)
                    {
                        playerHealth.Heal(healAmount);
                        if (GameManager.Instance != null)
                        {
                            GameManager.Instance.AddScore(50);
                        }
                        Destroy(gameObject);
                    }
                    else
                    {
                        var playerEnergy = other.GetComponentInParent<Player.PlayerEnergy>();
                        if (playerEnergy != null && playerEnergy.CurrentEnergy < playerEnergy.MaxEnergy)
                        {
                            playerEnergy.AddEnergy(30f);
                            if (GameManager.Instance != null)
                            {
                                GameManager.Instance.AddScore(100);
                            }
                            Destroy(gameObject);
                        }
                        else
                        {
                            // If both HP and Energy are full, award bonus score so the pickup is collected
                            if (GameManager.Instance != null)
                            {
                                GameManager.Instance.AddScore(150);
                            }
                            Destroy(gameObject);
                        }
                    }
                }
            }
        }
    }
}

