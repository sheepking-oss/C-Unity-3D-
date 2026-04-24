using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class ColliderOptimizer : MonoBehaviour
{
    public enum ColliderType
    {
        Ground,
        Wall,
        Ceiling,
        Obstacle,
        MovingPlatform
    }

    [Header("碰撞体类型")]
    [SerializeField] private ColliderType colliderType = ColliderType.Ground;

    [Header("优化设置")]
    [SerializeField] private bool autoOptimize = true;
    [SerializeField] private float minimumThickness = 0.1f;
    [SerializeField] private bool isTrigger = false;

    [Header("物理材质")]
    [SerializeField] private PhysicMaterial groundMaterial;
    [SerializeField] private PhysicMaterial wallMaterial;
    [SerializeField] private PhysicMaterial obstacleMaterial;

    [Header("调试信息")]
    [SerializeField] private bool showWarnings = true;

    private Collider col;
    private Rigidbody rb;

    private void Awake()
    {
        col = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();

        if (autoOptimize)
        {
            OptimizeCollider();
        }
    }

    private void OnValidate()
    {
        if (autoOptimize && Application.isEditor)
        {
            OptimizeCollider();
        }
    }

    public void OptimizeCollider()
    {
        if (col == null)
        {
            col = GetComponent<Collider>();
            if (col == null)
            {
                if (showWarnings)
                {
                    Debug.LogWarning($"ColliderOptimizer: {gameObject.name} 没有 Collider 组件", this);
                }
                return;
            }
        }

        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }

        SetupCollisionDetection();
        SetupPhysicMaterial();
        SetupColliderProperties();
        ValidateThickness();
    }

    private void SetupCollisionDetection()
    {
        if (rb != null)
        {
            if (colliderType == ColliderType.MovingPlatform)
            {
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.isKinematic = true;
            }
            else if (colliderType == ColliderType.Obstacle)
            {
                if (!rb.isKinematic)
                {
                    rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
                    rb.interpolation = RigidbodyInterpolation.Interpolate;
                }
            }
        }
    }

    private void SetupPhysicMaterial()
    {
        PhysicMaterial material = null;

        switch (colliderType)
        {
            case ColliderType.Ground:
                material = groundMaterial;
                break;
            case ColliderType.Wall:
                material = wallMaterial;
                break;
            case ColliderType.Obstacle:
                material = obstacleMaterial;
                break;
        }

        if (material != null)
        {
            col.material = material;
        }
        else if (showWarnings && col.material == null)
        {
            Debug.LogWarning($"ColliderOptimizer: {gameObject.name} 没有设置物理材质，建议创建并分配", this);
        }
    }

    private void SetupColliderProperties()
    {
        col.isTrigger = isTrigger;

        if (col is MeshCollider meshCollider)
        {
            if (!meshCollider.convex && rb != null && !rb.isKinematic)
            {
                if (showWarnings)
                {
                    Debug.LogWarning($"ColliderOptimizer: {gameObject.name} 的 MeshCollider 需要设置为 Convex 才能与非Kinematic的Rigidbody一起使用", this);
                }
            }

            if (colliderType == ColliderType.Ground || colliderType == ColliderType.Wall)
            {
                if (showWarnings && meshCollider.sharedMesh != null && meshCollider.sharedMesh.triangles.Length > 1000)
                {
                    Debug.LogWarning($"ColliderOptimizer: {gameObject.name} 的 MeshCollider 面数过多，建议使用更简单的碰撞体如 BoxCollider", this);
                }
            }
        }

        col.contactOffset = 0.01f;
    }

    private void ValidateThickness()
    {
        if (!showWarnings) return;

        if (col is BoxCollider boxCollider)
        {
            Vector3 size = boxCollider.size;
            float minDimension = Mathf.Min(size.x, size.y, size.z);

            if (minDimension < minimumThickness)
            {
                Debug.LogWarning($"ColliderOptimizer: {gameObject.name} 的 BoxCollider 厚度 ({minDimension:F2}) 小于最小建议厚度 ({minimumThickness:F2})，可能导致穿模问题", this);
            }

            if (colliderType == ColliderType.Ground || colliderType == ColliderType.Ceiling)
            {
                if (size.y < minimumThickness)
                {
                    Debug.LogWarning($"ColliderOptimizer: {gameObject.name} 作为地面/天花板，Y轴厚度 ({size.y:F2}) 太小，建议增加到至少 {minimumThickness:F2}", this);
                }
            }

            if (colliderType == ColliderType.Wall)
            {
                float xzMin = Mathf.Min(size.x, size.z);
                if (xzMin < minimumThickness)
                {
                    Debug.LogWarning($"ColliderOptimizer: {gameObject.name} 作为墙体，X/Z轴厚度 ({xzMin:F2}) 太小，建议增加到至少 {minimumThickness:F2}", this);
                }
            }
        }
    }

    public void CreateRecommendedPhysicMaterials()
    {
#if UNITY_EDITOR
        string path = "Assets/PhysicMaterials/";

        if (!System.IO.Directory.Exists(path))
        {
            System.IO.Directory.CreateDirectory(path);
        }

        if (groundMaterial == null)
        {
            PhysicMaterial groundMat = new PhysicMaterial("Ground");
            groundMat.dynamicFriction = 0.6f;
            groundMat.staticFriction = 0.8f;
            groundMat.bounciness = 0f;
            groundMat.frictionCombine = PhysicMaterialCombine.Average;
            groundMat.bounceCombine = PhysicMaterialCombine.Average;

            AssetDatabase.CreateAsset(groundMat, path + "Ground.mat");
            groundMaterial = groundMat;
        }

        if (wallMaterial == null)
        {
            PhysicMaterial wallMat = new PhysicMaterial("Wall");
            wallMat.dynamicFriction = 0.4f;
            wallMat.staticFriction = 0.6f;
            wallMat.bounciness = 0f;
            wallMat.frictionCombine = PhysicMaterialCombine.Average;
            wallMat.bounceCombine = PhysicMaterialCombine.Average;

            AssetDatabase.CreateAsset(wallMat, path + "Wall.mat");
            wallMaterial = wallMat;
        }

        if (obstacleMaterial == null)
        {
            PhysicMaterial obstacleMat = new PhysicMaterial("Obstacle");
            obstacleMat.dynamicFriction = 0.3f;
            obstacleMat.staticFriction = 0.5f;
            obstacleMat.bounciness = 0.1f;
            obstacleMat.frictionCombine = PhysicMaterialCombine.Average;
            obstacleMat.bounceCombine = PhysicMaterialCombine.Average;

            AssetDatabase.CreateAsset(obstacleMat, path + "Obstacle.mat");
            obstacleMaterial = obstacleMat;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("ColliderOptimizer: 已创建推荐的物理材质");
#endif
    }

    private void OnDrawGizmosSelected()
    {
        if (col == null) return;

        Gizmos.matrix = transform.localToWorldMatrix;

        Color gizmoColor = Color.white;
        switch (colliderType)
        {
            case ColliderType.Ground:
                gizmoColor = Color.green;
                break;
            case ColliderType.Wall:
                gizmoColor = Color.blue;
                break;
            case ColliderType.Ceiling:
                gizmoColor = Color.cyan;
                break;
            case ColliderType.Obstacle:
                gizmoColor = Color.yellow;
                break;
            case ColliderType.MovingPlatform:
                gizmoColor = Color.magenta;
                break;
        }

        gizmoColor.a = 0.3f;
        Gizmos.color = gizmoColor;

        if (col is BoxCollider boxCollider)
        {
            Gizmos.DrawCube(boxCollider.center, boxCollider.size);
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.8f);
            Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
        }
        else if (col is CapsuleCollider capsuleCollider)
        {
            Gizmos.DrawWireSphere(capsuleCollider.center + Vector3.up * (capsuleCollider.height / 2 - capsuleCollider.radius), capsuleCollider.radius);
            Gizmos.DrawWireSphere(capsuleCollider.center - Vector3.up * (capsuleCollider.height / 2 - capsuleCollider.radius), capsuleCollider.radius);
        }
        else if (col is SphereCollider sphereCollider)
        {
            Gizmos.DrawWireSphere(sphereCollider.center, sphereCollider.radius);
        }
    }
}
