using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using Platformer.Common;
using Platformer.Managers;

namespace Platformer.Player
{
    /// <summary>
    /// Manages player combat actions:
    /// 1. Melee attack (Key J / Left Mouse) with cooldown and hitbox in front.
    /// 2. Head stomp bounce on enemies from above.
    /// 3. Side-touch damage reception with knockback.
    /// </summary>
    [RequireComponent(typeof(PlayerController), typeof(Health), typeof(Rigidbody2D))]
    public class PlayerCombat : MonoBehaviour
    {
        [Header("Melee Attack")]
        [Tooltip("Cooldown period between melee attacks.")]
        [SerializeField] private float attackCooldown = 0.4f;

        [Tooltip("Damage dealt by melee attack.")]
        [SerializeField] private int attackDamage = 1;

        [Tooltip("Radius of the melee attack hitbox.")]
        [SerializeField] private float attackRadius = 0.85f;

        [Tooltip("Positional offset of the attack hitbox relative to player facing direction.")]
        [SerializeField] private Vector2 attackOffset = new Vector2(0.95f, 0f);

        [Tooltip("Knockback force applied to enemies hit by melee attack.")]
        [SerializeField] private float enemyKnockbackForce = 8.5f;

        [Tooltip("LayerMask containing targetable enemies.")]
        [SerializeField] private LayerMask enemyLayer;

        [Tooltip("Visual sprite displayed briefly during an attack.")]
        [SerializeField] private Sprite attackSlashSprite;

        [Header("Stomp & Contact Damage")]
        [Tooltip("Damage dealt to an enemy when jumping on top of them.")]
        [SerializeField] private int stompDamage = 1;

        [Tooltip("Upward bounce velocity impulse upon successful enemy stomp.")]
        [SerializeField] private float bounceForce = 13.5f;

        [Tooltip("Knockback velocity impulse applied to the player when touched from the side.")]
        [SerializeField] private float sideKnockbackForce = 9.5f;

        [Header("References")]
        [SerializeField] private PlayerController playerController;
        [SerializeField] private Health playerHealth;
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private PlayerEnergy playerEnergy;

        [Header("Energy & Ranged Skill")]
        [Tooltip("Cost in energy to cast Energy Blade Slash.")]
        [SerializeField] private float energySlashCost = 30f;

        [Tooltip("Prefab for Energy Blade Projectile.")]
        [SerializeField] private GameObject energySlashPrefab;

        [Tooltip("Energy gained when landing a melee sword hit.")]
        [SerializeField] private float energyPerMeleeHit = 20f;

        [Tooltip("Energy gained when performing a head stomp.")]
        [SerializeField] private float energyPerStomp = 25f;

        [Tooltip("Energy gained when performing a dash slice.")]
        [SerializeField] private float energyPerDashSlice = 20f;

        [Header("Parry & Deflect")]
        [Tooltip("Active window (seconds) during which incoming projectiles or attacks are deflected.")]
        [SerializeField] private float parryDuration = 0.22f;

        [Tooltip("Cooldown period between parry attempts.")]
        [SerializeField] private float parryCooldown = 0.45f;

        [Tooltip("Energy gained upon executing a successful parry or deflect.")]
        [SerializeField] private float parryEnergyGain = 20f;

        public event System.Action OnAttacked;
        public event System.Action OnStomped;
        public event System.Action OnRangedSlash;
        public event System.Action OnParried;

        private float cooldownTimer;
        private float parryTimer;
        private float parryCooldownTimer;
        private int comboStep = 0;
        private float lastAttackTime = 0f;
        private readonly System.Collections.Generic.HashSet<Collider2D> dashedEnemies = new System.Collections.Generic.HashSet<Collider2D>();

        public bool IsParrying => parryTimer > 0f;

        private void Awake()
        {
            if (playerController == null) playerController = GetComponent<PlayerController>();
            if (playerHealth == null) playerHealth = GetComponent<Health>();
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            if (playerEnergy == null) playerEnergy = GetComponent<PlayerEnergy>();
        }

        private void OnEnable()
        {
            if (playerController != null)
            {
                playerController.OnDashed += OnPlayerDashed;
            }
        }

