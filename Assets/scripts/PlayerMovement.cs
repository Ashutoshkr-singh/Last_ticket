using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 2f;
    public float runSpeed = 4.5f;

    [Header("Jump")]
    // Tuned so the player can climb from the tracks (y -2.13) onto the platform
    // (y -0.81), a 1.32m step, without clearing much more than that.
    public float jumpHeight = 1.5f;

    [Header("Gravity")]
    public float gravity = -9.81f;

    [Header("Look")]
    public float mouseSensitivity = 0.12f;
    public float minPitch = -85f;
    public float maxPitch = 85f;

    // The first person camera. Yaw is applied to the body, pitch to this transform,
    // so that transform.forward stays flat and can drive movement directly.
    public Transform cameraTransform;

    [Header("Head Bob")]
    public bool headBobEnabled = true;
    public float bobFrequency = 9f;
    public float bobVerticalAmount = 0.045f;
    public float bobHorizontalAmount = 0.03f;
    public float bobSmoothing = 12f;
    public float landingDipAmount = 0.12f;

    [Header("Input")]
    public InputActionReference movementAction;
    public InputActionReference jumpAction;

    private CharacterController controller;
    private float verticalVelocity;
    private float yaw;
    private float pitch;

    private Vector3 cameraBaseLocalPos;
    private float bobTimer;
    private float landingDip;
    private bool wasGroundedLastFrame;

    [Header("Pushing")]
    public float pushPower = 2.2f;

    public bool IsRunning { get; private set; }

    // Lets the player shove loose obstacles (the cone barriers) out of the way.
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        var body = hit.collider.attachedRigidbody;

        if (body == null || body.isKinematic)
            return;

        // Ignore downward hits so standing on something does not launch it.
        if (hit.moveDirection.y < -0.3f)
            return;

        // Set the velocity rather than adding force: contact fires every frame, so
        // impulses would accumulate and fling the obstacle across the platform.
        var push = new Vector3(hit.moveDirection.x, 0f, hit.moveDirection.z).normalized;
        var velocity = body.linearVelocity;
        var target = push * pushPower;

        body.linearVelocity = new Vector3(
            Mathf.Abs(target.x) > Mathf.Abs(velocity.x) ? target.x : velocity.x,
            velocity.y,
            Mathf.Abs(target.z) > Mathf.Abs(velocity.z) ? target.z : velocity.z);
    }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (controller == null)
        {
            Debug.LogError("PLAYER MOVEMENT ERROR: Character Controller is missing from Player!");
        }

        if (movementAction == null)
        {
            Debug.LogError("PLAYER MOVEMENT ERROR: Movement Action is not assigned!");
        }

        if (jumpAction == null)
        {
            Debug.LogError("PLAYER MOVEMENT ERROR: Jump Action is not assigned!");
        }

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        // Captured before any bob is applied, so it stays the true rest position.
        if (cameraTransform != null)
        {
            cameraBaseLocalPos = cameraTransform.localPosition;
        }

        SyncLookToTransform();
    }

    private void OnEnable()
    {
        if (movementAction != null && movementAction.action != null)
            movementAction.action.Enable();

        if (jumpAction != null && jumpAction.action != null)
            jumpAction.action.Enable();

        LockCursor(true);
    }

    private void OnDisable()
    {
        if (movementAction != null && movementAction.action != null)
            movementAction.action.Disable();

        if (jumpAction != null && jumpAction.action != null)
            jumpAction.action.Disable();

        LockCursor(false);
    }

    // Clears the accumulated gravity speed. Called after a teleport/respawn so the
    // player does not keep the downward velocity from the fall.
    public void ResetVerticalVelocity()
    {
        verticalVelocity = 0f;
    }

    // Re-reads the look angles from the transform. Needed after anything moves the
    // player directly (respawn), otherwise the stored yaw snaps it back next frame.
    public void SyncLookToTransform()
    {
        yaw = transform.eulerAngles.y;
        pitch = cameraTransform != null ? cameraTransform.localEulerAngles.x : 0f;
        if (pitch > 180f)
            pitch -= 360f;
    }

    private void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    private void Update()
    {
        // Make sure the Character Controller exists
        if (controller == null)
            return;

        HandleLook();
        HandleMovement();
    }

    private void HandleLook()
    {
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;

        // Escape releases the cursor so the editor stays usable, clicking recaptures it.
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            LockCursor(false);
        else if (mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
            LockCursor(true);

        // A pad should still aim when the cursor happens to be free.
        if (Cursor.lockState != CursorLockMode.Locked && !GameInput.GamepadPresent)
            return;

        Vector2 delta = GameInput.LookDelta(mouseSensitivity);

        // Mouse delta is already per frame, so it must not be scaled by deltaTime.
        yaw += delta.x;
        pitch -= delta.y;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        if (cameraTransform != null)
            cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void HandleMovement()
    {
        // Make sure movement input exists
        if (movementAction == null || movementAction.action == null)
            return;

        Vector2 input = movementAction.action.ReadValue<Vector2>() + GameInput.MoveStick();
        input = Vector2.ClampMagnitude(input, 1f);

        IsRunning = GameInput.RunHeld() && input.sqrMagnitude > 0.01f;
        float speed = IsRunning ? runSpeed : moveSpeed;

        // Movement is relative to where the player is looking, so W is always
        // "forward on screen" rather than a fixed world direction.
        Vector3 move = transform.right * input.x + transform.forward * input.y;
        move.y = 0f;

        if (move.sqrMagnitude > 1f)
            move.Normalize();

        controller.Move(move * speed * Time.deltaTime);

        // Gravity
        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        // Jump
        bool jumpPressed = GameInput.JumpPressed() ||
            (jumpAction != null && jumpAction.action != null && jumpAction.action.WasPressedThisFrame());

        if (controller.isGrounded && jumpPressed)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 verticalMove = Vector3.up * verticalVelocity;

        controller.Move(verticalMove * Time.deltaTime);

        HandleHeadBob(move.magnitude * speed);
    }

    // Walking sway: a sine bob on the camera so movement reads as footsteps rather
    // than gliding. Vertical runs at twice the horizontal rate, which is what makes
    // it feel like alternating steps instead of a single sway.
    private void HandleHeadBob(float horizontalSpeed)
    {
        if (cameraTransform == null)
            return;

        bool grounded = controller.isGrounded;

        // A small dip on landing sells the impact.
        if (grounded && !wasGroundedLastFrame)
            landingDip = landingDipAmount;

        wasGroundedLastFrame = grounded;
        landingDip = Mathf.Lerp(landingDip, 0f, Time.deltaTime * 6f);

        Vector3 target = cameraBaseLocalPos;

        if (headBobEnabled && grounded && horizontalSpeed > 0.1f)
        {
            bobTimer += Time.deltaTime * bobFrequency * (horizontalSpeed / Mathf.Max(moveSpeed, 0.01f));

            float amplitude = IsRunning ? 1.45f : 1f;
            target.y += Mathf.Sin(bobTimer * 2f) * bobVerticalAmount * amplitude;
            target.x += Mathf.Cos(bobTimer) * bobHorizontalAmount * amplitude;
        }
        else if (!grounded || horizontalSpeed <= 0.1f)
        {
            bobTimer = 0f;
        }

        target.y -= landingDip;

        cameraTransform.localPosition = Vector3.Lerp(
            cameraTransform.localPosition, target, Time.deltaTime * bobSmoothing);
    }
}
