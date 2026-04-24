using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class TriggerZone : MonoBehaviour
{
    public enum TriggerType
    {
        Victory,
        SceneSwitch,
        Death,
        Checkpoint,
        Collectible
    }

    [Header("触发设置")]
    [SerializeField] private TriggerType triggerType = TriggerType.Victory;
    [SerializeField] private string targetSceneName;
    [SerializeField] private float delayTime = 0f;
    [SerializeField] private bool oneTimeTrigger = true;
    [SerializeField] private LayerMask targetLayer;

    [Header("收集品设置 (仅Collectible类型)")]
    [SerializeField] private int collectibleValue = 1;
    [SerializeField] private string collectibleTag = "Collectible";

    [Header("视觉反馈")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private Color gizmoColor = Color.green;

    public event Action<TriggerType, GameObject> OnTriggered;

    private bool hasTriggered = false;
    private Collider zoneCollider;

    private void Awake()
    {
        zoneCollider = GetComponent<Collider>();
        if (zoneCollider != null && !zoneCollider.isTrigger)
        {
            Debug.LogWarning($"TriggerZone {gameObject.name}: Collider 未设置为 Trigger 模式");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleTrigger(other.gameObject);
    }

    private void OnTriggerStay(Collider other)
    {
        HandleTrigger(other.gameObject);
    }

    private void HandleTrigger(GameObject other)
    {
        if (oneTimeTrigger && hasTriggered) return;

        if (!IsInTargetLayer(other.layer)) return;

        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null) return;

        hasTriggered = true;
        ExecuteTriggerEvent(other);
        OnTriggered?.Invoke(triggerType, other);
    }

    private bool IsInTargetLayer(int layer)
    {
        return (targetLayer.value & (1 << layer)) != 0;
    }

    private void ExecuteTriggerEvent(GameObject other)
    {
        switch (triggerType)
        {
            case TriggerType.Victory:
                HandleVictory(other);
                break;
            case TriggerType.SceneSwitch:
                HandleSceneSwitch();
                break;
            case TriggerType.Death:
                HandleDeath(other);
                break;
            case TriggerType.Checkpoint:
                HandleCheckpoint(other);
                break;
            case TriggerType.Collectible:
                HandleCollectible(other);
                break;
        }
    }

    private void HandleVictory(GameObject other)
    {
        Debug.Log($"玩家 {other.name} 到达终点！胜利！");
        if (delayTime > 0)
        {
            Invoke(nameof(DelayedVictoryAction), delayTime);
        }
        else
        {
            ShowVictoryUI();
        }
    }

    private void DelayedVictoryAction()
    {
        ShowVictoryUI();
    }

    private void ShowVictoryUI()
    {
        Debug.Log("显示胜利界面");
    }

    private void HandleSceneSwitch()
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogWarning("TriggerZone: 未指定目标场景名称");
            return;
        }

        Debug.Log($"切换到场景: {targetSceneName}");
        if (delayTime > 0)
        {
            Invoke(nameof(LoadSceneDelayed), delayTime);
        }
        else
        {
            LoadScene();
        }
    }

    private void LoadSceneDelayed()
    {
        LoadScene();
    }

    private void LoadScene()
    {
        SceneManager.LoadScene(targetSceneName);
    }

    private void HandleDeath(GameObject other)
    {
        Debug.Log($"玩家 {other.name} 死亡！");
        PlayerStats stats = other.GetComponent<PlayerStats>();
        if (stats != null)
        {
            stats.SetStamina(0f);
        }
    }

    private void HandleCheckpoint(GameObject other)
    {
        Debug.Log($"玩家 {other.name} 到达检查点: {gameObject.name}");
    }

    private void HandleCollectible(GameObject other)
    {
        Debug.Log($"玩家 {other.name} 收集了 {collectibleValue} 个 {collectibleTag}");
        Destroy(gameObject);
    }

    public void ResetTrigger()
    {
        hasTriggered = false;
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Gizmos.color = gizmoColor;
        Collider collider = GetComponent<Collider>();

        if (collider is BoxCollider boxCollider)
        {
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
        }
        else if (collider is SphereCollider sphereCollider)
        {
            Gizmos.DrawWireSphere(transform.position + sphereCollider.center, sphereCollider.radius);
        }
        else if (collider is CapsuleCollider capsuleCollider)
        {
            DrawCapsuleGizmo(capsuleCollider);
        }
    }

    private void DrawCapsuleGizmo(CapsuleCollider capsule)
    {
        Vector3 center = capsule.center;
        float radius = capsule.radius;
        float height = capsule.height;

        Vector3 point1 = center + Vector3.up * (height / 2 - radius);
        Vector3 point2 = center - Vector3.up * (height / 2 - radius);

        Gizmos.DrawWireSphere(transform.position + point1, radius);
        Gizmos.DrawWireSphere(transform.position + point2, radius);

        Gizmos.DrawLine(
            transform.position + point1 + Vector3.right * radius,
            transform.position + point2 + Vector3.right * radius
        );
        Gizmos.DrawLine(
            transform.position + point1 - Vector3.right * radius,
            transform.position + point2 - Vector3.right * radius
        );
        Gizmos.DrawLine(
            transform.position + point1 + Vector3.forward * radius,
            transform.position + point2 + Vector3.forward * radius
        );
        Gizmos.DrawLine(
            transform.position + point1 - Vector3.forward * radius,
            transform.position + point2 - Vector3.forward * radius
        );
    }
}