        private void OnDisable()
        {
            if (playerController != null)
            {
                playerController.OnDashed -= OnPlayerDashed;
            }
        }

        private void OnPlayerDashed()
        {
            dashedEnemies.Clear();
        }

        private void Update()
        {
            cooldownTimer -= Time.deltaTime;
            if (parryTimer > 0f) parryTimer -= Time.deltaTime;
            if (parryCooldownTimer > 0f) parryCooldownTimer -= Time.deltaTime;

            // Perform dash slice damage detection while dashing through enemies
            if (playerController != null && playerController.IsDashing)
            {
                CheckDashSlice();
            }

            if (ReadUltimateDown())
            {
                if (ComboRankManager.Instance != null)
                {
                    if (!ComboRankManager.Instance.TryActivateBladeTempest(transform))
                    {
                        if (AudioManager.Instance != null) AudioManager.Instance.PlaySound("ui_deny", 0.5f);
                    }
                }
            }
            else if (ReadParryDown() && parryCooldownTimer <= 0f)
            {
                ExecuteParry();
            }
            else if (ReadAttackDown() && cooldownTimer <= 0f)
            {
                ExecuteMeleeAttack();
            }
            else if (ReadRangedAttackDown() && cooldownTimer <= 0f)
            {
                ExecuteRangedAttack();
            }
        }

        private void CheckDashSlice()
        {
            LayerMask mask = enemyLayer.value != 0 ? enemyLayer : (LayerMask)(1 << LayerMask.NameToLayer("Enemy"));
            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 1.25f, mask);
            foreach (var hit in hits)
            {
                if (dashedEnemies.Contains(hit)) continue;
                dashedEnemies.Add(hit);

                Health enemyHealth = hit.GetComponentInParent<Health>();
                if (enemyHealth != null && !enemyHealth.IsDead && enemyHealth.gameObject != gameObject)
                {
                    Vector2 dashKnock = ((Vector2)hit.transform.position - (Vector2)transform.position).normalized;
                    dashKnock.y = Mathf.Max(dashKnock.y, 0.3f);
                    enemyHealth.TakeDamage(attackDamage, dashKnock * enemyKnockbackForce);
                    UI.FloatingTextManager.ShowDamage(hit.transform.position, attackDamage, true);
                    if (playerEnergy != null) playerEnergy.AddEnergy(energyPerDashSlice);
                    CombatJuice.DirectionalShake(dashKnock, 0.16f, 0.32f);
                    CombatJuice.HitStop(0.08f);
                    ComboRankManager.Instance?.AddCombatAction("DashSlice", 75);
                }
            }
        }

        private void ExecuteParry()
        {
            parryTimer = parryDuration;
            parryCooldownTimer = parryCooldown;

            bool facingRight = playerController != null ? playerController.IsFacingRight : true;
            Vector2 origin = (Vector2)transform.position + new Vector2(facingRight ? 0.6f : -0.6f, 0f);

            // Spawn cyan/electric parry shield flash
            SpawnParryVisual(origin, facingRight);

            // Check if any incoming enemy projectile is within deflection range
            Collider2D[] nearby = Physics2D.OverlapCircleAll(origin, attackRadius * 1.5f);
            bool deflectedAny = false;
            foreach (var col in nearby)
            {
                var proj = col.GetComponent<Enemies.EnemyProjectile>();
                if (proj != null && !proj.IsReflected)
                {
                    Vector2 reflectDir = facingRight ? Vector2.right : Vector2.left;
                    proj.Deflect(reflectDir);
                    deflectedAny = true;
                    UI.FloatingTextManager.ShowDeflect(proj.transform.position);
                    CombatJuice.HitStop(0.1f);
                    CombatJuice.DirectionalShake(reflectDir, 0.18f, 0.45f);
                    if (AudioManager.Instance != null) AudioManager.Instance.PlaySound("deflect");
                    if (playerEnergy != null) playerEnergy.AddEnergy(parryEnergyGain);
                    ComboRankManager.Instance?.AddCombatAction("Deflect", 100);
                    AchievementManager.Instance?.Unlock("ACH_PARRY_MASTER");
                }
            }

            if (!deflectedAny)
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlaySound("parry", 0.5f);
            }

