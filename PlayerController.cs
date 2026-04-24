using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerController : MonoBehaviour
{
    [Header("移动设置")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float groundDrag = 5f;

    [Header("跳跃设置")]
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private float jumpCooldown = 0.5f;
    [SerializeField] private float airMultiplier = 0.5f;

    [Header("地面检测")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.3f;
    [SerializeField] private LayerMask groundLayer;

    [Header("体力消耗")]
    [SerializeField] private float sprintStaminaCost = 10f;
    [SerializeField] private float jumpStaminaCost = 15f;

    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;
    private PlayerStats playerStats;
    private Animator animator;

    private float horizontalInput;
    private float verticalInput;
    private Vector3 moveDirection;
    private bool isGrounded;
    private bool isSprinting;
    private bool canJump = true;

    private const float ANIMATION_BLEND_SPEED = 0.1f;
    private const float MIN_STAMINA_FOR_SPRINT = 5f;
    private const float MIN_STAMINA_FOR_JUMP = 10f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        playerStats = GetComponent<PlayerStats>();
        animator = GetComponent<Animator>();

        rb.freezeRotation = true;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    private void Update()
    {
        GetInput();
        CheckGround();
        HandleDrag();
        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    private void GetInput()
    {
        horizontalInput = Input.GetAxis("Horizontal");
        verticalInput = Input.GetAxis("Vertical");

        isSprinting = Input.GetKey(KeyCode.LeftShift);

        if (Input.GetKeyDown(KeyCode.Space) && canJump && isGrounded)
        {
            TryJump();
        }
    }

    private void CheckGround()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);
    }

    private void HandleDrag()
    {
        rb.drag = isGrounded ? groundDrag : 0f;
    }

    private void MovePlayer()
    {
        moveDirection = transform.right * horizontalInput + transform.forward * verticalInput;

        float currentSpeed = moveSpeed;
        if (isSprinting && moveDirection.magnitude > 0.1f)
        {
            if (playerStats != null && playerStats.CurrentStamina >= MIN_STAMINA_FOR_SPRINT)
            {
                currentSpeed = sprintSpeed;
                playerStats.ConsumeStamina(sprintStaminaCost * Time.deltaTime);
            }
            else
            {
                isSprinting = false;
            }
        }

        if (isGrounded)
        {
            rb.AddForce(moveDirection.normalized * currentSpeed * 10f, ForceMode.Force);
        }
        else
        {
            rb.AddForce(moveDirection.normalized * currentSpeed * 10f * airMultiplier, ForceMode.Force);
        }

        Vector3 flatVel = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        if (flatVel.magnitude > currentSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * currentSpeed;
            rb.velocity = new Vector3(limitedVel.x, rb.velocity.y, limitedVel.z);
        }

        RotatePlayer();
    }

    private void RotatePlayer()
    {
        if (moveDirection.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }

    private void TryJump()
    {
        if (playerStats != null && playerStats.CurrentStamina >= MIN_STAMINA_FOR_JUMP)
        {
            Jump();
        }
    }

    private void Jump()
    {
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
        canJump = false;
        playerStats?.ConsumeStamina(jumpStaminaCost);
        Invoke(nameof(ResetJump), jumpCooldown);
    }

    private void ResetJump()
    {
        canJump = true;
    }

    private void UpdateAnimation()
    {
        if (animator == null) return;

        float movementMagnitude = new Vector3(horizontalInput, 0f, verticalInput).magnitude;
        float targetSpeed = isSprinting ? 2f : movementMagnitude;
        float currentSpeed = animator.GetFloat("Speed");
        animator.SetFloat("Speed", Mathf.Lerp(currentSpeed, targetSpeed, ANIMATION_BLEND_SPEED));

        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsJumping", !isGrounded && rb.velocity.y > 0.1f);
        animator.SetBool("IsFalling", !isGrounded && rb.velocity.y < -0.1f);
    }

    private void OnCollisionEnter(Collision collision)
    {
        HandleCollision(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        HandleCollision(collision);
    }

    private void HandleCollision(Collision collision)
    {
        Rigidbody otherRb = collision.collider.GetComponent<Rigidbody>();
        if (otherRb != null && !otherRb.isKinematic)
        {
            Vector3 forceDirection = (collision.transform.position - transform.position).normalized;
            forceDirection.y = 0f;
            float pushForce = 2f;
            otherRb.AddForce(forceDirection * pushForce, ForceMode.Impulse);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
