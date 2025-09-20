using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class Bullet : MonoBehaviour
{
    float _damage;
    float _headMul = 2f;
    GameObject _hitFx;
    float _gravity;
    float _life;
    float _radius;
    Weapon _owner;
    Rigidbody _rb;
    bool _hit;
    Vector3 _prevPos;
    int _maskUsed = ~0; // Raycast 层过滤

    public void Init(Weapon owner, float damage, float headMul, GameObject hitFx, float gravity, float life, float radius, int maskUsed = ~0)
    {
        _owner = owner;
        _damage = damage;
        _headMul = headMul;
        _hitFx = hitFx;
        _gravity = gravity;
        _life = life;
        _radius = radius;
        _maskUsed = maskUsed;
    }

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = false;
    }

    void OnEnable()
    {
        if (_life > 0f) Destroy(gameObject, _life);
        _prevPos = transform.position;
    }

    void FixedUpdate()
    {
        if (_gravity != 0f && _rb)
            _rb.AddForce(Vector3.down * _gravity, ForceMode.Acceleration);

        // 手动CCD：在上帧到本帧之间做一次 SphereCast，避免高速穿透
        Vector3 cur = transform.position;
        Vector3 delta = cur - _prevPos;
        float dist = delta.magnitude;
        if (!_hit && dist > 1e-4f)
        {
            Vector3 dir = delta / dist;
            if (Physics.SphereCast(_prevPos, Mathf.Max(0.001f, _radius), dir, out RaycastHit hit, dist, _maskUsed, QueryTriggerInteraction.Collide))
            {
                Impact(hit.collider, hit.point, hit.normal);
                return;
            }
        }
        _prevPos = cur;
    }

    void OnCollisionEnter(Collision c)
    {
        if (_hit) return; // 只处理一次
        var contact = c.GetContact(0);
        Impact(c.collider, contact.point, contact.normal);
    }

    void Impact(Collider col, Vector3 point, Vector3 normal)
    {
        if (_hit) return;
        _hit = true;

        // 使用字符串比较，避免在未定义 "Head" 标签时 CompareTag 产生日志
        bool head = col && col.tag == "Head";
        float dmg = head ? _damage * _headMul : _damage;

        var eh = col.GetComponentInParent<EnemyHealth>();
        if (eh)
        {
            eh.TakeDamage(dmg, point, normal, head);
            if (UIManager.Instance != null) UIManager.Instance.PulseHitMarker();
        }
        else if (_hitFx)
        {
            var fx = GameObject.Instantiate(_hitFx, point + normal * 0.01f, Quaternion.LookRotation(normal));
            fx.transform.SetParent(col.transform, true);
            fx.SetActive(true);
        }

        Destroy(gameObject);
    }
}
