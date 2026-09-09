using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Moves the Player with WASD or the arrow keys.
/// Movement is applied directly to the Transform, without a Rigidbody,
/// to keep the setup simple and predictable.
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField]
    [Min(0f)]
    [Tooltip("Movement speed in units per second.")]
    private float moveSpeed = 5f;

    [Header("Debug (read only)")]
    [SerializeField]
    [Tooltip("Normalized direction produced by the current input.")]
    private Vector3 moveDirection;

    private void Update()
    {
        ReadInput();
        Move();
    }

    /// <summary>
    /// Reads the keyboard and turns it into a single direction vector.
    /// The vector is normalized so diagonal movement is not faster than
    /// movement along a single axis.
    /// </summary>
    private void ReadInput()
    {
        Keyboard keyboard = Keyboard.current;

        // Keyboard can be null when no device is connected.
        if (keyboard == null)
        {
            moveDirection = Vector3.zero;
            return;
        }

        float horizontal = 0f;
        float vertical = 0f;

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
        {
            horizontal -= 1f;
        }

        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
        {
            horizontal += 1f;
        }

        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
        {
            vertical -= 1f;
        }

        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
        {
            vertical += 1f;
        }

        // Movement happens on the XZ plane. Y is left at zero so the
        // Player stays on the Ground.
        moveDirection = new Vector3(horizontal, 0f, vertical);

        if (moveDirection.sqrMagnitude > 1f)
        {
            moveDirection = moveDirection.normalized;
        }
    }

    /// <summary>
    /// Applies the movement. Multiplying by Time.deltaTime keeps the speed
    /// independent of the frame rate.
    /// </summary>
    private void Move()
    {
        if (moveDirection == Vector3.zero)
        {
            return;
        }

        transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.World);
    }
}
