using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Platformer.Player
{
    /// <summary>
    /// Handles 2D platformer player movement, variable-height jumping, coyote time,
    /// jump buffering, double jumping, and sprite facing.
    /// Strictly uses modern Unity 6 APIs (e.g. Rigidbody2D.linearVelocity).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("Maximum horizontal running speed.")]
        [SerializeField] private float moveSpeed = 8.5f;

        [Tooltip("Acceleration rate when moving towards target speed.")]
        [SerializeField] private float acceleration = 65f;

        [Tooltip("Deceleration rate when stopping or changing directions.")]
        [SerializeField] private float deceleration = 65f;

        [Header("Jump Settings")]
        [Tooltip("Initial upward impulse force for jumping.")]
        [SerializeField] private float jumpForce = 14.5f;

        [Tooltip("Multiplier applied to upward velocity when jump key is released early.")]
        [Range(0f, 1f)]
        [SerializeField] private float jumpCutMultiplier = 0.5f;

        [Tooltip("Grace period (seconds) to jump after leaving a ledge.")]
        [SerializeField] private float coyoteTime = 0.1f;

        [Tooltip("Grace period (seconds) to buffer jump input before hitting the ground.")]
        [SerializeField] private float jumpBufferTime = 0.1f;

        [Tooltip("Whether the player can perform an additional jump mid-air.")]
        [SerializeField] private bool allowDoubleJump = true;

        [Header("Dash Settings")]
        [Tooltip("Whether the player is allowed to perform a quick dash.")]
        [SerializeField] private bool allowDash = true;

        [Tooltip("Impulse speed while dashing.")]
        [SerializeField] private float dashSpeed = 22f;

        [Tooltip("Duration of dash state in seconds.")]
        [SerializeField] private float dashDuration = 0.16f;

        [Tooltip("Cooldown period before dash can be executed again.")]
        [SerializeField] private float dashCooldown = 0.5f;

        [Tooltip("Whether an additional air dash is allowed before touching ground again.")]
        [SerializeField] private bool allowAirDash = true;

        [Header("Ledge Grab & Clamber")]
        [Tooltip("Whether the player can grab platform ledges and clamber up.")]
        [SerializeField] private bool allowLedgeGrab = true;

        [Tooltip("Forward reach distance to detect platform ledges.")]
        [SerializeField] private float ledgeRayLength = 0.55f;

        [Header("Wall Slide & Wall Jump")]
        [Tooltip("Whether the player can slide down walls and jump off them.")]
        [SerializeField] private bool allowWallSlide = true;
        [SerializeField] private float wallSlideSpeed = 2.8f;
        [SerializeField] private Vector2 wallJumpForce = new Vector2(9.5f, 13.5f);

        [Header("Ground Detection")]
        [Tooltip("Offset relative to player position for OverlapBox ground check.")]
        [SerializeField] private Vector2 groundCheckOffset = new Vector2(0f, -0.73f);

        [Tooltip("Dimensions of the OverlapBox ground check.")]
        [SerializeField] private Vector2 groundCheckSize = new Vector2(0.7f, 0.15f);

        [Tooltip("LayerMask specifying what qualifies as solid ground.")]
        [SerializeField] private LayerMask groundLayer;

        [Header("References")]
        [SerializeField] private Rigidbody2D rb;

        // Runtime state variables
        private float horizontalInput;
        private bool isGrounded;
        private bool wasGroundedLastFrame;
        private bool isFacingRight = true;
        private bool canDoubleJump;
        private float coyoteTimer;
        private float jumpBufferTimer;

        // Dash runtime state
        private bool isDashing;
        private bool canAirDash;
        private float dashTimer;
        private float dashCooldownTimer;
        private float originalGravity = 3.5f;
        private float dashDirection = 1f;

        // Ledge Grab runtime state
        private bool isLedgeGrabbing;
        private Vector2 ledgeCornerPos;
        private float ledgeGrabCooldownTimer;
        private float ledgeHangTimer;

        // Wall Slide runtime state
        private bool isWallSliding;
        private float wallJumpLockoutTimer;

        public bool IsFacingRight => isFacingRight;
        public bool IsGrounded => isGrounded;
        public bool IsDashing => isDashing;
        public bool IsLedgeGrabbing => isLedgeGrabbing;
        public bool IsWallSliding => isWallSliding;

        public event System.Action OnJumped;
        public event System.Action OnLanded;
        public event System.Action OnDashed;
        public event System.Action OnLedgeGrabbed;
        public event System.Action OnLedgeClambered;

        /// <summary>
        /// Resets dash availability (e.g. upon landing or stomping an enemy).
        /// </summary>
        public void ResetDash()
        {
            canAirDash = allowAirDash;
        }

        /// <summary>
        /// Resets double jump, coyote timer, and dash (e.g. after stomping an enemy).
        /// </summary>
        public void ResetJumps()
        {
            canDoubleJump = allowDoubleJump;
            coyoteTimer = coyoteTime;
            canAirDash = allowAirDash;
        }

        public void SetDashCooldown(float cooldown)
        {
            dashCooldown = Mathf.Max(0.1f, cooldown);
        }

        private void OnDisable()
        {
            if (isDashing)
            {
                EndDash();
            }
            if (isLedgeGrabbing)
            {
                isLedgeGrabbing = false;
                if (rb != null) rb.gravityScale = originalGravity;
            }
            isWallSliding = false;
            if (rb != null)
            {
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            }
            horizontalInput = 0f;
        }

        private void Reset()
        {
            rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                rb.gravityScale = 3.5f;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            }
        }

        private void Awake()
        {
            if (rb == null)
            {
                rb = GetComponent<Rigidbody2D>();
            }

            // Ensure rotation is locked to prevent unwanted tilting
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            if (GetComponent<ShadowDashEffect>() == null)
            {
                gameObject.AddComponent<ShadowDashEffect>();
            }
            originalGravity = rb.gravityScale;
        }

        private void Update()
        {
            // 0. Update dash and ledge timers
            if (dashCooldownTimer > 0f)
            {
                dashCooldownTimer -= Time.deltaTime;
            }

            if (ledgeGrabCooldownTimer > 0f)
            {
                ledgeGrabCooldownTimer -= Time.deltaTime;
            }

            // Handle active Ledge Grab hanging state
            if (isLedgeGrabbing)
            {
                ledgeHangTimer -= Time.deltaTime;
                float vert = ReadVerticalInput();
                float horiz = ReadHorizontalInput();
                bool towardsWall = isFacingRight ? (horiz > 0.3f) : (horiz < -0.3f);

                // Pull up / Clamber onto ledge
                if (ReadJumpDown() || vert > 0.3f || towardsWall)
                {
                    EndLedgeGrab(clamber: true);
                    return;
                }

                // Drop down from ledge
                if (vert < -0.3f || ledgeHangTimer <= 0f)
                {
                    EndLedgeGrab(clamber: false);
                    return;
                }

                return;
            }

            if (isDashing)
            {
                dashTimer -= Time.deltaTime;
                if (dashTimer <= 0f)
                {
                    EndDash();
                }
                return;
            }

            // 1. Read input
            horizontalInput = ReadHorizontalInput();

            // Dash execution
            if (ReadDashDown() && dashCooldownTimer <= 0f)
            {
                if (isGrounded || canAirDash)
                {
                    if (!isGrounded)
                    {
                        canAirDash = false;
                    }
                    StartDash();
                    return;
                }
            }

            if (ReadJumpDown())
            {
                jumpBufferTimer = jumpBufferTime;
            }
            else
            {
                jumpBufferTimer -= Time.deltaTime;
            }

            // 2. Perform ground check
            Vector2 checkCenter = (Vector2)transform.position + groundCheckOffset;
            isGrounded = Physics2D.OverlapBox(checkCenter, groundCheckSize, 0f, groundLayer) != null;

            if (!wasGroundedLastFrame && isGrounded)
            {
                OnLanded?.Invoke();
            }
            wasGroundedLastFrame = isGrounded;

            // 3. Update coyote time and double jump / air dash availability
            if (isGrounded)
            {
                coyoteTimer = coyoteTime;
                canDoubleJump = allowDoubleJump;
                canAirDash = allowAirDash;
                isWallSliding = false;
            }
            else
            {
                coyoteTimer -= Time.deltaTime;

                // Check for ledge grab when airborne and near a platform corner
                CheckLedgeGrab();

                // Check for wall slide when airborne against a wall
                CheckWallSlide();
            }

            // 4. Handle jump execution with buffer and coyote time
            if (jumpBufferTimer > 0f)
            {
                if (isWallSliding)
                {
                    ExecuteWallJump();
                    jumpBufferTimer = 0f;
                }
                else if (coyoteTimer > 0f)
                {
                    ExecuteJump();
                    jumpBufferTimer = 0f;
                    coyoteTimer = 0f;
                }
                else if (allowDoubleJump && canDoubleJump)
                {
                    ExecuteJump();
                    jumpBufferTimer = 0f;
                    canDoubleJump = false;
                }
            }

            // 5. Variable jump height: cut jump velocity when released early
            if (ReadJumpUp() && rb.linearVelocity.y > 0f)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
            }

            // 6. Corner correction when jumping upwards near ceiling edges
            CheckCornerCorrection();

            // 7. Handle sprite facing direction
            if (horizontalInput > 0.05f && !isFacingRight)
            {
                Flip();
            }
            else if (horizontalInput < -0.05f && isFacingRight)
            {
                Flip();
            }
        }

        private void FixedUpdate()
        {
            if (isDashing)
            {
                rb.linearVelocity = new Vector2(dashDirection * dashSpeed, 0f);
                return;
            }

            if (wallJumpLockoutTimer > 0f)
            {
                wallJumpLockoutTimer -= Time.fixedDeltaTime;
                return;
            }

            // Calculate target horizontal speed
            float targetSpeed = horizontalInput * moveSpeed;
            float accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? acceleration : deceleration;

            // Smoothly accelerate towards target velocity using modern Unity 6 linearVelocity
            float newVelocityX = Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, accelRate * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector2(newVelocityX, rb.linearVelocity.y);
        }

        private void StartDash()
        {
            isDashing = true;
            dashTimer = dashDuration;
            dashCooldownTimer = dashCooldown;

            // Determine dash direction from horizontal input or current facing direction
            if (Mathf.Abs(horizontalInput) > 0.1f)
            {
                dashDirection = Mathf.Sign(horizontalInput);
            }
            else
            {
                dashDirection = isFacingRight ? 1f : -1f;
            }

            if (dashDirection > 0f && !isFacingRight) Flip();
            else if (dashDirection < 0f && isFacingRight) Flip();

            rb.gravityScale = 0f;
            rb.linearVelocity = new Vector2(dashDirection * dashSpeed, 0f);

            OnDashed?.Invoke();
        }

        private void EndDash()
        {
            isDashing = false;
            if (rb != null)
            {
                rb.gravityScale = originalGravity;
                rb.linearVelocity = new Vector2(dashDirection * moveSpeed, 0f);
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (isDashing)
            {
                for (int i = 0; i < collision.contactCount; i++)
                {
                    if (Mathf.Abs(collision.GetContact(i).normal.x) > 0.7f)
                    {
                        EndDash();
                        break;
                    }
                }
            }
        }

        private void ExecuteJump()
        {
            // Reset vertical velocity before impulse to ensure consistent jump height
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            OnJumped?.Invoke();
        }

        private void Flip()
        {
            isFacingRight = !isFacingRight;
            Vector3 scale = transform.localScale;
            scale.x *= -1f;
            transform.localScale = scale;
        }

        /// <summary>
        /// Reads horizontal input supporting both the New Input System and Legacy fallback.
        /// </summary>
        private float ReadHorizontalInput()
        {
            float horizontal = 0f;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) horizontal -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) horizontal += 1f;
            }

            if (Gamepad.current != null)
            {
                float stick = Gamepad.current.leftStick.x.ReadValue();
                if (Mathf.Abs(stick) > 0.15f) horizontal = stick;
                if (Gamepad.current.dpad.left.isPressed) horizontal = -1f;
                if (Gamepad.current.dpad.right.isPressed) horizontal = 1f;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Mathf.Approximately(horizontal, 0f))
            {
                horizontal = Input.GetAxisRaw("Horizontal");
            }
