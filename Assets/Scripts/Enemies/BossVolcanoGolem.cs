using System.Collections;
using UnityEngine;
using Platformer.Common;
using Platformer.Managers;
using Platformer.UI;

namespace Platformer.Enemies
{
    /// <summary>
    /// Volcanic Sky Fortress Boss - "IGNIS, THE VOLCANIC TITAN".
    /// Upgraded AI & Combat Flow:
    /// - Heavy Marching Footsteps: Moves towards player with ground thuds instead of standing frozen.
    /// - Robust Ground Impact: Uses physical ground raycasts and timeout fallbacks (never gets stuck in midair).
    /// - Knockback Immunity: 50-ton volcanic titan resists sword knockback to keep attack trajectories stable.
    /// - 3 Attack Moves:
    ///   1. Seismic Jump Slam: Leaps into air, plummets down with ground shockwaves, followed by vulnerable stun window.
    ///   2. Molten Bull Charge (Phase 2): Stomps, roars, and dashes horizontally across the arena at high speed.
    ///   3. Lava Cataclysm (Phase 2): Summons falling meteors with pulsing ground telegraphs.
    /// - Smooth Enrage & Epic Multi-Burst Defeat sequence.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(Health))]
    public class BossVolcanoGolem : MonoBehaviour
    {
        [Header("Boss Identity")]
        [SerializeField] private string bossTitle = "IGNIS - VOLCANIC TITAN";
        [SerializeField] private int maxHealth = 10;
        [SerializeField] private int enrageHealth = 5;

        [Header("Arena Bounds")]
        [SerializeField] private float arenaMinX = 104f;
        [SerializeField] private float arenaMaxX = 117.5f;
        [SerializeField] private float groundY = 5.0f;
        [SerializeField] private float skyY = 14.0f;

        [Header("Prefabs")]
        [SerializeField] private GameObject shockwavePrefab;
        [SerializeField] private GameObject fireballPrefab;
        [SerializeField] private GameObject telegraphPrefab;
        [SerializeField] private GameObject defeatParticlePrefab;

        [Header("References")]
        [SerializeField] private Health health;
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Collider2D bossCollider;

        private BossArenaController arenaController;
        private BossHealthBarUI healthBarUI;
        private Transform playerTransform;

        private bool isAwakened = false;
        private bool isEnraged = false;
        private bool isPerformingAction = false;
        private Vector3 initialScale;
        private Vector3 initialSpawnPosition;

        private readonly Color normalColor = Color.white;
        private readonly Color enragedColor = new Color(1.0f, 0.42f, 0.35f, 1f);
        private readonly Color vulnerableColor = new Color(1.0f, 0.92f, 0.45f, 1f);

        public Health Health => health;
        public bool IsPerformingAction => isPerformingAction;

        private void Awake()
        {
            if (health == null) health = GetComponent<Health>();
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (bossCollider == null) bossCollider = GetComponent<Collider2D>();

            initialScale = transform.localScale;
            initialSpawnPosition = transform.position;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.mass = 50f;

            // Giant titan ignores knockback impulses so sword attacks don't interrupt its attacks
            if (health != null)
            {
                health.KnockbackMultiplier = 0f;
            }

            // Enemies ignore each other
            Physics2D.IgnoreLayerCollision(gameObject.layer, gameObject.layer, true);
        }

        private void OnEnable()
        {
            if (health != null)
            {
                health.OnDamaged.AddListener(HandleDamaged);
                health.OnDied.AddListener(HandleDeath);
            }
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.OnDamaged.RemoveListener(HandleDamaged);
                health.OnDied.RemoveListener(HandleDeath);
            }
        }

        private void Start()
        {
            if (arenaController == null)
            {
                arenaController = Object.FindFirstObjectByType<BossArenaController>();
            }
            if (healthBarUI == null)
            {
                healthBarUI = Object.FindFirstObjectByType<BossHealthBarUI>();
            }
        }

