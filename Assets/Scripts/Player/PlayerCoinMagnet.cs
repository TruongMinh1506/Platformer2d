using UnityEngine;
using Platformer.Common;

namespace Platformer.Player
{
    /// <summary>
    /// Vacuums nearby collectible coins towards the player when Coin Magnet perk is upgraded.
    /// </summary>
    public class PlayerCoinMagnet : MonoBehaviour
    {
        [Tooltip("Effective pull radius in world units.")]
        [SerializeField] private float magnetRadius = 0f;

        [Tooltip("Speed at which coins fly towards player.")]
        [SerializeField] private float pullSpeed = 9.0f;

        [Tooltip("LayerMask containing Collectible objects.")]
        [SerializeField] private LayerMask collectibleLayer;

        private void Awake()
        {
            if (collectibleLayer == 0)
            {
                // Default to all layers if not configured, or find by Collectible component
                collectibleLayer = ~0;
            }
        }

        public void SetRadius(float newRadius)
        {
            magnetRadius = newRadius;
        }

        private void Update()
        {
            float effectiveRadius = magnetRadius;
            if (Managers.CharmManager.Instance != null && Managers.CharmManager.Instance.IsEquipped(Managers.CharmType.MagnetRing))
            {
                effectiveRadius = Mathf.Max(6.5f, effectiveRadius * 2.5f);
            }

            if (effectiveRadius <= 0.1f) return;

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, effectiveRadius, collectibleLayer);
            foreach (var hit in hits)
            {
                var collectible = hit.GetComponent<Collectible>();
                if (collectible != null)
                {
                    // Move coin smoothly towards player by updating its base position
                    collectible.MoveTowards(transform.position, pullSpeed * Time.deltaTime);
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (magnetRadius > 0.1f)
            {
                Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.35f);
                Gizmos.DrawWireSphere(transform.position, magnetRadius);
            }
        }
    }
}
