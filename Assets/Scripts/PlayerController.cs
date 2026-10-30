using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public float speed = 2.5f;
    public float rotationSpeed = 100f;
    public float jumpForce = 6f;
    public float gravity = -18f;
    [Tooltip("Activat: A/D giren el personatge. Desactivat: A/D el mouen de costat")]
    public bool girarAmbTeclat = true;
    [Tooltip("Controls de 3a persona: WASD es mouen segons on mira la camera i el personatge es gira cap on camina (ho activa el GestorJugadors)")]
    public bool relatiuCamera = false;
    public Transform referenciaCamera;

    private CharacterController controller;
    private Animator animator;
    private Vector3 velocity;
    private Quaternion girObjectiu;
    private bool teGirObjectiu;

    // Si s'esta movent (per enviar-ho a l'altre ordinador en online)
    public bool Caminant { get; private set; }

    // Animacio de caminar per als personatges que no controla aquest ordinador (rival online o bot)
    public void PosarCaminant(bool caminant)
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
        Caminant = caminant;
        if (animator != null) animator.SetBool("isWalking", caminant);
    }

    void Start()
    {
        Preparar();
    }

    void Preparar()
    {
        if (controller == null) controller = GetComponent<CharacterController>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        if (controller == null)
        {
            Debug.LogWarning("PlayerController: no hi havia CharacterController, se n'ha afegit un.", this);
            controller = gameObject.AddComponent<CharacterController>();
            controller.height = 1.85f;
            controller.radius = 0.28f;
            controller.center = new Vector3(0f, 0.93f, 0f);
        }
        if (animator == null)
            Debug.LogWarning("PlayerController: no hi ha Animator, el jugador es mourà sense animacions.", this);
    }

    // Mou el jugador a un punt (el CharacterController s'ha de desactivar un moment perque no ho desfaci)
    public void Teletransportar(Vector3 pos, Quaternion rot)
    {
        Preparar();
        bool actiu = controller.enabled;
        controller.enabled = false;
        transform.SetPositionAndRotation(pos, rot);
        controller.enabled = actiu;
        velocity = Vector3.zero;
    }

    // Gira el personatge cap a un punt (per exemple, la casella que acaba de picar) si esta quiet
    public void MirarCap(Vector3 punt)
    {
        Vector3 d = punt - transform.position;
        d.y = 0f;
        if (d.sqrMagnitude < 0.01f) return;
        girObjectiu = Quaternion.LookRotation(d);
        teGirObjectiu = true;
    }

    // El mateix pero de cop (per al bot, que no fa servir l'Update d'aquest script)
    public void MirarCapAra(Vector3 punt)
    {
        Vector3 d = punt - transform.position;
        d.y = 0f;
        if (d.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(d);
    }

    // Deixa el personatge quiet (per quan no li toca jugar)
    public void Aturar()
    {
        Preparar();
        velocity = Vector3.zero;
        teGirObjectiu = false;
        if (animator != null)
        {
            animator.SetBool("isWalking", false);
            animator.SetBool("isJump", false);
        }
    }

    void Update()
    {
        if (controller == null) return;

        // Guardamos si toca el suelo ANTES de mover nada
        bool grounded = controller.isGrounded;

        float move = Input.GetAxis("Vertical");
        float turn = Input.GetAxis("Horizontal");
        bool jumpPressed = Input.GetKeyDown(KeyCode.Space);

        // Alternativa amb el nou Input System: si l'Input antic no rep res, es llegeix el teclat directament
        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            float kMove = (kb.wKey.isPressed || kb.upArrowKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed || kb.downArrowKey.isPressed ? 1f : 0f);
            float kTurn = (kb.dKey.isPressed || kb.rightArrowKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed || kb.leftArrowKey.isPressed ? 1f : 0f);
            if (Mathf.Abs(kMove) > Mathf.Abs(move)) move = kMove;
            if (Mathf.Abs(kTurn) > Mathf.Abs(turn)) turn = kTurn;
            jumpPressed |= kb.spaceKey.wasPressedThisFrame;
        }

        // Amb el menu de pausa obert no es fa cas del teclat (en online el temps no s'atura)
        if (PauseMenuController.EstaPausat)
        {
            move = 0f;
            turn = 0f;
            jumpPressed = false;
        }

        Vector3 horitzontal;
        if (relatiuCamera && referenciaCamera != null)
        {
            // 3a persona: la direccio surt de la camera i el personatge es gira suaument cap on camina
            Vector3 davant = referenciaCamera.forward; davant.y = 0f; davant.Normalize();
            Vector3 dreta = referenciaCamera.right; dreta.y = 0f; dreta.Normalize();
            Vector3 dir = Vector3.ClampMagnitude(davant * move + dreta * turn, 1f);
            if (dir.sqrMagnitude > 0.01f)
            {
                teGirObjectiu = false;
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir), rotationSpeed * Time.deltaTime);
            }
            horitzontal = dir * speed;
        }
        else
        {
            float strafe = 0f;
            if (girarAmbTeclat) transform.Rotate(0, turn * rotationSpeed * Time.deltaTime, 0);
            else strafe = turn;
            horitzontal = Vector3.ClampMagnitude(transform.forward * move + transform.right * strafe, 1f) * speed;
        }

        // Quan esta quiet i ha picat una casella, s'hi gira
        if (teGirObjectiu)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, girObjectiu, rotationSpeed * Time.deltaTime);
            if (Quaternion.Angle(transform.rotation, girObjectiu) < 1f) teGirObjectiu = false;
        }

        // Animacion de caminar
        Caminant = horitzontal.sqrMagnitude > 0.01f;
        if (animator != null) animator.SetBool("isWalking", Caminant);

        if (grounded)
        {
            // Si esta cayendo o quieto en el suelo, ya no esta saltando
            if (velocity.y < 0)
            {
                velocity.y = -2f;
                if (animator != null) animator.SetBool("isJump", false);
            }

            // Saltar
            if (jumpPressed)
            {
                velocity.y = jumpForce;
                if (animator != null) animator.SetBool("isJump", true);
            }
        }

        // Gravedad
        velocity.y += gravity * Time.deltaTime;

        // Un solo Move con todo junto
        Vector3 direction = horitzontal;
        direction.y = velocity.y;
        controller.Move(direction * Time.deltaTime);
    }
}