        private void Update()
        {
            if (!isAwakened)
            {
                LocatePlayer();
                if (playerTransform != null)
                {
                    float dist = Vector2.Distance(transform.position, playerTransform.position);
                    // Awaken if player is within 12 units or crossed threshold into arena (X >= arenaMinX - 1.0f)
                    if (dist <= 12f || playerTransform.position.x >= (arenaMinX - 1.0f))
                    {
                        Awaken();
                    }
                }
            }
        }

        public void SetArenaContext(float minX, float maxX, float gY, BossArenaController controller, BossHealthBarUI barUI)
        {
            arenaMinX = minX;
            arenaMaxX = maxX;
            groundY = gY;
            arenaController = controller;
            healthBarUI = barUI;
        }

        public void SetPrefabs(GameObject shockwave, GameObject fireball, GameObject telegraph, GameObject defeatFx)
        {
            shockwavePrefab = shockwave;
            fireballPrefab = fireball;
            telegraphPrefab = telegraph;
            defeatParticlePrefab = defeatFx;
        }

        /// <summary>
        /// Awakens the boss when player crosses the arena threshold, gets close, or attacks.
        /// </summary>
        public void Awaken()
        {
            if (isAwakened) return;
            isAwakened = true;

            LocatePlayer();

            if (arenaController != null && !arenaController.HasTriggered)
            {
                arenaController.TriggerArena(playerTransform != null ? playerTransform.gameObject : null);
            }

            if (healthBarUI == null)
            {
                healthBarUI = Object.FindFirstObjectByType<BossHealthBarUI>(FindObjectsInactive.Include);
            }

            if (healthBarUI != null)
            {
                healthBarUI.Show(bossTitle, health != null ? health.MaxHealth : maxHealth);
            }

            StartCoroutine(IntroAndCombatLoop());
        }

