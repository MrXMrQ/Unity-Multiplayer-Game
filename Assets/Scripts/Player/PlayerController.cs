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
    public Vector3 firstPersonOffset = new Vector3(0, 0.6f, 0);
    public Vector3 thirdPersonOffset = new Vector3(0, 2f, -4f);
    private bool isThirdPerson = false;

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
            if (playerCamera.GetComponent<AudioListener>())
                playerCamera.GetComponent<AudioListener>().enabled = true;

            // Cursor initial sperren
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

    private void OnColorChanged(Color previous, Color current) => ApplyColor(current);

    private void ApplyColor(Color color)
    {
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null) renderer.material.color = color;
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
        }
    }

    void Update()
    {
        if (!IsOwner) return;
        if (controller == null || !controller.enabled) return;

        // --- 1. INPUT CHECK ---
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            GetComponent<PauseManager>().TogglePause();
        }

        // Prüfen ob Steuerung blockiert ist (Pause oder Shop)
        bool inputBlocked = PauseManager.IsLocalPaused || ShopManager.IsLocalShopOpen;

        // Variablen für Bewegung vorbereiten
        float moveX = 0;
        float moveZ = 0;
        Vector3 inputDir = Vector3.zero;

        // --- 2. MAUS-LOOK & INPUT (Nur wenn nicht blockiert) ---
        if (!inputBlocked)
        {
            // Kamera-Rotation
            float mouseX = Input.GetAxis("Mouse X") * lookSpeed;
            transform.Rotate(Vector3.up * mouseX);
            float mouseY = Input.GetAxis("Mouse Y") * lookSpeed;
            rotationX -= mouseY;
            rotationX = Mathf.Clamp(rotationX, -90f, 90f);
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);

            if (Input.GetKeyDown(KeyCode.F5)) isThirdPerson = !isThirdPerson;

            // Bewegungs-Tasten
            moveX = Input.GetAxisRaw("Horizontal");
            moveZ = Input.GetAxisRaw("Vertical");
            inputDir = (transform.right * moveX + transform.forward * moveZ).normalized;
        }

        // --- 3. SCHWERKRAFT & SPRUNG (Schwerkraft läuft IMMER) ---
        if (controller.isGrounded)
        {
            verticalVelocity = -0.5f; // Kleiner Anpressdruck zum Boden
            if (!inputBlocked && Input.GetButtonDown("Jump"))
            {
                verticalVelocity = jumpForce;
            }
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }

        // --- 4. FINALE BEWEGUNG KOMBINIEREN ---
        float currentSpeed = moveSpeed;
        if (controller.isGrounded && Input.GetKey(KeyCode.LeftShift))
            currentSpeed *= sprintMultiplier;

        Vector3 horizontalMovement = inputDir * currentSpeed;
        Vector3 finalMovement = horizontalMovement;
        finalMovement.y = verticalVelocity;

        // Ausführung der Bewegung
        controller.Move(finalMovement * Time.deltaTime);

        // Kamera Position (Third Person Toggle)
        playerCamera.transform.localPosition = isThirdPerson ? thirdPersonOffset : firstPersonOffset;
    }

    // Quit Funktion für den Quit-Button im PauseManager
    public void QuitGame()
    {
        if (NetworkManager.Singleton != null) NetworkManager.Singleton.Shutdown();
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public override void OnNetworkDespawn()
    {
        playerColor.OnValueChanged -= OnColorChanged;
    }
}