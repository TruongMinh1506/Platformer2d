using UnityEngine;
using Platformer.Common;
using Platformer.Player;

namespace Platformer.Managers
{
    /// <summary>
    /// Event binder that connects gameplay events to AudioManager sound triggers.
    /// Eliminates tight coupling between gameplay controllers and audio systems.
    /// Uses modern Unity 6 APIs (FindFirstObjectByType).
    /// </summary>
    public class AudioEventBinder : MonoBehaviour
    {
        private PlayerController playerController;
        private PlayerCombat playerCombat;
        private Health playerHealth;

        private void Start()
        {
            // Subscribe to GameManager events
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnCoinsChanged += HandleCoinsChanged;
                GameManager.Instance.OnGameOver += HandleGameOver;
            }

            // Locate and hook player events using Unity 6 API
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
            if (player != null)
            {
                playerController = player;
                playerCombat = player.GetComponent<PlayerCombat>();
                playerHealth = player.GetComponent<Health>();

                if (playerController != null)
                {
                    playerController.OnJumped += HandleJumped;
                    playerController.OnDashed += HandleDashed;
                }

                if (playerCombat != null)
                {
                    playerCombat.OnAttacked += HandleAttacked;
                    playerCombat.OnStomped += HandleStomped;
                }

                if (playerHealth != null)
                {
                    playerHealth.OnDamaged.AddListener(HandleDamaged);
                }
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnCoinsChanged -= HandleCoinsChanged;
                GameManager.Instance.OnGameOver -= HandleGameOver;
            }

            if (playerController != null)
            {
                playerController.OnJumped -= HandleJumped;
                playerController.OnDashed -= HandleDashed;
            }

            if (playerCombat != null)
            {
                playerCombat.OnAttacked -= HandleAttacked;
                playerCombat.OnStomped -= HandleStomped;
            }

            if (playerHealth != null)
            {
                playerHealth.OnDamaged.RemoveListener(HandleDamaged);
            }
        }

        private void HandleCoinsChanged(int coins) => Play("coin");
        private void HandleGameOver() => Play("lose");
        private void HandleJumped() => Play("jump");
        private void HandleDashed() => Play("dash");
        private void HandleAttacked() { /* Attack whoosh/slash is handled specifically in PlayerCombat */ }
        private void HandleStomped() => Play("enemy death");
        private void HandleDamaged(int hp) => Play("hit");

        private void Play(string clipName)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(clipName);
            }
        }
    }
}

