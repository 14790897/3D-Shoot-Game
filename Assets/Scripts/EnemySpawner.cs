using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    [Header("Prefab & Target")]
    public GameObject enemyPrefab;          // 敌人预制体（包含 EnemyChase + EnemyHealth + NavMeshAgent）
    public Transform player;                // 若为空会在 Start 自动查找

    [Header("生成设置")]
    public bool spawnOnStart = true;        // 进入场景自动生成
    public float initialDelay = 1f;         // 开始延迟
    public float spawnInterval = 3f;        // 间隔
    public int maxAlive = 10;               // 同屏上限

    [Header("位置设置")]
    public Transform[] spawnPoints;         // 可选：指定若干出生点
    public bool useRandomCircle = false;    // 若为 true 或未配置出生点，则在 spawner 周围随机
    public float randomRadius = 12f;        // 随机半径

    readonly List<GameObject> _alive = new List<GameObject>(64);

    void Start()
    {
        if (!player)
        {
            // 仅通过 Player 标签查找
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go) player = go.transform;
        }

        if (spawnOnStart)
            StartCoroutine(SpawnLoop());
    }

    IEnumerator SpawnLoop()
    {
        if (initialDelay > 0) yield return new WaitForSeconds(initialDelay);

        while (true)
        {
            // 游戏结束则停止
            if (GameManager.Instance != null && GameManager.Instance.isGameOver)
                yield break;

            Prune();

            if (enemyPrefab == null)
            {
                Debug.LogWarning("EnemySpawner: 未设置 enemyPrefab。");
                yield break;
            }

            if (_alive.Count < maxAlive)
            {
                TrySpawnOne();
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    void Prune()
    {
        for (int i = _alive.Count - 1; i >= 0; i--)
        {
            if (_alive[i] == null) _alive.RemoveAt(i);
        }
    }

    void TrySpawnOne()
    {
        Vector3 pos;
        Quaternion rot = Quaternion.identity;

        bool useRandom = useRandomCircle || spawnPoints == null || spawnPoints.Length == 0;
        if (useRandom)
        {
            if (!RandomPointOnNavmeshNear(transform.position, randomRadius, out pos))
                pos = transform.position + Random.onUnitSphere * 0.5f;
        }
        else
        {
            var sp = spawnPoints[Random.Range(0, spawnPoints.Length)];
            pos = sp.position; rot = sp.rotation;
            // 贴到导航网格
            if (NavMesh.SamplePosition(pos, out var hit, 2f, NavMesh.AllAreas)) pos = hit.position;
        }

        var go = Instantiate(enemyPrefab, pos, rot);
        _alive.Add(go);

        // 赋目标给追击脚本
        var ch = go.GetComponent<EnemyChase>();
        if (ch && player) ch.target = player;
    }

    bool RandomPointOnNavmeshNear(Vector3 center, float radius, out Vector3 result)
    {
        for (int i = 0; i < 12; i++)
        {
            var rand = Random.insideUnitSphere; rand.y = 0f;
            var sample = center + rand.normalized * Random.Range(0.2f * radius, radius);
            if (NavMesh.SamplePosition(sample, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
            {
                result = hit.position;
                return true;
            }
        }
        result = Vector3.zero;
        return false;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (useRandomCircle || spawnPoints == null || spawnPoints.Length == 0)
        {
            Gizmos.color = new Color(1, 0.2f, 0.2f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, randomRadius);
        }
        if (spawnPoints != null)
        {
            Gizmos.color = new Color(0.2f, 1, 0.2f, 0.6f);
            foreach (var t in spawnPoints)
            {
                if (!t) continue;
                Gizmos.DrawSphere(t.position, 0.25f);
                Gizmos.DrawLine(transform.position, t.position);
            }
        }
    }
#endif
}
