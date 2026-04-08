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
    public float gravity = 20f; // Etwas höher für besseres Gefühl

    private float rotationX = 0;
    private float verticalVelocity = 0;
    private CharacterController controller; // Referenz auf den Controller
    public Camera playerCamera;

    [Header("Camera Settings")]
    private bool isThirdPerson = false;
    public Vector3 firstPersonOffset = new Vector3(0, 0.6f, 0); // Position im Kopf
    public Vector3 thirdPersonOffset = new Vector3(0, 2f, -4f); // Position hinter dem Spieler

    public override void OnNetworkSpawn()
    {
        // Wir holen uns die Komponente beim Start
        controller = GetComponent<CharacterController>();

        if (IsOwner)
        {
            playerCamera.enabled = true;
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

        if (IsServer)
        {
            // Der Server würfelt eine Farbe aus
            playerColor.Value = new Color(Random.value, Random.value, Random.value);
        }
        else
        {
            // Clients wenden die bereits gesetzte Farbe an
            ApplyColor(playerColor.Value);
        }
    }

    private IEnumerator DelayedSpawn()
    {
        // Warte maximal 2 Sekunden, bis der Spawner bereit ist
        float timer = 0;
        while (!FixedSpawnPoint.IsReady && timer < 2f)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        if (FixedSpawnPoint.IsReady)
        {
            // 1. Controller komplett aus
            if (controller != null) controller.enabled = false;

            // 2. Position hart setzen
            transform.position = FixedSpawnPoint.Pos;
            transform.rotation = FixedSpawnPoint.Rot;

            // 3. Einen Frame warten, damit Unity die Position registriert
            yield return new WaitForFixedUpdate();

            // 4. Controller wieder an
            if (controller != null) controller.enabled = true;

            Debug.Log("Teleport zum festen Punkt abgeschlossen!");
        }
        else
        {
            Debug.LogError("Spawner wurde nicht rechtzeitig gefunden!");
        }
    }

    void Update()
    {
        if (!IsOwner) return;

        // --- NEU: Abbruch, wenn der Controller gerade deaktiviert ist (z.B. während Spawn) ---
        if (controller == null || !controller.enabled) return;

        // --- KAMERA WECHSEL (F5) ---
        if (Input.GetKeyDown(KeyCode.F5))
        {
            isThirdPerson = !isThirdPerson; // Umschalten zwischen true/false
        }

        // --- SPRINT LOGIK ---
        float currentSpeed = moveSpeed;
        if (Input.GetKey(KeyCode.LeftShift))
        {
            currentSpeed *= sprintMultiplier;
        }

        // Kamera-Position sanft oder hart setzen
        if (isThirdPerson)
        {
            // Kamera hinter den Spieler schieben
            // Wir nutzen transform.TransformPoint, damit der Offset sich mit dem Spieler mitdreht
            playerCamera.transform.localPosition = thirdPersonOffset;
        }
        else
        {
            // Kamera zurück in den Kopf
            playerCamera.transform.localPosition = firstPersonOffset;
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

    // Die NetworkVariable sorgt dafür, dass die Farbe über das Netz synchronisiert wird
    private NetworkVariable<Color> playerColor = new NetworkVariable<Color>(
        Color.white,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server // Nur der Server darf die Farbe festlegen
    );

    private void Awake()
    {
        // Wir abonnieren die Änderung der Variable, damit die Farbe aktualisiert wird
        playerColor.OnValueChanged += OnColorChanged;
    }

    private void OnColorChanged(Color previous, Color current)
    {
        ApplyColor(current);
    }

    private void ApplyColor(Color color)
    {
        // Wir suchen den Renderer der Kapsel und setzen die Farbe
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.material.color = color;
        }
    }

    // Wichtig: Event wieder abbestellen, wenn das Objekt zerstört wird
    public override void OnNetworkDespawn()
    {
        playerColor.OnValueChanged -= OnColorChanged;
    }
}