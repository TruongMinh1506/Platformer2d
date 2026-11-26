using UnityEngine;
using Platformer.Common;

namespace Platformer.Player
{
    /// <summary>
    /// Forwards player physics and combat states to the Animator parameters.
    /// Contains NO gameplay, movement, or combat logic.
    /// </summary>
    [RequireComponent(typeof(Animator), typeof(Rigidbody2D))]
    public class PlayerAnimation : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator animator;
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private PlayerCombat playerCombat;
        [SerializeField] private Health playerHealth;

        [Header("Cosmetic Effects")]
        [Tooltip("Dust particle prefab instantiated upon touching the ground.")]
        [SerializeField] private GameObject landingDustPrefab;

        [Tooltip("Dust particle prefab instantiated upon jumping from the ground.")]
        [SerializeField] private GameObject jumpDustPrefab;

        private static readonly int ParamSpeed = Animator.StringToHash("Speed");
        private static readonly int ParamIsGrounded = Animator.StringToHash("IsGrounded");
        private static readonly int ParamVerticalVelocity = Animator.StringToHash("VerticalVelocity");
        private static readonly int ParamAttack = Animator.StringToHash("Attack");
        private static readonly int ParamHurt = Animator.StringToHash("Hurt");
        private static readonly int ParamDead = Animator.StringToHash("Dead");

        private void Awake()
        {
            if (animator == null) animator = GetComponent<Animator>();
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            if (playerController == null) playerController = GetComponent<PlayerController>();
            if (playerCombat == null) playerCombat = GetComponent<PlayerCombat>();
            if (playerHealth == null) playerHealth = GetComponent<Health>();
        }

        private void OnEnable()
        {
            if (playerCombat != null)
            {
                playerCombat.OnAttacked += HandleAttack;
            }
            if (playerHealth != null)
            {
                playerHealth.OnDamaged.AddListener(HandleDamaged);
                playerHealth.OnDied.AddListener(HandleDied);
            }
            if (playerController != null)
            {
                playerController.OnLanded += HandleLanded;
                playerController.OnJumped += HandleJumped;
            }
        }

        private void OnDisable()
        {
            if (playerCombat != null)
            {
                playerCombat.OnAttacked -= HandleAttack;
            }
            if (playerHealth != null)
            {
                playerHealth.OnDamaged.RemoveListener(HandleDamaged);
                playerHealth.OnDied.RemoveListener(HandleDied);
            }
            if (playerController != null)
            {
                playerController.OnLanded -= HandleLanded;
                playerController.OnJumped -= HandleJumped;
            }
        }

        private void Update()
        {
            if (animator == null || rb == null) return;

            // Forward horizontal running speed
            animator.SetFloat(ParamSpeed, Mathf.Abs(rb.linearVelocity.x));

            // Forward vertical velocity for jumping/falling distinction
            animator.SetFloat(ParamVerticalVelocity, rb.linearVelocity.y);

            // Forward grounded status
            if (playerController != null)
            {
                animator.SetBool(ParamIsGrounded, playerController.IsGrounded);
            }

            // Forward dead state
            if (playerHealth != null)
            {
                animator.SetBool(ParamDead, playerHealth.IsDead);
            }
        }

        private void HandleAttack()
        {
            if (animator != null)
            {
                animator.SetTrigger(ParamAttack);
            }
        }

        private void HandleDamaged(int currentHealth)
        {
            if (animator != null)
            {
                animator.SetTrigger(ParamHurt);
            }
        }

        private void HandleDied()
        {
            if (animator != null)
            {
                animator.SetBool(ParamDead, true);
            }
        }

        private void HandleLanded()
        {
            if (landingDustPrefab != null)
            {
                Instantiate(landingDustPrefab, transform.position + new Vector3(0f, -0.7f, 0f), Quaternion.identity);
            }
        }

        private void HandleJumped()
        {
            if (jumpDustPrefab != null)
            {
                Instantiate(jumpDustPrefab, transform.position + new Vector3(0f, -0.7f, 0f), Quaternion.identity);
            }
            else if (landingDustPrefab != null)
            {
                Instantiate(landingDustPrefab, transform.position + new Vector3(0f, -0.7f, 0f), Quaternion.identity);
            }
        }
    }
}

