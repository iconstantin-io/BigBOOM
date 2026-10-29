using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float speed = 2.5f;
    public float rotationSpeed = 100f;
    public float jumpForce = 6f;
    public float gravity = -18f;

    private CharacterController controller;
    private Animator animator;
    private Vector3 velocity;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        // Guardamos si toca el suelo ANTES de mover nada
        bool grounded = controller.isGrounded;

        float move = Input.GetAxis("Vertical");
        float turn = Input.GetAxis("Horizontal");

        transform.Rotate(0, turn * rotationSpeed * Time.deltaTime, 0);

        // Animacion de caminar
        animator.SetBool("isWalking", Mathf.Abs(move) > 0.1f);

        if (grounded)
        {
            // Si esta cayendo o quieto en el suelo, ya no esta saltando
            if (velocity.y < 0)
            {
                velocity.y = -2f;
                animator.SetBool("isJump", false);
            }

            // Saltar
            if (Input.GetKeyDown(KeyCode.Space))
            {
                velocity.y = jumpForce;
                animator.SetBool("isJump", true);
            }
        }

        // Gravedad
        velocity.y += gravity * Time.deltaTime;

        // Un solo Move con todo junto
        Vector3 direction = transform.forward * move * speed;
        direction.y = velocity.y;
        controller.Move(direction * Time.deltaTime);
    }
}