        private void LocatePlayer()
        {
            if (playerTransform == null)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null) playerTransform = player.transform;
            }
        }

        private IEnumerator IntroAndCombatLoop()
        {
            // Intro Roar & Camera Rumble
            CameraFollow.Shake(0.42f, 0.48f);
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySound("boss_roar");

            yield return new WaitForSeconds(0.8f);

            int attackIndex = 0;

            while (health != null && !health.IsDead)
            {
                // 1. Heavy Marching Footsteps towards Player
                yield return StartCoroutine(MarchTowardsPlayerRoutine(isEnraged ? 1.0f : 1.6f));

                if (health.IsDead) break;

                // 2. Select Attack Move
                if (!isEnraged)
                {
                    // Phase 1: Seismic Jump Slam
                    yield return StartCoroutine(JumpSlamRoutine());
                }
                else
                {
                    // Phase 2: Cycle between Jump Slam, Molten Charge, Lava Rain, and Meteor Shower!
                    int moveType = attackIndex % 4;
                    if (moveType == 0)
                    {
                        yield return StartCoroutine(JumpSlamRoutine());
                    }
                    else if (moveType == 1)
                    {
                        yield return StartCoroutine(MoltenChargeRoutine());
                    }
                    else if (moveType == 2)
                    {
                        yield return StartCoroutine(LavaRainRoutine());
                    }
                    else
                    {
                        yield return StartCoroutine(MeteorShowerRoutine());
                    }
                }

                attackIndex++;

                // Short rest between actions
                yield return new WaitForSeconds(isEnraged ? 0.4f : 0.7f);
            }
        }

        /// <summary>
        /// Boss marches heavily along the ground towards the player, shaking the screen on each step.
        /// </summary>
        private IEnumerator MarchTowardsPlayerRoutine(float duration)
        {
            isPerformingAction = true;
            LocatePlayer();

            float elapsed = 0f;
            float stepTimer = 0f;
            float walkSpeed = isEnraged ? 3.4f : 2.3f;

            while (elapsed < duration)
            {
                if (health != null && health.IsDead) yield break;

                elapsed += Time.deltaTime;
                stepTimer += Time.deltaTime;

                LocatePlayer();
                float dx = playerTransform != null ? (playerTransform.position.x - transform.position.x) : 0f;
                float dir = Mathf.Sign(dx);

                // Face the player
                FaceDirection(dir);

                // If close enough to player, transition directly to attack
                if (Mathf.Abs(dx) < 2.2f)
                {
                    break;
                }

                // Check arena boundary bounds
                float nextX = transform.position.x + dir * (walkSpeed * Time.deltaTime);
                if (nextX >= arenaMinX + 1.0f && nextX <= arenaMaxX - 1.0f)
                {
                    rb.linearVelocity = new Vector2(dir * walkSpeed, rb.linearVelocity.y);
                }
                else
                {
                    rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                }

                // Footstep impact feedback
                if (stepTimer >= 0.45f)
                {
                    stepTimer = 0f;
                    CameraFollow.Shake(0.04f, 0.07f);
                }

                // Slight walking bob
                float bob = Mathf.Sin(elapsed * 12f) * 0.06f;
                transform.localScale = new Vector3(transform.localScale.x, initialScale.y + bob, initialScale.z);

                yield return null;
            }

            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            transform.localScale = new Vector3(transform.localScale.x, initialScale.y, initialScale.z);
            isPerformingAction = false;
        }

        private void FaceDirection(float dir)
        {
            // By convention, scale.x > 0 faces right, scale.x < 0 faces left
            float sign = dir >= 0f ? 1f : -1f;
            transform.localScale = new Vector3(Mathf.Abs(initialScale.x) * sign, transform.localScale.y, initialScale.z);
        }

        private bool CheckGrounded()
        {
            if (bossCollider == null) return true;
            Vector2 feet = new Vector2(bossCollider.bounds.center.x, bossCollider.bounds.min.y);
            int groundMask = LayerMask.GetMask("Ground");
            if (groundMask == 0) groundMask = 1 << 6;

            RaycastHit2D hit = Physics2D.Raycast(feet + Vector2.up * 0.15f, Vector2.down, 0.35f, groundMask);
            return hit.collider != null;
        }

        /// <summary>
        /// Move 1: Anticipates, leaps high towards player X, plummets down with shockwaves, and enters vulnerable recovery.
        /// </summary>
        private IEnumerator JumpSlamRoutine()
        {
            isPerformingAction = true;
            LocatePlayer();

            // 1. Anticipation Squat
            float squishDuration = 0.35f;
            float elapsed = 0f;
            Vector3 squatScale = new Vector3(transform.localScale.x * 1.25f, initialScale.y * 0.72f, initialScale.z);
            Vector3 standScale = transform.localScale;

            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(1f, 0.6f, 0.2f, 1f);
            }

            while (elapsed < squishDuration)
            {
                elapsed += Time.deltaTime;
                transform.localScale = Vector3.Lerp(standScale, squatScale, elapsed / squishDuration);
                yield return null;
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.color = isEnraged ? enragedColor : normalColor;
            }

            // 2. Leap towards player X
            transform.localScale = standScale;
            float targetX = playerTransform != null ? playerTransform.position.x : transform.position.x;
            targetX = Mathf.Clamp(targetX, arenaMinX + 1.5f, arenaMaxX - 1.5f);

            float jumpDuration = 0.5f;
            float vx = (targetX - transform.position.x) / jumpDuration;
            float vy = 14.5f;

            rb.linearVelocity = new Vector2(vx, vy);

            // Wait until ascending towards peak
            float ascendTimer = 0f;
            while (ascendTimer < jumpDuration && rb.linearVelocity.y > 0.5f)
            {
                ascendTimer += Time.deltaTime;
                yield return null;
            }

            // 3. Apex Hover Pause
            rb.linearVelocity = Vector2.zero;
            yield return new WaitForSeconds(0.12f);

            // 4. Downward Plummet
            rb.linearVelocity = new Vector2(0f, -24f);

            // Wait until grounded with safe timeout to prevent any infinite freeze
            float fallTimeout = 0f;
            yield return new WaitForSeconds(0.08f); // Allow downward motion to initiate

            while (fallTimeout < 1.1f)
            {
                fallTimeout += Time.deltaTime;

                if (CheckGrounded() || Mathf.Abs(rb.linearVelocity.y) < 0.2f)
                {
                    break;
                }
                yield return null;
            }

            // 5. Ground Impact!
            rb.linearVelocity = Vector2.zero;

            CameraFollow.Shake(0.4f, 0.52f);
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySound("slam");

            // Spawn Shockwaves at actual feet level
            SpawnShockwaves();

            // 6. Stunned / Vulnerable Recovery Window
            if (spriteRenderer != null)
            {
                spriteRenderer.color = vulnerableColor;
            }

            float stunDuration = isEnraged ? 0.85f : 1.4f;
            yield return new WaitForSeconds(stunDuration);

            // Restore normal appearance
            if (spriteRenderer != null)
            {
                spriteRenderer.color = isEnraged ? enragedColor : normalColor;
            }

            isPerformingAction = false;
        }

        private void SpawnShockwaves()
        {
            if (shockwavePrefab == null) return;

            float feetY = bossCollider != null ? bossCollider.bounds.min.y + 0.25f : groundY + 0.35f;

            // Left shockwave
            GameObject swLeft = Instantiate(shockwavePrefab, new Vector3(transform.position.x - 0.85f, feetY, 0f), Quaternion.identity);
            var leftScript = swLeft.GetComponent<BossShockwave>();
            if (leftScript != null) leftScript.Initialize(-1f);

            // Right shockwave
            GameObject swRight = Instantiate(shockwavePrefab, new Vector3(transform.position.x + 0.85f, feetY, 0f), Quaternion.identity);
            var rightScript = swRight.GetComponent<BossShockwave>();
            if (rightScript != null) rightScript.Initialize(1f);
        }

        /// <summary>
        /// Move 2 (Phase 2): Stomps, roars, and charges horizontally across the arena at high speed!
        /// </summary>
        private IEnumerator MoltenChargeRoutine()
        {
            isPerformingAction = true;
            LocatePlayer();

            float dx = playerTransform != null ? (playerTransform.position.x - transform.position.x) : 1f;
            float chargeDir = Mathf.Sign(dx);
            FaceDirection(chargeDir);

            // Telegraph: Roar & scrape ground
            CameraFollow.Shake(0.28f, 0.35f);
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySound("boss_roar");

            if (spriteRenderer != null) spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.45f);
            if (spriteRenderer != null) spriteRenderer.color = enragedColor;

            // Charge forward at high speed
            float chargeSpeed = 13.0f;
            float chargeTime = 0.85f;
            float elapsed = 0f;

            while (elapsed < chargeTime)
            {
                if (health != null && health.IsDead) yield break;

                elapsed += Time.deltaTime;

                float nextX = transform.position.x + chargeDir * (chargeSpeed * Time.deltaTime);

                // Stop if hitting arena edges
                if (nextX <= arenaMinX + 1.2f || nextX >= arenaMaxX - 1.2f)
                {
                    CameraFollow.Shake(0.3f, 0.38f);
                    if (AudioManager.Instance != null) AudioManager.Instance.PlaySound("hit");
                    break;
                }

                rb.linearVelocity = new Vector2(chargeDir * chargeSpeed, rb.linearVelocity.y);
                yield return null;
            }

            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

            // Brief skid recovery
            yield return new WaitForSeconds(0.65f);
            isPerformingAction = false;
        }

        /// <summary>
        /// Move 3 (Phase 2): Summons 3 falling volcanic fireballs with pulsing red ground telegraphs.
        /// </summary>
        private IEnumerator LavaRainRoutine()
        {
            isPerformingAction = true;

            // Roar & telegraph
            CameraFollow.Shake(0.32f, 0.38f);
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySound("boss_roar");

            if (spriteRenderer != null) spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.4f);
            if (spriteRenderer != null) spriteRenderer.color = isEnraged ? enragedColor : normalColor;

            // Select 3 drop coordinates across arena
            LocatePlayer();
            float pX = playerTransform != null ? playerTransform.position.x : (arenaMinX + arenaMaxX) * 0.5f;
            pX = Mathf.Clamp(pX, arenaMinX + 1.2f, arenaMaxX - 1.2f);

            float rX1 = Mathf.Clamp(pX + Random.Range(-4.5f, -1.8f), arenaMinX + 1.2f, arenaMaxX - 1.2f);
            float rX2 = Mathf.Clamp(pX + Random.Range(1.8f, 4.5f), arenaMinX + 1.2f, arenaMaxX - 1.2f);

            float[] targets = new float[] { pX, rX1, rX2 };

            // Spawn ground telegraphs
            if (telegraphPrefab != null)
            {
                foreach (float x in targets)
                {
                    Instantiate(telegraphPrefab, new Vector3(x, groundY + 0.15f, 0f), Quaternion.identity);
                }
            }

            // Telegraph duration before fireballs fall
            yield return new WaitForSeconds(0.85f);

            // Drop fireballs
            if (fireballPrefab != null)
            {
                foreach (float x in targets)
                {
                    GameObject fb = Instantiate(fireballPrefab, new Vector3(x, skyY, 0f), Quaternion.identity);
                    var fbScript = fb.GetComponent<BossFireball>();
                    if (fbScript != null)
                    {
                        fbScript.Initialize(groundY + 0.3f, defeatParticlePrefab);
                    }
                }
            }

            yield return new WaitForSeconds(0.5f);
            isPerformingAction = false;
        }

        /// <summary>
        /// Move 4 (Ultimate Phase 2): Summons a massive Meteor Shower raining down across the entire arena!
        /// </summary>
        private IEnumerator MeteorShowerRoutine()
        {
            isPerformingAction = true;

            // Roar & Seismic Ground Thud
            CameraFollow.Shake(0.5f, 0.58f);
            if (AudioManager.Instance != null) AudioManager.Instance.PlaySound("boss_roar");

            if (spriteRenderer != null) spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.45f);
            if (spriteRenderer != null) spriteRenderer.color = enragedColor;

            // 5 staggered targets across the entire arena floor
            int meteorCount = 5;
            float arenaWidth = (arenaMaxX - arenaMinX) - 2.4f;
            float interval = arenaWidth / (meteorCount - 1);
            float[] dropXs = new float[meteorCount];

            for (int i = 0; i < meteorCount; i++)
            {
                dropXs[i] = Mathf.Clamp(arenaMinX + 1.2f + i * interval + Random.Range(-0.5f, 0.5f), arenaMinX + 1.2f, arenaMaxX - 1.2f);
            }

            // Spawn warning telegraphs on the floor
            if (telegraphPrefab != null)
            {
                foreach (float x in dropXs)
                {
                    Instantiate(telegraphPrefab, new Vector3(x, groundY + 0.15f, 0f), Quaternion.identity);
                }
            }

            // Telegraph build-up time
            yield return new WaitForSeconds(0.75f);

            // Cascade meteors down from the ceiling with slight delays
            if (fireballPrefab != null)
            {
                foreach (float x in dropXs)
                {
                    if (health != null && health.IsDead) yield break;

                    GameObject fb = Instantiate(fireballPrefab, new Vector3(x, skyY + Random.Range(0f, 1.5f), 0f), Quaternion.identity);
                    var fbScript = fb.GetComponent<BossFireball>();
                    if (fbScript != null)
                    {
                        fbScript.Initialize(groundY + 0.3f, defeatParticlePrefab);
                    }

                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.PlaySound("meteor_impact", 0.7f);
                    }

                    yield return new WaitForSeconds(0.18f);
                }
            }

            yield return new WaitForSeconds(0.6f);
            isPerformingAction = false;
        }

        private void HandleDamaged(int currentHp)
        {
            // If damaged before awakening, immediately awaken
            if (!isAwakened)
            {
                Awaken();
            }

            if (healthBarUI != null)
            {
                healthBarUI.UpdateHealth(currentHp, health.MaxHealth, isEnraged);
            }

            // Phase 2 Enrage Transition
            if (!isEnraged && currentHp <= enrageHealth && currentHp > 0)
            {
                TriggerEnrage();
            }
        }

        private void TriggerEnrage()
        {
            isEnraged = true;

            if (spriteRenderer != null)
            {
                spriteRenderer.color = enragedColor;
            }

            CombatJuice.HitStop(0.12f);
            CameraFollow.Shake(0.52f, 0.65f);
            UI.FloatingTextManager.ShowDamage(transform.position, 0, true);

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound("boss_roar");
            }
        }

        private void HandleDeath()
        {
            StopAllCoroutines();

            if (bossCollider != null) bossCollider.enabled = false;
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.bodyType = RigidbodyType2D.Kinematic;
            }

            // High-impact cinematic finisher: bullet-time slow-mo + camera zoom-in
            CombatJuice.SlowMotion(0.22f, 1.8f);
            CombatJuice.CinematicZoom(5.2f, 0.45f, 1.2f);
            CameraFollow.Shake(0.85f, 0.9f);
            AchievementManager.Instance?.Unlock("ACH_TITAN_SLAYER");

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound("boss_defeat");
            }

            if (healthBarUI != null)
            {
                healthBarUI.Hide();
            }

            StartCoroutine(DefeatSequence());
        }

        private IEnumerator DefeatSequence()
        {
            // Spawn multiple defeat bursts
            if (defeatParticlePrefab != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    Vector3 offset = new Vector3(Random.Range(-0.8f, 0.8f), Random.Range(-0.5f, 1.2f), 0f);
                    Instantiate(defeatParticlePrefab, transform.position + offset, Quaternion.identity);
                    yield return new WaitForSeconds(0.25f);
                }
            }
            else
            {
                yield return new WaitForSeconds(1.0f);
            }

            // Fade out sprite
            float elapsed = 0f;
            Color startColor = spriteRenderer != null ? spriteRenderer.color : Color.white;

            while (elapsed < 1.2f)
            {
                elapsed += Time.deltaTime;
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, 1f - (elapsed / 1.2f));
                }
                yield return null;
            }

            // Notify arena controller to open gate and unlock goal
            if (arenaController == null)
            {
                arenaController = Object.FindFirstObjectByType<BossArenaController>();
            }

            if (arenaController != null)
            {
                arenaController.OnBossDefeated();
            }
            else
            {
                // Fallback: directly find and activate LevelGoal if arenaController missing
                var goal = Object.FindFirstObjectByType<LevelGoal>(FindObjectsInactive.Include);
                if (goal != null)
                {
                    goal.transform.position = new Vector3(113.5f, groundY, 0f);
                    goal.gameObject.SetActive(true);
                }
            }

            Destroy(gameObject);
        }

        /// <summary>
        /// Resets the boss state, health, position, and active projectiles when player dies in the arena.
        /// </summary>
        public void ResetBoss()
        {
            StopAllCoroutines();

            // Clear stray projectiles from active battle
            var shockwaves = Object.FindObjectsByType<BossShockwave>(FindObjectsSortMode.None);
            foreach (var sw in shockwaves) if (sw != null) Destroy(sw.gameObject);
            var fireballs = Object.FindObjectsByType<BossFireball>(FindObjectsSortMode.None);
            foreach (var fb in fireballs) if (fb != null) Destroy(fb.gameObject);
            var telegraphs = Object.FindObjectsByType<BossTelegraph>(FindObjectsSortMode.None);
            foreach (var tg in telegraphs) if (tg != null) Destroy(tg.gameObject);

            // Restore position & physics
            transform.position = initialSpawnPosition;
            if (rb != null)
            {
                rb.position = initialSpawnPosition;
                rb.linearVelocity = Vector2.zero;
                rb.bodyType = RigidbodyType2D.Dynamic;
            }
            Physics2D.SyncTransforms();

            // Restore visual & state
            transform.localScale = initialScale;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = normalColor;
            }
            if (bossCollider != null)
            {
                bossCollider.enabled = true;
            }

            isAwakened = false;
            isEnraged = false;
            isPerformingAction = false;

            if (health != null)
            {
                health.ResetHealth();
            }

            if (healthBarUI != null)
            {
                healthBarUI.HideInstant();
            }
        }
    }
}
