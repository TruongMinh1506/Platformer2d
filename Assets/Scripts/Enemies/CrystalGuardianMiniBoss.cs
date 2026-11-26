using System.Collections;
using UnityEngine;
using TMPro;
using Platformer.Common;
using Platformer.Managers;
using Platformer.UI;

namespace Platformer.Enemies
{
    /// <summary>
    /// Level 2 Mini-Boss: Crystal Guardian (Hộ Vệ Pha Lê).
    /// Features an impenetrable crystalline frontal shield that deflects melee attacks.
    /// Shield can be staggered/shattered via Stomp, Parry, or striking from behind!
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(Health))]
    public class CrystalGuardianMiniBoss : MonoBehaviour
    {
        [Header("Boss Stats")]
        [SerializeField] private int maxHp = 8;
        [SerializeField] private float moveSpeed = 1.8f;
        [SerializeField] private float attackRange = 3.2f;
        [SerializeField] private float thrustSpeed = 7.5f;

        [Header("Shield Mechanics")]
        [SerializeField] private bool isShieldActive = true;
        [SerializeField] private float shieldBreakDuration = 3.0f;

        private Rigidbody2D rb;
        private Health health;
        private SpriteRenderer spriteRenderer;
        private Transform player;
        private bool isAttacking = false;
        private bool isStunned = false;
        private int facingDirection = 1;
        private float attackCooldownTimer = 1.5f;

        // Visual HUD elements
        private GameObject healthBarContainer;
        private Transform healthFillTr;
        private TextMeshPro nameTmp;

        public bool IsShieldActive => isShieldActive && !isStunned;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            health = GetComponent<Health>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        private void Start()
        {
            if (health != null)
            {
                health.SetMaxHealth(maxHp, true);
                health.OnDamaged.AddListener(OnDamaged);
                health.OnDied.AddListener(OnDefeated);
            }

            CreateMiniBossHUD();
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.OnDamaged.RemoveListener(OnDamaged);
                health.OnDied.RemoveListener(OnDefeated);
            }
        }

        private void Update()
        {
            if (health != null && health.IsDead) return;

            if (player == null)
            {
                var p = GameObject.FindWithTag("Player");
                if (p != null) player = p.transform;
                return;
            }

            float distToPlayer = Vector2.Distance(transform.position, player.position);

            // Show HUD when player is within battle range (14 units)
            if (healthBarContainer != null)
            {
                healthBarContainer.SetActive(distToPlayer <= 14f);
                // Cancel parent X scale so boss title and health bar are never mirrored backwards
                healthBarContainer.transform.localScale = new Vector3(facingDirection, 1f, 1f);
            }

            if (isStunned || isAttacking) return;

            // Face the player
            float dirX = player.position.x - transform.position.x;
            if (Mathf.Abs(dirX) > 0.4f)
            {
                facingDirection = dirX > 0 ? 1 : -1;
                transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x) * facingDirection, transform.localScale.y, transform.localScale.z);
            }

            // Patrol towards player if in pursuit range
            if (distToPlayer > attackRange && distToPlayer < 12f)
            {
                rb.linearVelocity = new Vector2(facingDirection * moveSpeed, rb.linearVelocity.y);
            }
            else
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }

            // Attack cooldown and trigger
            if (attackCooldownTimer > 0f)
            {
                attackCooldownTimer -= Time.deltaTime;
            }
            else if (distToPlayer <= attackRange)
            {
                StartCoroutine(ThrustAttackRoutine());
            }
        }

        private IEnumerator ThrustAttackRoutine()
        {
            isAttacking = true;
            attackCooldownTimer = 2.8f;

            // Telegraph: Flash red/white for 0.45s
            Color orig = spriteRenderer != null ? spriteRenderer.color : Color.white;
            float flash = 0f;
            while (flash < 0.45f)
            {
                flash += Time.deltaTime;
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = Color.Lerp(orig, new Color(1f, 0.4f, 0.4f, 1f), Mathf.PingPong(flash * 8f, 1f));
                }
                yield return null;
            }
            if (spriteRenderer != null) spriteRenderer.color = orig;

            // Thrust Lunge forward
            rb.linearVelocity = new Vector2(facingDirection * thrustSpeed, 0f);
            yield return new WaitForSeconds(0.35f);

            rb.linearVelocity = Vector2.zero;
            yield return new WaitForSeconds(0.4f);

            isAttacking = false;
        }

        /// <summary>
        /// Shatters the crystal shield, stunning the boss.
        /// </summary>
        public void ShatterShield()
        {
            if (!isShieldActive) return;
            StartCoroutine(ShieldBreakRoutine());
        }

        private IEnumerator ShieldBreakRoutine()
        {
            isShieldActive = false;
            isStunned = true;
            isAttacking = false;
            rb.linearVelocity = Vector2.zero;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound("crystal_shatter");
            }
            FloatingTextManager.ShowDamage(transform.position + Vector3.up * 1.2f, 0, true);
            CombatJuice.SlowMotion(0.2f, 0.4f);

            // Stunned visual color (dim bluish-gray)
            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(0.5f, 0.6f, 0.8f, 0.85f);
            }

            yield return new WaitForSeconds(shieldBreakDuration);

            // Restore shield
            isStunned = false;
            isShieldActive = true;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.white;
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (health != null && health.IsDead) return;

            if (collision.gameObject.CompareTag("Player"))
            {
                // Check if player stomped from above
                for (int i = 0; i < collision.contactCount; i++)
                {
                    if (collision.GetContact(i).normal.y < -0.5f)
                    {
                        // Stomp shatters shield!
                        ShatterShield();
                        return;
                    }
                }

                // If shield is active and attack came from front, deflect!
                float relX = collision.transform.position.x - transform.position.x;
                bool isFromFront = (relX * facingDirection) > 0f;

                if (IsShieldActive && isFromFront)
                {
                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.PlaySound("crystal_clink");
                    }
                    CombatJuice.Shake(0.12f, 0.25f);
                }
            }
        }

        private void OnDamaged(int currentHealth)
        {
            UpdateHealthFill();
        }

        private void UpdateHealthFill()
        {
            if (healthFillTr != null && health != null)
            {
                float pct = Mathf.Clamp01((float)health.CurrentHealth / health.MaxHealth);
                healthFillTr.localScale = new Vector3(pct, 1f, 1f);
            }
        }

        private void CreateMiniBossHUD()
        {
            healthBarContainer = new GameObject("MiniBoss_HUD");
            healthBarContainer.transform.SetParent(transform, false);
            healthBarContainer.transform.localPosition = new Vector3(0f, 1.8f, 0f);

            // Title
            GameObject titleGo = new GameObject("Title");
            titleGo.transform.SetParent(healthBarContainer.transform, false);
            titleGo.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            nameTmp = titleGo.AddComponent<TextMeshPro>();
            nameTmp.text = "<color=#00e5ff>CRYSTAL GUARDIAN</color>";
            nameTmp.fontSize = 2.4f;
            nameTmp.alignment = TextAlignmentOptions.Center;
            nameTmp.sortingOrder = 15;

            // Bar Background
            GameObject bgGo = new GameObject("Bar_BG");
            bgGo.transform.SetParent(healthBarContainer.transform, false);
            var srBg = bgGo.AddComponent<SpriteRenderer>();
            srBg.sprite = CreateQuadSprite(new Color(0.1f, 0.12f, 0.16f, 0.9f));
            bgGo.transform.localScale = new Vector3(2.5f, 0.25f, 1f);
            srBg.sortingOrder = 14;

            // Bar Fill
            GameObject fillGo = new GameObject("Bar_Fill");
            fillGo.transform.SetParent(healthBarContainer.transform, false);
            fillGo.transform.localPosition = new Vector3(-1.25f, 0f, 0f);
            healthFillTr = fillGo.transform;

            GameObject innerGo = new GameObject("Inner");
            innerGo.transform.SetParent(fillGo.transform, false);
            innerGo.transform.localPosition = new Vector3(1.25f, 0f, 0f);
            innerGo.transform.localScale = new Vector3(2.5f, 0.2f, 1f);
            var srFill = innerGo.AddComponent<SpriteRenderer>();
            srFill.sprite = CreateQuadSprite(new Color(0f, 0.85f, 1f, 1f));
            srFill.sortingOrder = 15;

            UpdateHealthFill();
        }

        private Sprite CreateQuadSprite(Color color)
        {
            Texture2D tex = new Texture2D(4, 4);
            Color[] cols = new Color[16];
            for (int i = 0; i < cols.Length; i++) cols[i] = color;
            tex.SetPixels(cols);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        }

        private void OnDefeated()
        {
            if (healthBarContainer != null) Destroy(healthBarContainer);

            var cols = GetComponentsInChildren<Collider2D>();
            foreach (var c in cols) c.enabled = false;
            if (rb != null) rb.linearVelocity = Vector2.zero;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound("crystal_shatter");
            }
            CombatJuice.SlowMotion(0.2f, 0.6f);
            CameraFollow.Shake(0.35f, 0.6f);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddScore(1000);
                GameManager.Instance.AddCoin(5);
            }

            if (ComboRankManager.Instance != null)
            {
                ComboRankManager.Instance.AddCombatAction("MiniBossDefeat", 300);
            }

            Destroy(gameObject, 0.3f);
        }
    }
}
