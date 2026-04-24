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

    [Header("防穿模设置")]
    [SerializeField] private float maxHorizontalSpeed = 10f;
    [SerializeField] private float maxFallSpeed = 20f;
    [SerializeField] private float skinWidth = 0.05f;
    [SerializeField] private bool enableCCD = true;

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
    private Vector3 lastPosition;
    private Vector3 predictedPosition;

    private const float ANIMATION_BLEND_SPEED = 0.1f;
    private const float MIN_STAMINA_FOR_SPRINT = 5f;
    private const float MIN_STAMINA_FOR_JUMP = 10f;
    private const float COLLISION_PADDING = 0.01f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        playerStats = GetComponent<PlayerStats>();
        animator = GetComponent<Animator>();

        SetupRigidbody();
        SetupCollider();
        lastPosition = transform.position;
    }

    private void SetupRigidbody()
    {
        rb.freezeRotation = true;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        if (enableCCD)
        {
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }
        else
        {
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }
    }

    private void SetupCollider()
    {
        if (capsuleCollider != null)
        {
            capsuleCollider.contactOffset = 0.01f;
        }
    }

    private void Update()
    {
        GetInput();
        CheckGround();
        HandleDrag();
        UpdateAnimation();
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

        if (moveDirection.magnitude > 0.1f)
        {
            if (isGrounded)
            {
                rb.AddForce(moveDirection.normalized * currentSpeed * 10f, ForceMode.Force);
            }
            else
            {
                rb.AddForce(moveDirection.normalized * currentSpeed * 10f * airMultiplier, ForceMode.Force);
            }
        }

        LimitVelocity();
        RotatePlayer();
    }

    private void LimitVelocity()
    {
        Vector3 horizontalVelocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        float verticalVelocity = rb.velocity.y;

        if (horizontalVelocity.magnitude > maxHorizontalSpeed)
        {
            Vector3 limitedHorizontalVel = horizontalVelocity.normalized * maxHorizontalSpeed;
            rb.velocity = new Vector3(limitedHorizontalVel.x, verticalVelocity, limitedHorizontalVel.z);
        }

        if (verticalVelocity < -maxFallSpeed)
        {
            rb.velocity = new Vector3(rb.velocity.x, -maxFallSpeed, rb.velocity.z);
        }
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

    private void FixedUpdate()
    {
        MovePlayer();
        PreventClipping();
        lastPosition = transform.position;
    }

    private void PreventClipping()
    {
        if (capsuleCollider == null) return;

        float colliderRadius = capsuleCollider.radius * 0.95f;
        float colliderHeight = capsuleCollider.height;
        Vector3 center = capsuleCollider.center;

        Vector3 point1 = transform.TransformPoint(center + Vector3.up * (colliderHeight / 2 - colliderRadius));
        Vector3 point2 = transform.TransformPoint(center - Vector3.up * (colliderHeight / 2 - colliderRadius));

        Vector3 moveDelta = transform.position - lastPosition;
        float moveDistance = moveDelta.magnitude;

        if (moveDistance > 0.001f)
        {
            Vector3 moveDirection = moveDelta.normalized;
            float checkDistance = moveDistance + skinWidth;

            if (Physics.CapsuleCast(point1, point2, colliderRadius, moveDirection, out RaycastHit hit, checkDistance, groundLayer, QueryTriggerInteraction.Ignore))
            {
                float penetrationDepth = checkDistance - hit.distance;
                if (penetrationDepth > 0.001f)
                {
                    Vector3 correction = -moveDirection * penetrationDepth;
                    rb.position += correction;
                    rb.velocity = Vector3.ProjectOnPlane(rb.velocity, hit.normal);
                }
            }
        }

        CheckOverlapAndResolve(point1, point2, colliderRadius);
    }

    private void CheckOverlapAndResolve(Vector3 point1, Vector3 point2, float radius)
    {
        Collider[] overlaps = Physics.OverlapCapsule(point1, point2, radius + COLLISION_PADDING, groundLayer, QueryTriggerInteraction.Ignore);

        foreach (Collider overlap in overlaps)
        {
            if (overlap == capsuleCollider) continue;
            if (overlap.attachedRigidbody == rb) continue;

            if (Physics.ComputePenetration(
                capsuleCollider, transform.position, transform.rotation,
                overlap, overlap.transform.position, overlap.transform.rotation,
                out Vector3 direction, out float distance))
            {
                if (distance > 0.001f)
                {
                    Vector3 correction = direction * (distance + COLLISION_PADDING);
                    rb.position += correction;

                    if (rb.velocity.magnitude > 0.1f)
                    {
                        float dot = Vector3.Dot(rb.velocity, direction);
                        if (dot < 0)
                        {
                            rb.velocity -= direction * dot * 0.8f;
                        }
                    }
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        if (capsuleCollider != null && enableCCD)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            float radius = capsuleCollider.radius * 0.95f;
            float height = capsuleCollider.height;
            Vector3 center = capsuleCollider.center;

            Vector3 point1 = transform.TransformPoint(center + Vector3.up * (height / 2 - radius));
            Vector3 point2 = transform.TransformPoint(center - Vector3.up * (height / 2 - radius));

            DrawWireCapsule(point1, point2, radius);
        }
    }

    private void DrawWireCapsule(Vector3 point1, Vector3 point2, float radius)
    {
        Gizmos.DrawWireSphere(point1, radius);
        Gizmos.DrawWireSphere(point2, radius);

        Gizmos.DrawLine(point1 + Vector3.right * radius, point2 + Vector3.right * radius);
        Gizmos.DrawLine(point1 - Vector3.right * radius, point2 - Vector3.right * radius);
        Gizmos.DrawLine(point1 + Vector3.forward * radius, point2 + Vector3.forward * radius);
        Gizmos.DrawLine(point1 - Vector3.forward * radius, point2 - Vector3.forward * radius);
    }
}
