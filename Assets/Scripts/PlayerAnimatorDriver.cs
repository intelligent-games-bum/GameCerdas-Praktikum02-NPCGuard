using UnityEngine;

/// <summary>
/// Translates the Player's locomotion into Animator parameters.
///
/// The Animator Controller only needs one integer parameter. Every state is
/// entered from Any State by comparing that integer, which keeps the state
/// machine flat: three states, three transitions, no webs of bools to reason
/// about.
///
/// Put this next to <see cref="PlayerController"/>. The Animator itself usually
/// lives on the character model further down the hierarchy, and is found
/// automatically.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerAnimatorDriver : MonoBehaviour
{
    /// <summary>
    /// Values written into the Animator's integer parameter. They have to match
    /// the conditions set on the Any State transitions.
    /// </summary>
    public enum MoveAnimation
    {
        Idle = 0,
        Running = 1,
        Flying = 2
    }

    [Header("References")]
    [SerializeField]
    [Tooltip("Animator on the character model. Found in the children if left empty.")]
    private Animator animator;

    [Header("Parameters")]
    [SerializeField]
    [Tooltip("Integer parameter selecting the state. Must exist on the Controller.")]
    private string stateParameter = "MoveState";

    [SerializeField]
    [Tooltip("Optional float parameter holding how hard the Player is steering, " +
             "from 0 to 1. Leave the name empty if the Controller does not use it.")]
    private string speedParameter = "Speed";

    [SerializeField]
    [Min(0f)]
    [Tooltip("How quickly the speed parameter catches up. Only affects blending.")]
    private float speedSmoothing = 12f;

    [Header("Behaviour")]
    [SerializeField]
    [Tooltip("Play the flying clip only while a movement key is held. " +
             "Turn this off to keep the flying pose while hovering still.")]
    private bool flyingNeedsInput = true;

    [Header("Debug (read only)")]
    [SerializeField]
    [Tooltip("Animation selected on this frame.")]
    private MoveAnimation current = MoveAnimation.Idle;

    [SerializeField]
    [Tooltip("Smoothed value written into the speed parameter.")]
    private float speed;

    private PlayerController controller;
    private int stateHash;
    private int speedHash;
    private bool hasStateParameter;
    private bool hasSpeedParameter;

    private void Awake()
    {
        controller = GetComponent<PlayerController>();

        if (animator == null)
        {
            animator = FindAnimator();
        }
    }

    /// <summary>
    /// Picks the Animator to drive.
    ///
    /// A plain GetComponentInChildren would take the first one it meets, and
    /// that search starts at this GameObject. Character prefabs routinely carry
    /// an empty Animator on their root, so that stray one would win purely by
    /// sitting higher up the hierarchy. Preferring an Animator that actually
    /// has a Controller picks the model that was set up on purpose.
    /// </summary>
    private Animator FindAnimator()
    {
        Animator[] found = GetComponentsInChildren<Animator>();

        for (int i = 0; i < found.Length; i++)
        {
            if (found[i].runtimeAnimatorController != null)
            {
                return found[i];
            }
        }

        return found.Length > 0 ? found[0] : null;
    }

    private void Start()
    {
        if (animator == null)
        {
            Debug.LogError(
                "[PlayerAnimatorDriver] No Animator found. Add one to the character " +
                "model, or drag it into this Inspector.", this);
            enabled = false;
            return;
        }

        if (animator.runtimeAnimatorController == null)
        {
            Debug.LogError(
                $"[PlayerAnimatorDriver] The Animator on '{animator.name}' has no " +
                "Controller assigned. If the character model is a different object, " +
                "drag its Animator into this Inspector.", animator);
            enabled = false;
            return;
        }

        stateHash = Animator.StringToHash(stateParameter);
        speedHash = Animator.StringToHash(speedParameter);

        hasStateParameter = HasParameter(stateParameter, AnimatorControllerParameterType.Int);
        hasSpeedParameter = HasParameter(speedParameter, AnimatorControllerParameterType.Float);

        if (!hasStateParameter)
        {
            Debug.LogError(
                $"[PlayerAnimatorDriver] The Controller has no int parameter named " +
                $"'{stateParameter}'. Add it, or correct the name here.", this);
            enabled = false;
            return;
        }

        // Push the starting state so frame one already matches reality.
        animator.SetInteger(stateHash, (int)current);
    }

    private void Update()
    {
        MoveAnimation wanted = Resolve();

        if (wanted != current)
        {
            current = wanted;
            animator.SetInteger(stateHash, (int)current);
        }

        if (hasSpeedParameter)
        {
            // Only count towards speed when actually running, so the value does
            // not sit high while hovering in place.
            float wantedSpeed = current == MoveAnimation.Idle ? 0f : controller.MoveAmount;

            speed = Mathf.Lerp(speed, wantedSpeed, Smoothing(speedSmoothing, Time.deltaTime));
            animator.SetFloat(speedHash, speed);
        }
    }

    /// <summary>
    /// Picks the animation for this frame.
    ///
    /// Both movement clips are gated behind the movement keys, so releasing
    /// WASD drops the character back to Idle whether it is on the ground or in
    /// the air.
    ///
    /// A plain jump keeps the ground poses. Only the flight itself, and the
    /// fall that follows it, use the flying clip.
    /// </summary>
    private MoveAnimation Resolve()
    {
        bool moving = controller.IsMoving;

        if (controller.UsesFlightPose)
        {
            return !flyingNeedsInput || moving
                ? MoveAnimation.Flying
                : MoveAnimation.Idle;
        }

        return moving ? MoveAnimation.Running : MoveAnimation.Idle;
    }

    private bool HasParameter(string parameterName, AnimatorControllerParameterType type)
    {
        if (string.IsNullOrEmpty(parameterName))
        {
            return false;
        }

        AnimatorControllerParameter[] parameters = animator.parameters;

        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].name == parameterName && parameters[i].type == type)
            {
                return true;
            }
        }

        return false;
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
