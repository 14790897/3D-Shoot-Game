using UnityEngine;
using UnityEngine.AI;

public class EnemyChase : MonoBehaviour
{
    public Transform target;   // 拖 Player
    public float damage = 10f;
    public float attackInterval = 1.0f;
    NavMeshAgent agent;
    float atkTimer;

    void Awake() => agent = GetComponent<NavMeshAgent>();

    void Update()
    {
        if (!target) return;
        agent.SetDestination(target.position);

        atkTimer -= Time.deltaTime;
        if (atkTimer <= 0f && Vector3.Distance(transform.position, target.position) < 1.5f)
        {
            atkTimer = attackInterval;
            // 通过 UIManager 管理的玩家血量系统扣血
            if (UIManager.Instance != null)
                UIManager.Instance.PlayerTakeDamage(damage);
            else
                Debug.LogWarning("EnemyChase: UIManager 未就绪，无法对玩家造成伤害。");
        }
    }
}
