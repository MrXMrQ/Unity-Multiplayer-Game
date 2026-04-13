using System.Collections;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
    [Header("Movement Speeds")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintMultiplier = 2f;
    [SerializeField] private float lookSpeed = 2f;

    [Header("Jumping & Gravity")]
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float gravity = 20f;

    [Header("Camera Settings")]
    [SerializeField] private Camera playerCamera;
    public Camera PlayerCamera => playerCamera;
    [SerializeField] private Vector3 firstPersonOffset = new Vector3(0, 0.6f, 0);
    [SerializeField] private bool lockCursorOnStart = true;

    private CharacterController controller;
    private float rotationX = 0;
    private float verticalVelocity = 0;

    /// <summary>
    /// Initializes the controller and camera state based on ownership.
    /// </summary>
    public override void OnNetworkSpawn()
    {
        controller = GetComponent<CharacterController>();

        if (IsOwner)
        {
            playerCamera.enabled = true;

            // Cursor sperren
            if (lockCursorOnStart)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            // Starte die Spawn-Verzögerung
            StartCoroutine(DelayedSpawn());

            if (playerCamera.TryGetComponent<AudioListener>(out var listener))
            {
                listener.enabled = true;
            }
        }
        else
        {
            playerCamera.enabled = false;
            this.enabled = false;
        }
    }

    private void Update()
    {
        if (!IsOwner || controller == null || !controller.enabled) return;

        bool inputBlocked = PauseManager.IsLocalPaused || ShopManager.IsLocalShopOpen;

        HandleRotation(inputBlocked);
        HandleMovement(inputBlocked);
    }

    /// <summary>
    /// Processes mouse input for player and camera rotation.
    /// </summary>
    /// <param name="blocked">Whether input is currently suppressed by UI.</param>
    private void HandleRotation(bool blocked)
    {
        if (blocked) return;

        // Horizontal rotation (Player)
        float mouseX = Input.GetAxis("Mouse X") * lookSpeed;
        transform.Rotate(Vector3.up * mouseX);

        // Vertical rotation (Camera)
        float mouseY = Input.GetAxis("Mouse Y") * lookSpeed;
        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, -90f, 90f);
        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
    }

    /// <summary>
    /// Calculates and applies movement vectors including jumping and gravity.
    /// </summary>
    /// <param name="blocked">Whether movement input is currently suppressed by UI.</param>
    private void HandleMovement(bool blocked)
    {
        Vector3 moveDirection = Vector3.zero;

        if (!blocked)
        {
            float moveX = Input.GetAxisRaw("Horizontal");
            float moveZ = Input.GetAxisRaw("Vertical");
            moveDirection = (transform.right * moveX + transform.forward * moveZ).normalized;
        }

        // Apply Gravity and Jumping
        if (controller.isGrounded)
        {
            verticalVelocity = -0.5f;
            if (!blocked && Input.GetButtonDown("Jump"))
            {
                verticalVelocity = jumpForce;
            }
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }

        float currentSpeed = moveSpeed;
        if (controller.isGrounded && Input.GetKey(KeyCode.LeftShift) && !blocked)
        {
            currentSpeed *= sprintMultiplier;
        }

        Vector3 finalVelocity = moveDirection * currentSpeed;
        finalVelocity.y = verticalVelocity;

        controller.Move(finalVelocity * Time.deltaTime);
    }

    /// <summary>
    /// Waits for the FixedSpawnPoint to be ready before teleporting the player.
    /// </summary>
    private IEnumerator DelayedSpawn()
    {
        float timeout = 2f;
        float timer = 0;

        // Warten bis der Spawnpoint bereit ist (Klasse FixedSpawnPoint muss existieren)
        while (!FixedSpawnPoint.IsReady && timer < timeout)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (FixedSpawnPoint.IsReady)
        {
            if (controller != null) controller.enabled = false;

            transform.position = FixedSpawnPoint.Pos;
            transform.rotation = FixedSpawnPoint.Rot;

            // Ein Frame warten, damit die Physik-Engine die Position übernimmt
            yield return new WaitForFixedUpdate();

            if (controller != null) controller.enabled = true;
        }
    }
}