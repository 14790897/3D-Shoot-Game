using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class EnemyHealth : MonoBehaviour
{
    public float maxHP = 100f;
    public float hp;
    public bool dropCoinOnDeath = true;      // 复用你已有 Coin 预制件可选
    public GameObject coinPrefab;            // 可留空
    public float destroyDelay = 3f;

    bool dead;

    void Awake() { hp = maxHP; }

    public void TakeDamage(float dmg, Vector3 hitPoint, Vector3 hitNormal, bool headshot = false)
    {
        if (dead) return;
        hp = Mathf.Max(0, hp - dmg);
        // 轻量命中特效：可在此处实例化血雾/火花
        if (hp <= 0) Die();
    }

    void Die()
    {
        if (dead) return;
        dead = true;

        // 停止追击/导航
        var ch = GetComponent<EnemyChase>();
        if (ch) ch.enabled = false;

        // 可选：播放倒地动画（此处略），或简单下沉表示死亡
        StartCoroutine(SinkAndDestroy());

        // 计分：沿用你的 GameManager
        GameManager.Instance.AddScore(1);

        // 掉落
        if (dropCoinOnDeath && coinPrefab)
            Instantiate(coinPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);

        // 关闭碰撞避免挡路
        foreach (var col in GetComponentsInChildren<Collider>()) col.enabled = false;
    }

    IEnumerator SinkAndDestroy()
    {
        float t = 0;
        Vector3 start = transform.position;
        Vector3 end = start + Vector3.down * 0.8f;
        while (t < destroyDelay)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(start, end, t / destroyDelay);
            yield return null;
        }
        Destroy(gameObject);
    }
}
