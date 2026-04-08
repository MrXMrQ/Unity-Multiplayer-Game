using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class PlayerController : NetworkBehaviour
{
    [Header("Speeds")]
    public float moveSpeed = 5f;
    public float sprintMultiplier = 2f;
    public float lookSpeed = 2f;

    [Header("Jumping")]
    public float jumpForce = 5f;
    public float gravity = 20f;

    private float rotationX = 0;
    private float verticalVelocity = 0;
    private CharacterController controller;
    public Camera playerCamera;

    [Header("Camera Settings")]
    private bool isThirdPerson = false;
    public Vector3 firstPersonOffset = new Vector3(0, 0.6f, 0);
    public Vector3 thirdPersonOffset = new Vector3(0, 2f, -4f);

    // Die NetworkVariable für die Farbe (Server schreibt, alle lesen)
    private NetworkVariable<Color> playerColor = new NetworkVariable<Color>(
        Color.white,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        controller = GetComponent<CharacterController>();

        if (IsOwner)
        {
            playerCamera.enabled = true;
            // Falls ein AudioListener auf der Kamera ist, aktivieren
            if (playerCamera.GetComponent<AudioListener>())
                playerCamera.GetComponent<AudioListener>().enabled = true;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            StartCoroutine(DelayedSpawn());
        }
        else
        {
            playerCamera.enabled = false;
            if (playerCamera.GetComponent<AudioListener>())
                playerCamera.GetComponent<AudioListener>().enabled = false;
        }

        // Farbe initial zuweisen
        ApplyColor(playerColor.Value);

        if (IsServer)
        {
            playerColor.Value = new Color(Random.value, Random.value, Random.value);
        }
    }

    private void Awake()
    {
        playerColor.OnValueChanged += OnColorChanged;
    }

    private void OnColorChanged(Color previous, Color current)
    {
        ApplyColor(current);
    }

    private void ApplyColor(Color color)
    {
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.material.color = color;
        }
    }

    private IEnumerator DelayedSpawn()
    {
        float timer = 0;
        while (!FixedSpawnPoint.IsReady && timer < 2f)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (FixedSpawnPoint.IsReady)
        {
            if (controller != null) controller.enabled = false;

            transform.position = FixedSpawnPoint.Pos;
            transform.rotation = FixedSpawnPoint.Rot;

            yield return new WaitForFixedUpdate();

            if (controller != null) controller.enabled = true;
            Debug.Log("Teleport abgeschlossen!");
        }
    }

    void Update()
    {
        if (!IsOwner) return;
        if (controller == null || !controller.enabled) return;

        // --- INPUTS ---
        if (Input.GetKeyDown(KeyCode.F5)) isThirdPerson = !isThirdPerson;

        float currentSpeed = moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? sprintMultiplier : 1f);

        // --- MAUS-LOOK (Links/Rechts dreht den Körper, Hoch/Runter die Kamera) ---
        float mouseX = Input.GetAxis("Mouse X") * lookSpeed;
        transform.Rotate(Vector3.up * mouseX);

        float mouseY = Input.GetAxis("Mouse Y") * lookSpeed;
        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, -90f, 90f);
        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);

        // --- KAMERA POSITION ---
        playerCamera.transform.localPosition = isThirdPerson ? thirdPersonOffset : firstPersonOffset;

        // --- BEWEGUNG ---
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");
        Vector3 moveDirection = (transform.right * moveX + transform.forward * moveZ).normalized;

        // --- SCHWERKRAFT & SPRUNG ---
        if (controller.isGrounded)
        {
            verticalVelocity = -0.5f;
            if (Input.GetButtonDown("Jump"))
            {
                verticalVelocity = jumpForce;
            }
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }

        Vector3 finalMovement = moveDirection * currentSpeed;
        finalMovement.y = verticalVelocity;

        controller.Move(finalMovement * Time.deltaTime);

        // Cursor lösen
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public override void OnNetworkDespawn()
    {
        playerColor.OnValueChanged -= OnColorChanged;
    }
}