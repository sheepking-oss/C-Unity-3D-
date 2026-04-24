using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class AntiClipPrevention : MonoBehaviour
{
    public enum DetectionMode
    {
        CastBased,
        OverlapBased,
        Both
    }

    [Header("检测设置")]
    [SerializeField] private DetectionMode detectionMode = DetectionMode.Both;
    [SerializeField] private LayerMask collisionLayers;
    [SerializeField] private float skinWidth = 0.05f;
    [SerializeField] private float maxPenetration = 0.1f;
    [SerializeField] private int correctionIterations = 3;

    [Header("调试设置")]
    [SerializeField] private bool drawGizmos = true;
    [SerializeField] private Color gizmoColor = Color.cyan;

    private Rigidbody rb;
    private Collider col;
    private Vector3 lastPosition;
    private Quaternion lastRotation;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        lastPosition = transform.position;
        lastRotation = transform.rotation;

        if (collisionLayers.value == 0)
        {
            collisionLayers = Physics.AllLayers;
        }
    }

    private void FixedUpdate()
    {
        PreventClipping();
        lastPosition = transform.position;
        lastRotation = transform.rotation;
    }

    private void PreventClipping()
    {
        if (rb == null || col == null) return;

        Vector3 moveDelta = transform.position - lastPosition;
        float moveDistance = moveDelta.magnitude;

        if (detectionMode == DetectionMode.CastBased || detectionMode == DetectionMode.Both)
        {
            if (moveDistance > 0.001f)
            {
                PerformCastDetection(moveDelta, moveDistance);
            }
        }

        if (detectionMode == DetectionMode.OverlapBased || detectionMode == DetectionMode.Both)
        {
            PerformOverlapDetection();
        }
    }

    private void PerformCastDetection(Vector3 moveDelta, float moveDistance)
    {
        Vector3 moveDirection = moveDelta.normalized;
        float castDistance = moveDistance + skinWidth;

        if (col is CapsuleCollider capsuleCollider)
        {
            float radius = capsuleCollider.radius * 0.95f;
            float height = capsuleCollider.height;
            Vector3 center = capsuleCollider.center;

            Vector3 point1 = transform.TransformPoint(center + Vector3.up * (height / 2 - radius));
            Vector3 point2 = transform.TransformPoint(center - Vector3.up * (height / 2 - radius));

            if (Physics.CapsuleCast(point1, point2, radius, moveDirection, out RaycastHit hit, castDistance, collisionLayers, QueryTriggerInteraction.Ignore))
            {
                ResolvePenetration(hit, moveDirection, castDistance);
            }
        }
        else if (col is BoxCollider boxCollider)
        {
            Vector3 halfExtents = Vector3.Scale(boxCollider.size, transform.lossyScale) * 0.5f;
            Vector3 center = transform.TransformPoint(boxCollider.center);

            if (Physics.BoxCast(center, halfExtents, moveDirection, out RaycastHit hit, transform.rotation, castDistance, collisionLayers, QueryTriggerInteraction.Ignore))
            {
                ResolvePenetration(hit, moveDirection, castDistance);
            }
        }
        else if (col is SphereCollider sphereCollider)
        {
            float radius = sphereCollider.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z) * 0.95f;
            Vector3 center = transform.TransformPoint(sphereCollider.center);

            if (Physics.SphereCast(center, radius, moveDirection, out RaycastHit hit, castDistance, collisionLayers, QueryTriggerInteraction.Ignore))
            {
                ResolvePenetration(hit, moveDirection, castDistance);
            }
        }
    }

    private void ResolvePenetration(RaycastHit hit, Vector3 moveDirection, float castDistance)
    {
        if (hit.collider == col) return;
        if (hit.collider.attachedRigidbody == rb) return;

        float penetrationDepth = castDistance - hit.distance;
        if (penetrationDepth > 0.001f && penetrationDepth <= maxPenetration)
        {
            Vector3 correction = -moveDirection * penetrationDepth;
            rb.position += correction;

            if (rb.velocity.magnitude > 0.1f)
            {
                float dot = Vector3.Dot(rb.velocity, hit.normal);
                if (dot < 0)
                {
                    rb.velocity -= hit.normal * dot * 0.9f;
                }
            }
        }
    }

    private void PerformOverlapDetection()
    {
        for (int i = 0; i < correctionIterations; i++)
        {
            if (!ResolveSingleOverlap())
            {
                break;
            }
        }
    }

    private bool ResolveSingleOverlap()
    {
        Collider[] overlaps = GetOverlappingColliders();
        bool resolved = false;

        foreach (Collider overlap in overlaps)
        {
            if (overlap == col) continue;
            if (overlap.attachedRigidbody == rb) continue;

            if (Physics.ComputePenetration(
                col, transform.position, transform.rotation,
                overlap, overlap.transform.position, overlap.transform.rotation,
                out Vector3 direction, out float distance))
            {
                if (distance > 0.001f && distance <= maxPenetration)
                {
                    Vector3 correction = direction * (distance + 0.001f);
                    rb.position += correction;

                    if (rb.velocity.magnitude > 0.1f)
                    {
                        float dot = Vector3.Dot(rb.velocity, direction);
                        if (dot < 0)
                        {
                            rb.velocity -= direction * dot * 0.8f;
                        }
                    }

                    resolved = true;
                }
            }
        }

        return resolved;
    }

    private Collider[] GetOverlappingColliders()
    {
        if (col is CapsuleCollider capsuleCollider)
        {
            float radius = capsuleCollider.radius;
            float height = capsuleCollider.height;
            Vector3 center = capsuleCollider.center;

            Vector3 point1 = transform.TransformPoint(center + Vector3.up * (height / 2 - radius));
            Vector3 point2 = transform.TransformPoint(center - Vector3.up * (height / 2 - radius));

            return Physics.OverlapCapsule(point1, point2, radius, collisionLayers, QueryTriggerInteraction.Ignore);
        }
        else if (col is BoxCollider boxCollider)
        {
            Vector3 halfExtents = Vector3.Scale(boxCollider.size, transform.lossyScale) * 0.5f;
            Vector3 center = transform.TransformPoint(boxCollider.center);

            return Physics.OverlapBox(center, halfExtents, transform.rotation, collisionLayers, QueryTriggerInteraction.Ignore);
        }
        else if (col is SphereCollider sphereCollider)
        {
            float radius = sphereCollider.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
            Vector3 center = transform.TransformPoint(sphereCollider.center);

            return Physics.OverlapSphere(center, radius, collisionLayers, QueryTriggerInteraction.Ignore);
        }

        return new Collider[0];
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos || col == null) return;

        Gizmos.color = gizmoColor;
        Gizmos.matrix = transform.localToWorldMatrix;

        if (col is CapsuleCollider capsuleCollider)
        {
            float radius = capsuleCollider.radius;
            float height = capsuleCollider.height;
            Vector3 center = capsuleCollider.center;

            Vector3 point1 = center + Vector3.up * (height / 2 - radius);
            Vector3 point2 = center - Vector3.up * (height / 2 - radius);

            Gizmos.DrawWireSphere(point1, radius);
            Gizmos.DrawWireSphere(point2, radius);

            Gizmos.DrawLine(point1 + Vector3.right * radius, point2 + Vector3.right * radius);
            Gizmos.DrawLine(point1 - Vector3.right * radius, point2 - Vector3.right * radius);
            Gizmos.DrawLine(point1 + Vector3.forward * radius, point2 + Vector3.forward * radius);
            Gizmos.DrawLine(point1 - Vector3.forward * radius, point2 - Vector3.forward * radius);
        }
        else if (col is BoxCollider boxCollider)
        {
            Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
        }
        else if (col is SphereCollider sphereCollider)
        {
            Gizmos.DrawWireSphere(sphereCollider.center, sphereCollider.radius);
        }
    }
}
