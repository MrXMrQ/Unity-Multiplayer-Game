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
    public Vector3 firstPersonOffset = new Vector3(0, 0.6f, 0); // Position im Kopf
    public Vector3 thirdPersonOffset = new Vector3(0, 2f, -4f); // Position hinter dem Spieler
    private bool isThirdPerson = false;

    [Header("UI Menü")]
    public GameObject pauseMenuPanel;
    private bool isPaused = false;

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

            // Initialen Status setzen
            isPaused = false;
            if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);

            SetCursorState(false);
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

        // --- PAUSE TOGGLE ---
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }

        // --- SCHWERKRAFT (Wird IMMER berechnet, auch in Pause) ---
        if (controller.isGrounded)
        {
            verticalVelocity = -0.5f;
            // Springen nur erlauben, wenn NICHT pausiert
            if (!isPaused && Input.GetButtonDown("Jump"))
            {
                verticalVelocity = jumpForce;
            }
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }

        // --- INPUT & LOOK (Nur wenn NICHT pausiert) ---
        Vector3 moveDirection = Vector3.zero;
        float currentSpeed = moveSpeed;

        if (!isPaused)
        {
            if (Input.GetKeyDown(KeyCode.F5)) isThirdPerson = !isThirdPerson;

            currentSpeed = moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? sprintMultiplier : 1f);

            // Look
            float mouseX = Input.GetAxis("Mouse X") * lookSpeed;
            transform.Rotate(Vector3.up * mouseX);

            float mouseY = Input.GetAxis("Mouse Y") * lookSpeed;
            rotationX -= mouseY;
            rotationX = Mathf.Clamp(rotationX, -90f, 90f);
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);

            // Move Input
            float moveX = Input.GetAxisRaw("Horizontal");
            float moveZ = Input.GetAxisRaw("Vertical");
            moveDirection = (transform.right * moveX + transform.forward * moveZ).normalized;
        }

        // --- FINALE BEWEGUNG ANWENDEN ---
        // (Wichtig: verticalVelocity ist hier immer enthalten, damit man weiterfällt!)
        Vector3 finalMovement = moveDirection * currentSpeed;
        finalMovement.y = verticalVelocity;
        controller.Move(finalMovement * Time.deltaTime);

        // Kamera Position
        playerCamera.transform.localPosition = isThirdPerson ? thirdPersonOffset : firstPersonOffset;
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(isPaused);
        SetCursorState(isPaused);
    }

    private void SetCursorState(bool paused)
    {
        Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = paused;
    }

    public void QuitGame()
    {
        Debug.Log("Quit");
        NetworkManager.Singleton.Shutdown();
        Application.Quit();

        UnityEditor.EditorApplication.isPlaying = false;

    }

    public override void OnNetworkDespawn()
    {
        playerColor.OnValueChanged -= OnColorChanged;
    }
}