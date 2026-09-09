using UnityEngine;

/// <summary>
/// Enemy states, ordered from calm to fully aware.
/// Together they form the simplest kind of finite state machine:
/// every transition is decided by the distance to the Player.
/// </summary>
public enum EnemyState
{
    Idle,
    Suspicious,
    Alert
}

/// <summary>
/// How often state information is written to the Console.
/// </summary>
public enum DetectorLogMode
{
    /// <summary>Log nothing.</summary>
    Off,

    /// <summary>Log only when the state changes. Recommended.</summary>
    OnStateChange,

    /// <summary>
    /// Log the distance on every frame. Useful while checking the
    /// perception math, but it floods the Console and costs performance.
    /// Turn it on only while tracking down a problem.
    /// </summary>
    EveryFrame
}

/// <summary>
/// A minimal AI agent built around Perception - Decision - Action.
///
/// Perception : measures the distance from the Enemy to the Player.
/// Decision   : compares that distance against two radius parameters.
/// Action     : recolors the Point Light to match the resulting state.
/// </summary>
public class EnemyDetector : MonoBehaviour
{
    [Header("Perception")]
    [SerializeField]
    [Tooltip("Reference to the Player Transform. Drag the Player object here.")]
    private Transform player;

    [Header("AI Parameters")]
    [SerializeField]
    [Min(0f)]
    [Tooltip("Outer radius. A Player inside it makes the Enemy Suspicious.")]
    private float suspiciousRadius = 8f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("Inner radius. A Player inside it makes the Enemy Alert.")]
    private float alertRadius = 4f;

    [Header("Action")]
    [SerializeField]
    [Tooltip("Point Light attached under the Enemy. Its color follows the state.")]
    private Light enemyLight;

    [SerializeField]
    [Tooltip("Color used while the Player is outside both radii.")]
    private Color idleColor = Color.blue;

    [SerializeField]
    [Tooltip("Color used while the Player is between the two radii.")]
    private Color suspiciousColor = Color.yellow;

    [SerializeField]
    [Tooltip("Color used while the Player is inside the alert radius.")]
    private Color alertColor = Color.red;

    [Header("Debug (read only)")]
    [SerializeField]
    [Tooltip("Distance to the Player on this frame.")]
    private float currentDistance;

    [SerializeField]
    [Tooltip("Enemy state on this frame.")]
    private EnemyState currentState = EnemyState.Idle;

    [SerializeField]
    [Tooltip("How often to log. EveryFrame is meant for short debugging sessions only.")]
    private DetectorLogMode logMode = DetectorLogMode.OnStateChange;

    [Header("Gizmos")]
    [SerializeField]
    [Tooltip("Draw the detection radii even when the Enemy is not selected.")]
    private bool alwaysDrawGizmos = true;

    /// <summary>
    /// Current state, read by other components such as EnemyPatrol.
    /// </summary>
    public EnemyState CurrentState => currentState;

    private void Start()
    {
        if (player == null)
        {
            Debug.LogError(
                "[EnemyDetector] The 'player' field is empty. " +
                "Drag the Player object into the Enemy Inspector.", this);
        }

        if (enemyLight == null)
        {
            Debug.LogError(
                "[EnemyDetector] The 'enemyLight' field is empty. " +
                "Drag the Enemy's child Point Light into the Inspector.", this);
        }

        // Apply the starting color so the visuals match the state from frame one.
        ApplyStateColor(currentState);
    }

    /// <summary>
    /// Keeps the alert radius from growing past the suspicious radius.
    /// If they were swapped, the Suspicious state could never be reached.
    /// </summary>
    private void OnValidate()
    {
        if (alertRadius > suspiciousRadius)
        {
            alertRadius = suspiciousRadius;
        }
    }

    private void Update()
    {
        // Without a Player reference there is nothing to perceive.
        if (player == null)
        {
            return;
        }

        Perceive();
        Decide();

        if (logMode == DetectorLogMode.EveryFrame)
        {
            Debug.Log(
                $"[Frame {Time.frameCount}] distance: {currentDistance:F2} m, " +
                $"state: {currentState}",
                this);
        }
    }

    /// <summary>
    /// PERCEPTION - read information from the environment.
    /// Distance is the only sensor used here.
    /// </summary>
    private void Perceive()
    {
        currentDistance = Vector3.Distance(transform.position, player.position);
    }

    /// <summary>
    /// DECISION - compare what was perceived against the AI parameters.
    ///
    /// Order matters: the smaller radius is tested first, because a Player
    /// inside the alert radius is necessarily inside the suspicious one too.
    /// </summary>
    private void Decide()
    {
        EnemyState newState;

        if (currentDistance <= alertRadius)
        {
            newState = EnemyState.Alert;
        }
        else if (currentDistance <= suspiciousRadius)
        {
            newState = EnemyState.Suspicious;
        }
        else
        {
            newState = EnemyState.Idle;
        }

        // React only on a real transition, so the Console is not flooded
        // with one identical line per frame.
        if (newState != currentState)
        {
            SetState(newState);
        }
    }

    /// <summary>
    /// Records the new state and runs the action that belongs to it.
    /// </summary>
    private void SetState(EnemyState newState)
    {
        EnemyState previousState = currentState;
        currentState = newState;

        if (logMode == DetectorLogMode.OnStateChange)
        {
            Debug.Log(
                $"Enemy State: {previousState} -> {currentState} " +
                $"(distance: {currentDistance:F2} m)",
                this);
        }

        ApplyStateColor(currentState);
    }

    /// <summary>
    /// ACTION - the response the player can actually see.
    /// </summary>
    private void ApplyStateColor(EnemyState state)
    {
        if (enemyLight == null)
        {
            return;
        }

        enemyLight.color = GetStateColor(state);
    }

    private Color GetStateColor(EnemyState state)
    {
        switch (state)
        {
            case EnemyState.Alert:
                return alertColor;

            case EnemyState.Suspicious:
                return suspiciousColor;

            default:
                return idleColor;
        }
    }

    /// <summary>
    /// Draws the detection radii in the Scene view.
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!alwaysDrawGizmos)
        {
            return;
        }

        DrawDetectionGizmos();
    }

    private void OnDrawGizmosSelected()
    {
        if (alwaysDrawGizmos)
        {
            // Already drawn by OnDrawGizmos; avoid drawing twice.
            return;
        }

        DrawDetectionGizmos();
    }

    private void DrawDetectionGizmos()
    {
        // Each boundary is drawn in the color of the state it triggers.
        Gizmos.color = suspiciousColor;
        Gizmos.DrawWireSphere(transform.position, suspiciousRadius);

        Gizmos.color = alertColor;
        Gizmos.DrawWireSphere(transform.position, alertRadius);

        if (player == null)
        {
            return;
        }

        // Line from Enemy to Player, tinted by the current state so the
        // decision is readable straight from the Scene view.
        Gizmos.color = GetStateColor(currentState);
        Gizmos.DrawLine(transform.position, player.position);
    }
}