#endif

            return Mathf.Clamp(horizontal, -1f, 1f);
        }

        /// <summary>
        /// Detects if jump button was pressed down this frame.
        /// </summary>
        private bool ReadJumpDown()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.spaceKey.wasPressedThisFrame ||
                    Keyboard.current.wKey.wasPressedThisFrame ||
                    Keyboard.current.upArrowKey.wasPressedThisFrame)
                {
                    return true;
                }
            }

            if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
            {
                return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            {
                return true;
            }
#endif

            return false;
        }

        /// <summary>
        /// Detects if jump button was released this frame.
        /// </summary>
        private bool ReadJumpUp()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.spaceKey.wasReleasedThisFrame ||
                    Keyboard.current.wKey.wasReleasedThisFrame ||
                    Keyboard.current.upArrowKey.wasReleasedThisFrame)
                {
                    return true;
                }
            }

            if (Gamepad.current != null && Gamepad.current.buttonSouth.wasReleasedThisFrame)
            {
                return true;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetButtonUp("Jump") || Input.GetKeyUp(KeyCode.Space) || Input.GetKeyUp(KeyCode.W) || Input.GetKeyUp(KeyCode.UpArrow))
            {
                return true;
            }
#endif

            return false;
        }

        /// <summary>
        /// Detects if dash button was pressed down this frame.
        /// Supports LeftShift, RightShift, Q, Gamepad East / Triggers.
        /// </summary>
        private bool ReadDashDown()
        {
            if (!allowDash) return false;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.leftShiftKey.wasPressedThisFrame ||
                    Keyboard.current.rightShiftKey.wasPressedThisFrame ||
                    Keyboard.current.qKey.wasPressedThisFrame)
                {
                    return true;
                }
            }

            if (Gamepad.current != null)
            {
                if (Gamepad.current.buttonEast.wasPressedThisFrame ||
                    Gamepad.current.rightTrigger.wasPressedThisFrame ||
                    Gamepad.current.leftTrigger.wasPressedThisFrame)
                {
                    return true;
                }
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift) || Input.GetKeyDown(KeyCode.Q))
            {
                return true;
            }
