using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerRespawn : MonoBehaviour
{
    [Header("Fall Detection")]
    public float fallThresholdY = -10f;

    [Header("Spawn")]
    public Transform spawnPoint;

    private CharacterController controller;
    private PlayerMovement movement;
    private Vector3 startPosition;
    private Quaternion startRotation;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        movement = GetComponent<PlayerMovement>();

        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    private void Update()
    {
        if (transform.position.y < fallThresholdY)
        {
            Respawn();
        }
    }

    public void Respawn()
    {
        Vector3 targetPosition = spawnPoint != null ? spawnPoint.position : startPosition;
        Quaternion targetRotation = spawnPoint != null ? spawnPoint.rotation : startRotation;

        // The Character Controller writes its own position back to the transform every
        // frame, so it has to be off while we teleport or the move gets overwritten.
        if (controller != null)
            controller.enabled = false;

        transform.SetPositionAndRotation(targetPosition, targetRotation);

        if (controller != null)
            controller.enabled = true;

        // Without this the downward speed built up during the fall carries over and
        // the player drops straight back through the floor on the next frame.
        if (movement != null)
        {
            movement.ResetVerticalVelocity();

            // The look script drives rotation from its own stored yaw, so it has to be
            // told about the teleport or it snaps the player back on the next frame.
            movement.SyncLookToTransform();
        }
    }
}