            OnParried?.Invoke();
        }

        private void SpawnParryVisual(Vector2 position, bool facingRight)
        {
            GameObject parryObj = new GameObject("ParryShieldEffect");
            parryObj.transform.position = position;
            parryObj.transform.localScale = new Vector3(facingRight ? 1.2f : -1.2f, 1.2f, 1f);

            SpriteRenderer sr = parryObj.AddComponent<SpriteRenderer>();
            sr.sprite = attackSlashSprite;
            sr.color = new Color(0.2f, 0.95f, 1f, 0.9f); // Electric cyan shield glint
            sr.sortingOrder = 16;

            Destroy(parryObj, parryDuration);
        }

        private void ExecuteRangedAttack()
        {
            if (playerEnergy != null && !playerEnergy.TryConsumeEnergy(energySlashCost))
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlaySound("ui_deny", 0.5f);
                return; // Not enough energy!
            }

            cooldownTimer = attackCooldown;
            bool facingRight = playerController != null ? playerController.IsFacingRight : true;
            Vector2 origin = (Vector2)transform.position + new Vector2(facingRight ? 0.85f : -0.85f, 0f);

            if (energySlashPrefab != null)
            {
                GameObject slashGo = Instantiate(energySlashPrefab, origin, Quaternion.identity);
                EnergySlashProjectile proj = slashGo.GetComponent<EnergySlashProjectile>();
                if (proj != null)
                {
                    proj.Initialize(facingRight ? Vector2.right : Vector2.left);
                }
            }

            CameraFollow.Shake(0.12f, 0.22f);
            OnRangedSlash?.Invoke();
        }

        private void ExecuteMeleeAttack()
        {
            cooldownTimer = attackCooldown;

            bool facingRight = playerController != null ? playerController.IsFacingRight : true;
            float vert = ReadVerticalInput();
            bool isGrounded = playerController != null ? playerController.IsGrounded : true;

            bool isUpSlash = vert > 0.4f;
            bool isDownSlash = !isGrounded && vert < -0.4f;

            Vector2 origin;
            float slashAngle = 0f;
            int damageDealt = attackDamage;
            float visualScale = 1f;

            if (isUpSlash)
            {
                origin = (Vector2)transform.position + new Vector2(0f, 0.95f);
                slashAngle = 90f;
                ComboRankManager.Instance?.AddCombatAction("UpSlash", 65);
            }
            else if (isDownSlash)
            {
                origin = (Vector2)transform.position + new Vector2(0f, -0.9f);
                slashAngle = -90f;
                ComboRankManager.Instance?.AddCombatAction("PogoSlash", 90);
            }
            else
            {
                // 3-Hit Combo
                if (Time.time - lastAttackTime > 0.85f)
                {
                    comboStep = 0;
                }
                else
                {
                    comboStep = (comboStep + 1) % 3;
                }
                lastAttackTime = Time.time;

                origin = (Vector2)transform.position + new Vector2(facingRight ? attackOffset.x : -attackOffset.x, attackOffset.y);
                if (comboStep == 1)
                {
                    slashAngle = facingRight ? 25f : -25f;
                    visualScale = 1.15f;
                }
                else if (comboStep == 2)
                {
                    slashAngle = facingRight ? -15f : 15f;
                    damageDealt += 1; // Heavy combo finisher!
                    visualScale = 1.4f;
                    CameraFollow.Shake(0.18f, 0.35f);
                    ComboRankManager.Instance?.AddCombatAction("Finisher", 85);
                }
                else
                {
                    ComboRankManager.Instance?.AddCombatAction("Melee", 50);
                }
            }

            // Spawn visual slash
            if (attackSlashSprite != null)
            {
                SpawnSlashVisual(origin, facingRight, slashAngle, visualScale);
            }

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound("sword_slash", comboStep == 2 ? 1f : 0.85f);
            }

            OnAttacked?.Invoke();

            // Detect enemy and secret wall colliders within attack radius
            int groundMask = LayerMask.GetMask("Ground", "Default");
            LayerMask targetEnemyMask = enemyLayer.value != 0 ? enemyLayer : (LayerMask)(1 << LayerMask.NameToLayer("Enemy"));
            LayerMask attackMask = targetEnemyMask | groundMask;
            Collider2D[] hits = Physics2D.OverlapCircleAll(origin, attackRadius * visualScale, attackMask);
            bool landedHit = false;
            foreach (var hit in hits)
            {
                if (hit.transform.root == transform) continue;

                // Check Secret Breakable Wall
                var secretWall = hit.GetComponentInParent<BreakableSecretWall>();
                if (secretWall != null)
                {
                    Health wallHealth = secretWall.GetComponent<Health>();
                    if (wallHealth != null && !wallHealth.IsDead)
                    {
                        landedHit = true;
                        wallHealth.TakeDamage(damageDealt);
                        UI.FloatingTextManager.ShowDamage(hit.transform.position, damageDealt, true);
                        CombatJuice.HitStop(0.06f);
                        CameraFollow.Shake(0.14f, 0.22f);
                    }
                    continue;
                }

                Health enemyHealth = hit.GetComponentInParent<Health>();
                if (enemyHealth != null && !enemyHealth.IsDead && !enemyHealth.CompareTag("Player"))
                {
                    landedHit = true;
                    Vector2 knockDir;
                    if (isUpSlash) knockDir = Vector2.up;
                    else if (isDownSlash) knockDir = Vector2.down;
                    else knockDir = ((Vector2)hit.transform.position - (Vector2)transform.position).normalized;
                    knockDir.y = Mathf.Max(knockDir.y, 0.3f);

                    // Check Crystal Guardian Mini-Boss frontal shield
                    var guardian = hit.GetComponentInParent<Enemies.CrystalGuardianMiniBoss>();
                    if (guardian != null && guardian.IsShieldActive)
                    {
                        if (isDownSlash)
                        {
                            guardian.ShatterShield();
                        }
                        else
                        {
                            float dirToBoss = hit.transform.position.x - transform.position.x;
                            float bossFacing = hit.transform.localScale.x;
                            bool attackFromFront = (dirToBoss * bossFacing) < 0f;
                            if (attackFromFront)
                            {
                                if (AudioManager.Instance != null) AudioManager.Instance.PlaySound("crystal_clink");
                                UI.FloatingTextManager.ShowDeflect(hit.transform.position);
                                CombatJuice.DirectionalShake(-knockDir, 0.1f, 0.25f);
                                continue;
                            }
                        }
                    }

                    int finalDamage = damageDealt;
                    if (CharmManager.Instance != null && CharmManager.Instance.IsEquipped(CharmType.BerserkerCrest))
                    {
                        if (playerHealth != null && playerHealth.CurrentHealth <= Mathf.CeilToInt(playerHealth.MaxHealth * 0.35f))
                        {
                            finalDamage = Mathf.CeilToInt(finalDamage * 1.5f);
                        }
                    }

                    enemyHealth.TakeDamage(finalDamage, knockDir * enemyKnockbackForce);
                    UI.FloatingTextManager.ShowDamage(hit.transform.position, finalDamage, finalDamage > attackDamage);
                    CombatJuice.HitStop(0.06f);
                    CombatJuice.DirectionalShake(knockDir, 0.14f, 0.35f);

                    // Downward pogo bounce on enemy hit!
                    if (isDownSlash && rb != null)
                    {
                        rb.linearVelocity = new Vector2(rb.linearVelocity.x, bounceForce);
                        playerController?.ResetJumps();
                        playerController?.ResetDash();
                    }

                    // Vampiric Fang Charm: heal 1 HP upon kill
                    if (enemyHealth.IsDead && CharmManager.Instance != null && CharmManager.Instance.IsEquipped(CharmType.VampiricFang))
                    {
                        if (playerHealth != null)
                        {
                            playerHealth.Heal(1);
                        }
                    }
                }
            }

            // Downward pogo check against Spikes!
            if (isDownSlash && rb != null)
            {
                Collider2D[] spikeHits = Physics2D.OverlapCircleAll(origin, attackRadius, LayerMask.GetMask("Default", "Ground"));
                foreach (var sp in spikeHits)
                {
                    if (sp.GetComponentInParent<HazardSpikes>() != null)
                    {
                        rb.linearVelocity = new Vector2(rb.linearVelocity.x, bounceForce);
                        playerController?.ResetJumps();
                        playerController?.ResetDash();
                        if (AudioManager.Instance != null) AudioManager.Instance.PlaySound("deflect");
                        CombatJuice.HitStop(0.07f);
                        CombatJuice.Shake(0.15f, 0.3f);
                        break;
                    }
                }
            }

            if (landedHit && playerEnergy != null)
            {
                playerEnergy.AddEnergy(energyPerMeleeHit);
            }
        }

        private void SpawnSlashVisual(Vector2 position, bool facingRight, float angle = 0f, float scale = 1f)
        {
            GameObject slashObj = new GameObject("SlashEffect");
            slashObj.transform.position = position;
            slashObj.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            slashObj.transform.localScale = new Vector3((facingRight ? 1f : -1f) * scale, scale, 1f);

            SpriteRenderer sr = slashObj.AddComponent<SpriteRenderer>();
            sr.sprite = attackSlashSprite;
            sr.sortingOrder = 15;

            Destroy(slashObj, 0.12f);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            HandleEnemyContact(collision);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            HandleEnemyContact(collision);
        }

        private void HandleEnemyContact(Collision2D collision)
        {
            // Verify if colliding with enemy layer or tag
            if (((1 << collision.gameObject.layer) & enemyLayer) == 0 && !collision.gameObject.CompareTag("Enemy"))
            {
                return;
            }

            Health enemyHealth = collision.gameObject.GetComponentInParent<Health>();
            if (enemyHealth == null || enemyHealth.IsDead) return;

            // Parry / Deflect counter against melee enemy
            if (IsParrying)
            {
                var stunnable = collision.gameObject.GetComponentInParent<Common.IStunnable>();
                if (stunnable != null)
                {
                    stunnable.Stun(1.5f);
                }
                Vector2 parryKnock = ((Vector2)collision.transform.position - (Vector2)transform.position).normalized * enemyKnockbackForce * 1.2f;
                enemyHealth.TakeDamage(attackDamage, parryKnock);
                UI.FloatingTextManager.ShowParry(transform.position);
                CombatJuice.HitStop(0.12f);
                CombatJuice.Shake(0.2f, 0.45f);
                if (AudioManager.Instance != null) AudioManager.Instance.PlaySound("parry");
                if (playerEnergy != null) playerEnergy.AddEnergy(parryEnergyGain);
                ComboRankManager.Instance?.AddCombatAction("Parry", 120);
                AchievementManager.Instance?.Unlock("ACH_PARRY_MASTER");
                return; // Negates all incoming damage!
            }

            // Dashing slice: if dashing through an enemy, pierce and damage enemy instead of taking damage!
            if (playerController != null && playerController.IsDashing)
            {
                Vector2 dashKnock = ((Vector2)collision.transform.position - (Vector2)transform.position).normalized;
                dashKnock.y = Mathf.Max(dashKnock.y, 0.3f);
                enemyHealth.TakeDamage(attackDamage, dashKnock * enemyKnockbackForce);
                UI.FloatingTextManager.ShowDamage(collision.transform.position, attackDamage, true);
                if (playerEnergy != null) playerEnergy.AddEnergy(energyPerDashSlice);
                CombatJuice.DirectionalShake(dashKnock, 0.16f, 0.32f);
                CombatJuice.HitStop(0.08f);
                ComboRankManager.Instance?.AddCombatAction("DashSlice", 75);
                return;
            }

            // Determine if collision landed from above (Head Stomp)
            bool isStomp = false;
            for (int i = 0; i < collision.contactCount; i++)
            {
                ContactPoint2D contact = collision.GetContact(i);
                if (contact.normal.y > 0.5f)
                {
                    isStomp = true;
                    break;
                }
            }

            // Fallback check if player is physically above enemy and descending
            if (!isStomp && rb != null && rb.linearVelocity.y < 0.1f && (transform.position.y - collision.transform.position.y > 0.35f))
            {
                isStomp = true;
            }

            if (isStomp)
            {
                var guardian = collision.gameObject.GetComponentInParent<Enemies.CrystalGuardianMiniBoss>();
                if (guardian != null) guardian.ShatterShield();

                // Defeat or damage enemy via head stomp
                enemyHealth.TakeDamage(stompDamage);
                UI.FloatingTextManager.ShowDamage(collision.transform.position, stompDamage, true);
                CombatJuice.HitStop(0.08f);
                CombatJuice.DirectionalShake(Vector2.down, 0.15f, 0.38f);
                if (playerEnergy != null) playerEnergy.AddEnergy(energyPerStomp);
                ComboRankManager.Instance?.AddCombatAction("Stomp", 80);

                // Rebound player upward using Unity 6 linearVelocity
                if (rb != null)
                {
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, bounceForce);
                }

                // Refresh jump allowances for snappy platforming control
                if (playerController != null)
                {
                    playerController.ResetJumps();
                    playerController.ResetDash();
                }

                OnStomped?.Invoke();
            }
            else
            {
                // Side or upward contact: Player takes damage and receives knockback
                if (playerHealth != null && !playerHealth.IsInvincible && !playerHealth.IsDead)
                {
                    Vector2 knockDir = ((Vector2)transform.position - (Vector2)collision.transform.position).normalized;
                    knockDir.y = Mathf.Max(knockDir.y, 0.45f);
                    playerHealth.TakeDamage(1, knockDir * sideKnockbackForce);
                    UI.FloatingTextManager.ShowPlayerDamage(transform.position, 1);
                    CombatJuice.DirectionalShake(knockDir, 0.2f, 0.45f);
                    ComboRankManager.Instance?.OnPlayerDamaged();
                }
            }
        }

        public void SetEnergySlashCost(float cost)
        {
            energySlashCost = Mathf.Max(5f, cost);
        }

        private bool ReadAttackDown()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.jKey.wasPressedThisFrame) return true;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
            if (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame) return true;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.J) || Input.GetMouseButtonDown(0)) return true;
