using Unity.Netcode;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [Header("Speeds")]
    public float moveSpeed = 5f;
    public float sprintMultiplier = 2f;
    public float lookSpeed = 2f;

    [Header("Jumping")]
    public float jumpForce = 5f;
    public float gravity = 20f; // Etwas höher für besseres Gefühl

    private float rotationX = 0;
    private float verticalVelocity = 0;
    private CharacterController controller; // Referenz auf den Controller
    public Camera playerCamera;

    public override void OnNetworkSpawn()
    {
        // Wir holen uns die Komponente beim Start
        controller = GetComponent<CharacterController>();

        if (IsOwner)
        {
            playerCamera.enabled = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            playerCamera.enabled = false;
            if (playerCamera.GetComponent<AudioListener>())
                playerCamera.GetComponent<AudioListener>().enabled = false;
        }
    }

    void Update()
    {
        if (!IsOwner) return;

        // --- SPRINT LOGIK ---
        float currentSpeed = moveSpeed;
        if (Input.GetKey(KeyCode.LeftShift))
        {
            currentSpeed *= sprintMultiplier;
        }

        // --- BEWEGUNG (Horizontal/Vertikal) ---
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");
        Vector3 move = (transform.right * moveX + transform.forward * moveZ).normalized;

        // --- SPRUNG & SCHWERKRAFT ---
        // Der CharacterController weiß selbst, ob er am Boden ist
        if (controller.isGrounded)
        {
            verticalVelocity = -2f; // Ein bisschen Druck nach unten hält isGrounded stabil

            if (Input.GetButtonDown("Jump"))
            {
                verticalVelocity = jumpForce;
            }
        }
        else
        {
            // Fallbeschleunigung: Geschwindigkeit nimmt pro Frame ab
            verticalVelocity -= gravity * Time.deltaTime;
        }

        // --- FINALE BEWEGUNG ANWENDEN ---
        // Wir berechnen den gesamten Bewegungs-Vektor
        Vector3 finalMovement = move * currentSpeed;
        finalMovement.y = verticalVelocity;

        // WICHTIG: CharacterController.Move übernimmt die Kollisionsprüfung!
        controller.Move(finalMovement * Time.deltaTime);

        // --- MAUS-LOOK ---
        float mouseX = Input.GetAxis("Mouse X") * lookSpeed;
        transform.Rotate(Vector3.up * mouseX);

        float mouseY = Input.GetAxis("Mouse Y") * lookSpeed;
        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, -90f, 90f);
        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}