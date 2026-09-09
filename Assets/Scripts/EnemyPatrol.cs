using UnityEngine;

/// <summary>
/// Walks the Enemy back and forth between two points.
///
/// The patrol path is derived from wherever the Enemy stands when Play is
/// pressed, so no waypoint objects are needed in the scene.
///
/// When an EnemyDetector is present, patrolling can stop automatically
/// once the Enemy notices the Player.
/// </summary>
[RequireComponent(typeof(EnemyDetector))]
public class EnemyPatrol : MonoBehaviour
{
    [Header("Path")]
    [SerializeField]
    [Tooltip("Offset from the starting position to the far end of the path.")]
    private Vector3 patrolOffset = new Vector3(0f, 0f, 6f);

    [Header("Movement")]
    [SerializeField]
    [Min(0f)]
    [Tooltip("Patrol speed in units per second.")]
    private float moveSpeed = 2f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("Seconds to pause at each end before turning around.")]
    private float waitTime = 1f;

    [SerializeField]
    [Tooltip("Turn the Enemy to face the direction it is walking.")]
    private bool faceMovementDirection = true;

    [SerializeField]
    [Min(0f)]
    [Tooltip("Turning speed in degrees per second.")]
    private float turnSpeed = 360f;

    [Header("Reaction to the Player")]
    [SerializeField]
    [Tooltip("Stop patrolling while the Enemy is in the Alert state.")]
    private bool haltWhenAlert = true;

    [Header("Debug (read only)")]
    [SerializeField]
    [Tooltip("Point the Enemy is currently walking towards.")]
    private Vector3 currentTarget;

    [SerializeField]
    [Tooltip("Remaining pause time at the end of the path.")]
    private float waitTimer;

    private EnemyDetector detector;
    private Vector3 pointA;
    private Vector3 pointB;

    private void Awake()
    {
        detector = GetComponent<EnemyDetector>();
    }

    private void Start()
    {
        // The position at the moment Play starts becomes one end of the path.
        pointA = transform.position;
        pointB = pointA + patrolOffset;

        currentTarget = pointB;
    }

    private void Update()
    {
        if (IsHalted())
        {
            return;
        }

        if (waitTimer > 0f)
        {
            waitTimer -= Time.deltaTime;
            return;
        }

        MoveTowardsTarget();
    }

    /// <summary>
    /// Patrolling stops once the Enemy is aware of the Player, so the
    /// behavior change is visible and not just a color swap.
    /// </summary>
    private bool IsHalted()
    {
        if (!haltWhenAlert)
        {
            return false;
        }

        return detector != null && detector.CurrentState == EnemyState.Alert;
    }

    private void MoveTowardsTarget()
    {
        Vector3 direction = currentTarget - transform.position;

        if (faceMovementDirection)
        {
            RotateTowards(direction);
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            currentTarget,
            moveSpeed * Time.deltaTime);

        // A distance threshold is used because floating point positions
        // rarely land exactly on the target.
        if (Vector3.Distance(transform.position, currentTarget) < 0.05f)
        {
            SwitchTarget();
        }
    }

    private void SwitchTarget()
    {
        currentTarget = currentTarget == pointA ? pointB : pointA;
        waitTimer = waitTime;
    }

    private void RotateTowards(Vector3 direction)
    {
        // Drop the vertical component so the Enemy does not tilt up or down.
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            turnSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Draws the patrol path in the Scene view. Outside Play mode the path
    /// is derived from the current position, so it can be tuned without
    /// running the game.
    /// </summary>
    private void OnDrawGizmos()
    {
        Vector3 start = Application.isPlaying ? pointA : transform.position;
        Vector3 end = Application.isPlaying ? pointB : transform.position + patrolOffset;

        Gizmos.color = Color.green;
        Gizmos.DrawLine(start, end);
        Gizmos.DrawWireCube(start, Vector3.one * 0.3f);
        Gizmos.DrawWireCube(end, Vector3.one * 0.3f);
    }
}
