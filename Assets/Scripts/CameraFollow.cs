using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// A third person camera that orbits the target with the mouse.
///
/// The camera sits on a virtual arm anchored to a pivot above the target. The
/// mouse controls the angle of that arm; its length shortens whenever scenery
/// gets between the camera and the target, so the view never ends up inside a
/// mountain or a tree.
///
/// The pivot is followed with damping rather than snapped to, which is what
/// keeps the view from feeling rigid. The aim is computed from the damped
/// position every frame, so the target still stays centered.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField]
    [Tooltip("Object the camera follows. Drag the Player here.")]
    private Transform target;

    [Header("Placement")]
    [SerializeField]
    [Min(0f)]
    [Tooltip("Resting length of the camera arm, in units.")]
    private float distance = 6f;

    [SerializeField]
    [Tooltip("Height of the pivot above the target's feet.")]
    private float height = 2f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("Damping time for following the target. 0 is rigid, larger is softer.")]
    private float followSmoothTime = 0.08f;

    [Header("Rotation")]
    [SerializeField]
    [Min(0f)]
    [Tooltip("Mouse look sensitivity.")]
    private float mouseSensitivity = 2f;

    [SerializeField]
    [Tooltip("Lowest the camera can look from. Negative values look up at the target.")]
    private float minPitch = -15f;

    [SerializeField]
    [Tooltip("Highest the camera can look from.")]
    private float maxPitch = 45f;

    [Header("Cursor")]
    [SerializeField]
    [Tooltip("Hide and lock the cursor while playing. Escape releases it, " +
             "clicking in the Game view takes it back.")]
    private bool lockCursor = true;

    [Header("Collision")]
    [SerializeField]
    [Tooltip("Pull the camera in when scenery blocks the view of the target.")]
    private bool avoidObstacles = true;

    [SerializeField]
    [Min(0.01f)]
    [Tooltip("Thickness of the probe. Roughly the near clip size of the camera.")]
    private float collisionRadius = 0.3f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("Extra gap kept between the camera and whatever it hit.")]
    private float collisionBuffer = 0.2f;

    [SerializeField]
    [Min(0.1f)]
    [Tooltip("Closest the camera may ever get to the pivot.")]
    private float minDistance = 0.8f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("How quickly the arm grows back once the view is clear again. " +
             "Shortening is always instant, so the camera never clips through.")]
    private float distanceRecoverSpeed = 4f;

    [Header("Flight")]
    [SerializeField]
    [Tooltip("PlayerController on the target. Filled in automatically if left empty.")]
    private PlayerController player;

    [SerializeField]
    [Tooltip("Extra arm length while the Player is flying, so the height reads better.")]
    private float extraDistanceWhenFlying = 2.5f;

    [SerializeField]
    [Min(0f)]
    [Tooltip("How quickly the arm grows and shrinks when a flight starts or ends.")]
    private float flightDistanceSmoothing = 3f;

    private float yaw;
    private float pitch = 15f;

    private Vector3 smoothedPivot;
    private Vector3 pivotVelocity;
    private float currentDistance;
    private float restingDistance;

    /// <summary>Orbit angle of the camera, readable by other scripts.</summary>
    public float Yaw => yaw;

    private void Start()
    {
        if (target == null)
        {
            Debug.LogError(
                "[CameraFollow] The 'target' field is empty. " +
                "Drag the Player object into the Main Camera Inspector.", this);
            enabled = false;
            return;
        }

        // If this script sits on the same object as its target, that object
        // ends up chasing its own position plus the offset and drifts away
        // forever.
        if (target == transform)
        {
            Debug.LogError(
                "[CameraFollow] The target cannot be this object itself. " +
                "This script belongs on the Main Camera, not on the Player.",
                this);
            enabled = false;
            return;
        }

        if (player == null)
        {
            player = target.GetComponent<PlayerController>();
        }

        // Start already in place, so the camera does not glide in from the
        // origin when Play is pressed.
        smoothedPivot = GetPivot();
        restingDistance = distance;
        currentDistance = distance;
        ApplyTransform();

        SetCursorLocked(lockCursor);
    }

    private void OnDisable()
    {
        // Never leave the editor with a captured cursor.
        SetCursorLocked(false);
    }

    /// <summary>
    /// LateUpdate runs after the Player has finished moving this frame.
    /// Using Update instead would make the camera chase last frame's
    /// position, which reads as jitter.
    /// </summary>
    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        float deltaTime = Time.deltaTime;

        HandleCursor();
        ReadMouseLook();

        smoothedPivot = Vector3.SmoothDamp(
            smoothedPivot,
            GetPivot(),
            ref pivotVelocity,
            followSmoothTime);

        UpdateDistance(deltaTime);
        ApplyTransform();
    }

    private Vector3 GetPivot()
    {
        return target.position + Vector3.up * height;
    }

    /// <summary>
    /// Escape releases the cursor so the editor stays usable; clicking in the
    /// Game view takes it back.
    /// </summary>
    private void HandleCursor()
    {
        if (!lockCursor)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            SetCursorLocked(false);
        }
        else if (mouse != null
                 && mouse.leftButton.wasPressedThisFrame
                 && Cursor.lockState != CursorLockMode.Locked)
        {
            SetCursorLocked(true);
        }
    }

    private void ReadMouseLook()
    {
        Mouse mouse = Mouse.current;

        if (mouse == null)
        {
            return;
        }

        // While the cursor is free the mouse belongs to the editor, not to us.
        if (lockCursor && Cursor.lockState != CursorLockMode.Locked)
        {
            return;
        }

        Vector2 mouseDelta = mouse.delta.ReadValue();

        yaw += mouseDelta.x * mouseSensitivity * 0.1f;
        pitch -= mouseDelta.y * mouseSensitivity * 0.1f;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    /// <summary>
    /// Works out how long the camera arm should be this frame: the resting
    /// length, widened during flight, then cut short by anything in the way.
    /// </summary>
    private void UpdateDistance(float deltaTime)
    {
        float wanted = distance;

        if (player != null && player.IsAirborne)
        {
            wanted += extraDistanceWhenFlying;
        }

        // Ease between the walking and flying lengths.
        restingDistance = Mathf.Lerp(
            restingDistance,
            wanted,
            Smoothing(flightDistanceSmoothing, deltaTime));

        float allowed = avoidObstacles
            ? GetUnobstructedDistance(restingDistance)
            : restingDistance;

        // Closing in has to be immediate, or the camera spends a few frames
        // inside the obstacle. Growing back can afford to be gentle.
        currentDistance = allowed < currentDistance
            ? allowed
            : Mathf.Lerp(currentDistance, allowed, Smoothing(distanceRecoverSpeed, deltaTime));
    }

    /// <summary>
    /// Sweeps a sphere from the pivot back along the arm and reports how far it
    /// got. Colliders belonging to the target are skipped, so the Player's own
    /// body never shoves the camera into its face.
    /// </summary>
    private float GetUnobstructedDistance(float wanted)
    {
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 direction = -(rotation * Vector3.forward);

        RaycastHit[] hits = Physics.SphereCastAll(
            smoothedPivot,
            collisionRadius,
            direction,
            wanted + collisionBuffer,
            ~0,
            QueryTriggerInteraction.Ignore);

        float closest = wanted;
        Transform targetRoot = target.root;

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider.transform.root == targetRoot)
            {
                continue;
            }

            // A zero distance means the sweep started already overlapping,
            // which tells us nothing useful about where the surface is.
            if (hits[i].distance <= 0f)
            {
                continue;
            }

            float usable = hits[i].distance - collisionBuffer;

            if (usable < closest)
            {
                closest = usable;
            }
        }

        return Mathf.Max(minDistance, closest);
    }

    private void ApplyTransform()
    {
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

        transform.position = smoothedPivot - rotation * Vector3.forward * currentDistance;

        Vector3 toPivot = smoothedPivot - transform.position;

        // LookRotation of a zero vector is undefined; minDistance normally
        // prevents it, but a degenerate setup should not spam the Console.
        if (toPivot.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(toPivot);
        }
    }

    private static void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    /// <summary>
    /// Exponential smoothing factor. Unlike a plain "speed * deltaTime" this
    /// lands on the same result whatever the frame rate is.
    /// </summary>
    private static float Smoothing(float sharpness, float deltaTime)
    {
        return 1f - Mathf.Exp(-sharpness * deltaTime);
    }
}
