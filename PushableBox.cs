using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class PushableBox : MonoBehaviour
{
    [Header("物理设置")]
    [SerializeField] private float mass = 10f;
    [SerializeField] private float drag = 1f;
    [SerializeField] private float angularDrag = 5f;
    [SerializeField] private float pushResistance = 1f;

    [Header("移动限制")]
    [SerializeField] private bool lockXAxis = false;
    [SerializeField] private bool lockYAxis = true;
    [SerializeField] private bool lockZAxis = false;
    [SerializeField] private bool lockRotation = true;

    [Header("视觉反馈")]
    [SerializeField] private bool showPushIndicator = true;
    [SerializeField] private Color normalColor = Color.gray;
    [SerializeField] private Color beingPushedColor = Color.yellow;

    private Rigidbody rb;
    private Renderer boxRenderer;
    private bool isBeingPushed = false;
    private Vector3 lastVelocity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        boxRenderer = GetComponent<Renderer>();

        SetupRigidbody();
        SetupConstraints();
    }

    private void SetupRigidbody()
    {
        rb.mass = mass;
        rb.drag = drag;
        rb.angularDrag = angularDrag;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    private void SetupConstraints()
    {
        RigidbodyConstraints constraints = RigidbodyConstraints.None;

        if (lockXAxis) constraints |= RigidbodyConstraints.FreezePositionX;
        if (lockYAxis) constraints |= RigidbodyConstraints.FreezePositionY;
        if (lockZAxis) constraints |= RigidbodyConstraints.FreezePositionZ;
        if (lockRotation) constraints |= RigidbodyConstraints.FreezeRotation;

        rb.constraints = constraints;
    }

    private void Update()
    {
        CheckIfBeingPushed();
        UpdateVisualFeedback();
    }

    private void CheckIfBeingPushed()
    {
        Vector3 velocityChange = rb.velocity - lastVelocity;
        isBeingPushed = velocityChange.magnitude > 0.1f || rb.velocity.magnitude > 0.1f;
        lastVelocity = rb.velocity;
    }

    private void UpdateVisualFeedback()
    {
        if (!showPushIndicator || boxRenderer == null) return;

        boxRenderer.material.color = isBeingPushed ? beingPushedColor : normalColor;
    }

    private void OnCollisionEnter(Collision collision)
    {
        HandlePlayerCollision(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        HandlePlayerCollision(collision);
    }

    private void HandlePlayerCollision(Collision collision)
    {
        PlayerController player = collision.collider.GetComponent<PlayerController>();
        if (player == null) return;

        Vector3 pushDirection = (transform.position - player.transform.position).normalized;
        pushDirection.y = 0f;

        float pushForce = CalculatePushForce(player);
        rb.AddForce(pushDirection * pushForce, ForceMode.Force);
    }

    private float CalculatePushForce(PlayerController player)
    {
        Rigidbody playerRb = player.GetComponent<Rigidbody>();
        if (playerRb == null) return 0f;

        float playerForce = playerRb.mass * playerRb.velocity.magnitude;
        float effectiveForce = playerForce / (mass * pushResistance);

        return Mathf.Clamp(effectiveForce, 0f, 100f);
    }

    public void SetMass(float newMass)
    {
        mass = newMass;
        rb.mass = mass;
    }

    public void SetPushResistance(float newResistance)
    {
        pushResistance = Mathf.Max(0.1f, newResistance);
    }

    public void ResetPosition(Vector3 position)
    {
        transform.position = position;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    private void OnDrawGizmosSelected()
    {
        if (rb == null) return;

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + rb.velocity * 0.5f);

        Gizmos.color = Color.red;
        if (rb.constraints.HasFlag(RigidbodyConstraints.FreezePositionX))
        {
            Gizmos.DrawLine(transform.position - Vector3.right, transform.position + Vector3.right);
        }
        if (rb.constraints.HasFlag(RigidbodyConstraints.FreezePositionZ))
        {
            Gizmos.DrawLine(transform.position - Vector3.forward, transform.position + Vector3.forward);
        }
    }
}
