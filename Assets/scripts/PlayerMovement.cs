using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;

    [Header("Jump")]
    public float jumpHeight = 2f;

    [Header("Gravity")]
    public float gravity = -9.81f;

    [Header("Input")]
    public InputActionReference movementAction;
    public InputActionReference jumpAction;

    private CharacterController controller;
    private float verticalVelocity;

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
    }

    private void OnEnable()
    {
        if (movementAction != null && movementAction.action != null)
            movementAction.action.Enable();

        if (jumpAction != null && jumpAction.action != null)
            jumpAction.action.Enable();
    }

    private void OnDisable()
    {
        if (movementAction != null && movementAction.action != null)
            movementAction.action.Disable();

        if (jumpAction != null && jumpAction.action != null)
            jumpAction.action.Disable();
    }

    private void Update()
    {
        // Make sure the Character Controller exists
        if (controller == null)
            return;

        // Make sure movement input exists
        if (movementAction == null || movementAction.action == null)
            return;

        Vector2 input = movementAction.action.ReadValue<Vector2>();
        Debug.Log(input);

        Vector3 move = new Vector3(input.x, 0f, input.y);

        controller.Move(move * moveSpeed * Time.deltaTime);

        // Gravity
        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }

        // Jump
        if (jumpAction != null &&
            jumpAction.action != null &&
            controller.isGrounded &&
            jumpAction.action.WasPressedThisFrame())
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 verticalMove = Vector3.up * verticalVelocity;

        controller.Move(verticalMove * Time.deltaTime);
    }
}