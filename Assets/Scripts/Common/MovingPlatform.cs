using UnityEngine;

namespace Platformer.Common
{
    /// <summary>
    /// Kinematic moving platform that oscillates between start position and target offset.
    /// Smoothly transports the player by parenting on top-contact and unparenting on exit.
    /// Uses modern Unity 6 linearVelocity and Rigidbody2D.MovePosition.
    /// </summary>
    [RequireComponent(typeof(Collider2D), typeof(Rigidbody2D))]
    public class MovingPlatform : MonoBehaviour
    {
        [Header("Movement Path")]
        [Tooltip("Offset relative to initial position representing the other waypoint.")]
        [SerializeField] private Vector3 moveOffset = new Vector3(5f, 0f, 0f);

        [Tooltip("Movement speed in units per second.")]
        [SerializeField] private float speed = 2.5f;

        [Tooltip("Pause duration at each end in seconds.")]
        [SerializeField] private float waitTime = 0.4f;

        [Header("Visual")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        private Vector3 startPos;
        private Vector3 targetPos;
        private Vector3 destination;
        private float waitTimer = 0f;
        private bool movingToTarget = true;
        private Rigidbody2D rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.useFullKinematicContacts = true;
            }
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            startPos = transform.position;
            targetPos = startPos + moveOffset;
            destination = targetPos;
        }

        private void FixedUpdate()
        {
            if (waitTimer > 0f)
            {
                waitTimer -= Time.fixedDeltaTime;
                return;
            }

            Vector3 currentPos = transform.position;
            Vector3 newPos = Vector3.MoveTowards(currentPos, destination, speed * Time.fixedDeltaTime);

            if (rb != null)
            {
                rb.MovePosition(newPos);
            }
            else
            {
                transform.position = newPos;
            }

            if (Vector3.Distance(newPos, destination) < 0.02f)
            {
                movingToTarget = !movingToTarget;
                destination = movingToTarget ? targetPos : startPos;
                waitTimer = waitTime;
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!collision.gameObject.CompareTag("Player")) return;

            // Only attach player if landing on top of the platform
            for (int i = 0; i < collision.contactCount; i++)
            {
                if (collision.GetContact(i).normal.y < -0.5f)
                {
                    collision.transform.SetParent(transform);
                    break;
                }
            }
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                if (collision.transform.parent == transform)
                {
                    collision.transform.SetParent(null);
                }
            }
        }

        private void OnDisable()
        {
            // Safeguard unparenting if platform is disabled
            transform.DetachChildren();
        }

        public void SetMoveOffset(Vector3 offset)
        {
            moveOffset = offset;
            startPos = transform.position;
            targetPos = startPos + moveOffset;
            destination = targetPos;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Vector3 start = Application.isPlaying ? startPos : transform.position;
            Vector3 end = start + moveOffset;
            Gizmos.DrawLine(start, end);
            Gizmos.DrawWireSphere(end, 0.25f);
        }
    }
}
