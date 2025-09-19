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
            var hp = target.GetComponent<PlayerHealth>();
            if (hp) hp.TakeDamage(damage);
        }
    }
}
