using UnityEngine;

/// <summary>
/// A simple third person camera that trails a target at a fixed offset.
///
/// It deliberately ignores the target's rotation, since the Player in this
/// project slides around without turning. That keeps the WASD directions
/// consistent with what is on screen.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField]
    [Tooltip("Object the camera follows. Drag the Player here.")]
    private Transform target;

    [Header("Placement")]
    [SerializeField]
    [Tooltip("Offset from the target. Y is height, negative Z sits behind it.")]
    private Vector3 offset = new Vector3(0f, 9f, -9f);

    [SerializeField]
    [Min(0f)]
    [Tooltip("Damping time for the camera. 0 is rigid, larger is softer.")]
    private float smoothTime = 0.2f;

    [Header("Aim")]
    [SerializeField]
    [Tooltip("Point the camera at the target every frame.")]
    private bool lookAtTarget = true;

    [SerializeField]
    [Tooltip("Raises the aim point so the target does not sit at the bottom edge.")]
    private Vector3 lookAtOffset = new Vector3(0f, 1f, 0f);

    private Vector3 currentVelocity;

    private void Start()
    {
        if (target == null)
        {
            Debug.LogError(
                "[CameraFollow] The 'target' field is empty. " +
                "Drag the Player object into the Main Camera Inspector.", this);
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

        // Start already in place, so the camera does not glide in from the
        // origin when Play is pressed.
        transform.position = target.position + offset;
        AimAtTarget();
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

        Vector3 desiredPosition = target.position + offset;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref currentVelocity,
            smoothTime);

        AimAtTarget();
    }

    private void AimAtTarget()
    {
        if (!lookAtTarget)
        {
            return;
        }

        transform.LookAt(target.position + lookAtOffset);
    }
}
