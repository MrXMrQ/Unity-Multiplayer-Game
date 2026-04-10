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
            // --- ICH BIN DER LOKALE SPIELER ---
            playerCamera.enabled = true;
            if (playerCamera.GetComponent<AudioListener>())
                playerCamera.GetComponent<AudioListener>().enabled = true;

            // UI initialisieren
            isPaused = false;
            if (pauseMenuPanel != null)
            {
                pauseMenuPanel.SetActive(false); // Sichergehen, dass es aus ist
            }

            SetCursorState(false);
            StartCoroutine(DelayedSpawn());
        }
        else
        {
            // --- DAS IST EIN ANDERER SPIELER (Remote Client) ---
            playerCamera.enabled = false;
            if (playerCamera.GetComponent<AudioListener>())
                playerCamera.GetComponent<AudioListener>().enabled = false;

            // WICHTIG: UI für andere komplett deaktivieren oder löschen
            if (pauseMenuPanel != null)
            {
                // Wir schalten das Panel aus, damit wir das Menü von Spieler 2 nicht sehen
                pauseMenuPanel.SetActive(false);

                // Optional: Wenn das UI auf einem eigenen Canvas liegt, 
                // kannst du sogar das ganze Canvas-Objekt für Fremde löschen:
                // Destroy(pauseMenuPanel.transform.root.gameObject); 
            }
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

    // Füge diese Variable oben bei den anderen privaten Variablen hinzu:
    private Vector3 airMoveDirection;

    void Update()
    {
        if (!IsOwner) return;
        if (controller == null || !controller.enabled) return;

        if (Input.GetKeyDown(KeyCode.Escape)) TogglePause();

        // --- 1. MAUS-LOOK (Immer berechnen, außer in Pause) ---
        if (!isPaused)
        {
            float mouseX = Input.GetAxis("Mouse X") * lookSpeed;
            transform.Rotate(Vector3.up * mouseX);
            float mouseY = Input.GetAxis("Mouse Y") * lookSpeed;
            rotationX -= mouseY;
            rotationX = Mathf.Clamp(rotationX, -90f, 90f);
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
            if (Input.GetKeyDown(KeyCode.F5)) isThirdPerson = !isThirdPerson;
        }

        // --- 2. BEWEGUNGS-INPUT BERECHNEN ---
        float moveX = 0;
        float moveZ = 0;
        if (!isPaused)
        {
            moveX = Input.GetAxisRaw("Horizontal");
            moveZ = Input.GetAxisRaw("Vertical");
        }
        Vector3 inputDir = (transform.right * moveX + transform.forward * moveZ).normalized;

        // --- 3. SCHWERKRAFT & SPRUNG ---
        if (controller.isGrounded)
        {
            verticalVelocity = -0.5f;
            if (!isPaused && Input.GetButtonDown("Jump"))
            {
                verticalVelocity = jumpForce;
            }
        }
        else
        {
            // In der Luft: Schwerkraft wirkt ein
            verticalVelocity -= gravity * Time.deltaTime;
        }

        // --- 4. FINALE BEWEGUNG KOMBINIEREN ---
        // Wir berechnen die horizontale Bewegung (X/Z)
        float currentSpeed = moveSpeed;
        if (controller.isGrounded && Input.GetKey(KeyCode.LeftShift))
            currentSpeed *= sprintMultiplier;

        // Das ist der Clou: inputDir ist in der Luft nicht 0, wenn du WASD drückst!
        Vector3 horizontalMovement = inputDir * currentSpeed;

        // Y-Komponente (Fallen/Springen) hinzufügen
        Vector3 finalMovement = horizontalMovement;
        finalMovement.y = verticalVelocity;

        // Alles mit DeltaTime bewegen
        controller.Move(finalMovement * Time.deltaTime);

        // Kamera Position aktualisieren
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

        // Trenne die Netzwerkverbindung sauber
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }

        // Dieser Teil beendet die fertig gebaute .exe oder App
        Application.Quit();

        // Dieser Teil beendet den Play-Modus NUR im Unity Editor
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public override void OnNetworkDespawn()
    {
        playerColor.OnValueChanged -= OnColorChanged;
    }
}