#endif

            return false;
        }

        private bool ReadRangedAttackDown()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && (Keyboard.current.kKey.wasPressedThisFrame || Keyboard.current.lKey.wasPressedThisFrame || Keyboard.current.uKey.wasPressedThisFrame)) return true;
            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame) return true;
            if (Gamepad.current != null && (Gamepad.current.buttonNorth.wasPressedThisFrame || Gamepad.current.rightTrigger.wasPressedThisFrame)) return true;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.K) || Input.GetKeyDown(KeyCode.L) || Input.GetKeyDown(KeyCode.U) || Input.GetMouseButtonDown(1)) return true;
#endif

            return false;
        }

        private bool ReadParryDown()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && (Keyboard.current.fKey.wasPressedThisFrame || Keyboard.current.leftAltKey.wasPressedThisFrame)) return true;
            if (Gamepad.current != null && (Gamepad.current.leftShoulder.wasPressedThisFrame || Gamepad.current.rightShoulder.wasPressedThisFrame)) return true;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.LeftAlt)) return true;
#endif

            return false;
        }

        private bool ReadUltimateDown()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && (Keyboard.current.rKey.wasPressedThisFrame || Keyboard.current.xKey.wasPressedThisFrame)) return true;
            if (Gamepad.current != null && (Gamepad.current.dpad.up.wasPressedThisFrame || Gamepad.current.rightStickButton.wasPressedThisFrame)) return true;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.X)) return true;
#endif

            return false;
        }

        private float ReadVerticalInput()
        {
            float vert = 0f;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) vert = 1f;
                else if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) vert = -1f;
            }
            if (Gamepad.current != null && Mathf.Approximately(vert, 0f))
            {
                vert = Gamepad.current.leftStick.y.ReadValue();
                if (Gamepad.current.dpad.up.isPressed) vert = 1f;
                else if (Gamepad.current.dpad.down.isPressed) vert = -1f;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Mathf.Approximately(vert, 0f))
            {
                vert = Input.GetAxisRaw("Vertical");
            }
#endif
            return vert;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.magenta;
            bool facingRight = playerController != null ? playerController.IsFacingRight : true;
            Vector2 origin = (Vector2)transform.position + new Vector2(facingRight ? attackOffset.x : -attackOffset.x, attackOffset.y);
            Gizmos.DrawWireSphere(origin, attackRadius);
        }
    }
}