#endif

            return false;
        }

        private float ReadVerticalInput()
        {
            float vertical = 0f;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) vertical += 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) vertical -= 1f;
            }

            if (Gamepad.current != null)
            {
                float stick = Gamepad.current.leftStick.y.ReadValue();
                if (Mathf.Abs(stick) > 0.15f) vertical = stick;
                if (Gamepad.current.dpad.up.isPressed) vertical = 1f;
                if (Gamepad.current.dpad.down.isPressed) vertical = -1f;
            }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Mathf.Approximately(vertical, 0f))
            {
                vertical = Input.GetAxisRaw("Vertical");
            }
#endif

            return Mathf.Clamp(vertical, -1f, 1f);
        }

        private void CheckLedgeGrab()
        {
            if (!allowLedgeGrab || isGrounded || isDashing || ledgeGrabCooldownTimer > 0f) return;

            // Player must be falling or at apex
            if (rb.linearVelocity.y > 1.5f) return;

            float facingSign = isFacingRight ? 1f : -1f;
            Vector2 waistPos = (Vector2)transform.position + new Vector2(0f, -0.1f);
            Vector2 headPos = (Vector2)transform.position + new Vector2(0f, 0.55f);

            // Raycast forward from waist and head
            RaycastHit2D waistHit = Physics2D.Raycast(waistPos, Vector2.right * facingSign, ledgeRayLength, groundLayer);
            RaycastHit2D headHit = Physics2D.Raycast(headPos, Vector2.right * facingSign, ledgeRayLength, groundLayer);

            // If waist hits wall but head is completely unobstructed -> Ledge detected!
            if (waistHit.collider != null && headHit.collider == null)
            {
                Vector2 topDownOrigin = headPos + new Vector2(facingSign * (waistHit.distance + 0.1f), 0.35f);
                RaycastHit2D surfaceHit = Physics2D.Raycast(topDownOrigin, Vector2.down, 0.9f, groundLayer);

                if (surfaceHit.collider != null)
                {
                    ledgeCornerPos = new Vector2(waistHit.point.x, surfaceHit.point.y);
                    StartLedgeGrab();
                }
            }
        }

        private void StartLedgeGrab()
        {
            isLedgeGrabbing = true;
            ledgeHangTimer = 1.8f;
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0f;

            // Snap player so hands align cleanly with the ledge corner
            float facingSign = isFacingRight ? 1f : -1f;
            transform.position = new Vector3(ledgeCornerPos.x - facingSign * 0.35f, ledgeCornerPos.y - 0.52f, transform.position.z);

            ResetJumps();
            ResetDash();

            if (Managers.AudioManager.Instance != null)
            {
                Managers.AudioManager.Instance.PlaySound("clamber", 0.7f);
            }

            OnLedgeGrabbed?.Invoke();
        }

        private void EndLedgeGrab(bool clamber)
        {
            isLedgeGrabbing = false;
            rb.gravityScale = originalGravity;
            ledgeGrabCooldownTimer = 0.35f;

            float facingSign = isFacingRight ? 1f : -1f;
            if (clamber)
            {
                // Pull up smoothly onto the top surface of the ledge
                transform.position = new Vector3(ledgeCornerPos.x + facingSign * 0.35f, ledgeCornerPos.y + 0.8f, transform.position.z);
                rb.linearVelocity = new Vector2(facingSign * 2.5f, 3.5f);

                if (Managers.AudioManager.Instance != null)
                {
                    Managers.AudioManager.Instance.PlaySound("jump", 0.65f);
                }

                OnLedgeClambered?.Invoke();
            }
            else
            {
                // Drop down
                rb.linearVelocity = new Vector2(0f, -1.5f);
            }
        }

        private void CheckWallSlide()
        {
            if (!allowWallSlide || isGrounded || isLedgeGrabbing || isDashing)
            {
                isWallSliding = false;
                return;
            }

            float facingSign = isFacingRight ? 1f : -1f;
            float horiz = ReadHorizontalInput();
            bool pushingTowardsWall = isFacingRight ? (horiz > 0.1f) : (horiz < -0.1f);

            Vector2 waistPos = (Vector2)transform.position + new Vector2(0f, -0.1f);
            RaycastHit2D wallHit = Physics2D.Raycast(waistPos, Vector2.right * facingSign, 0.48f, groundLayer);

            if (wallHit.collider != null && pushingTowardsWall && rb.linearVelocity.y < 0.2f)
            {
                isWallSliding = true;
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, -wallSlideSpeed));
            }
            else
            {
                isWallSliding = false;
            }
        }

        private void ExecuteWallJump()
        {
            float jumpAwayDir = isFacingRight ? -1f : 1f;
            rb.linearVelocity = new Vector2(jumpAwayDir * wallJumpForce.x, wallJumpForce.y);
            wallJumpLockoutTimer = 0.15f;
            isWallSliding = false;
            Flip();
            canDoubleJump = true;
            canAirDash = true;

            if (Managers.AudioManager.Instance != null)
            {
                Managers.AudioManager.Instance.PlaySound("jump");
            }
            OnJumped?.Invoke();
        }

        private void CheckCornerCorrection()
        {
            if (rb.linearVelocity.y <= 0.5f || isGrounded) return;

            Vector2 center = (Vector2)transform.position + new Vector2(0f, 0.65f);
            float cornerOffset = 0.32f;
            float checkDist = 0.25f;

            RaycastHit2D leftHit = Physics2D.Raycast(center + Vector2.left * cornerOffset, Vector2.up, checkDist, groundLayer);
            RaycastHit2D rightHit = Physics2D.Raycast(center + Vector2.right * cornerOffset, Vector2.up, checkDist, groundLayer);
            RaycastHit2D centerHit = Physics2D.Raycast(center, Vector2.up, checkDist, groundLayer);

            // If right corner catches ceiling while center and left are clear -> nudge left
            if (rightHit.collider != null && centerHit.collider == null && leftHit.collider == null)
            {
                transform.position += Vector3.left * (Time.deltaTime * 3.5f);
            }
            // If left corner catches ceiling while center and right are clear -> nudge right
            else if (leftHit.collider != null && centerHit.collider == null && rightHit.collider == null)
            {
                transform.position += Vector3.right * (Time.deltaTime * 3.5f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Green wireframe when grounded, red when airborne
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Vector2 checkCenter = (Vector2)transform.position + groundCheckOffset;
            Gizmos.DrawWireCube(checkCenter, groundCheckSize);
        }
    }
}

